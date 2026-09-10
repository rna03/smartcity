using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartCity.Application.Abstractions;
using SmartCity.Application.Configuration;
using SmartCity.Application.Exceptions;
using SmartCity.Application.Models;

namespace SmartCity.Infrastructure.OpenStreetMap;

internal sealed partial class OverpassClient(
    HttpClient httpClient,
    OverpassResponseMapper mapper,
    IOptions<OpenStreetMapOptions> options,
    ILogger<OverpassClient> logger)
    : IOpenStreetMapDataSource
{
    public async Task<OpenStreetMapDataset> FetchAsync(
        BoundingBox boundingBox,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;

        try
        {
            return await FetchFromEndpointAsync(
                settings.Primary,
                "primary",
                boundingBox,
                settings.MaxResponseBytes,
                cancellationToken);
        }
        catch (TransientOverpassException primaryFailure)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LogFallbackTransition(
                logger,
                settings.Primary.Url,
                settings.Fallback.Url,
                primaryFailure.Message);

            try
            {
                return await FetchFromEndpointAsync(
                    settings.Fallback,
                    "fallback",
                    boundingBox,
                    settings.MaxResponseBytes,
                    cancellationToken);
            }
            catch (TransientOverpassException fallbackFailure)
            {
                throw new ExternalDataSourceException(
                    fallbackFailure.Message,
                    fallbackFailure.InnerException ?? fallbackFailure);
            }
        }
    }

    private async Task<OpenStreetMapDataset> FetchFromEndpointAsync(
        OverpassEndpointOptions endpoint,
        string endpointRole,
        BoundingBox boundingBox,
        int maximumResponseBytes,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LogAttempt(logger, endpointRole, endpoint.Url, endpoint.TimeoutSeconds);

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(endpoint.TimeoutSeconds));

        try
        {
            var query = OverpassQueryBuilder.Build(boundingBox, endpoint.TimeoutSeconds);
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint.Url)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["data"] = query
                })
            };
            using var response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                timeoutSource.Token);

            if (!response.IsSuccessStatusCode)
            {
                LogHttpFailure(
                    logger,
                    endpointRole,
                    endpoint.Url,
                    (int)response.StatusCode,
                    response.ReasonPhrase ?? "Unknown");
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                throw new ExternalDataSourceException(
                    "Overpass API rate limit was reached. Wait before retrying the import.");
            }

            if ((int)response.StatusCode >= 500)
            {
                throw new TransientOverpassException(
                    $"Overpass API returned HTTP {(int)response.StatusCode} ({response.ReasonPhrase}).");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new ExternalDataSourceException(
                    $"Overpass API returned HTTP {(int)response.StatusCode} ({response.ReasonPhrase}).");
            }

            if (response.Content.Headers.ContentLength > maximumResponseBytes)
            {
                throw new ExternalDataSourceException(
                    "Overpass response exceeded the configured size limit.");
            }

            var json = await ReadWithLimitAsync(
                response.Content,
                maximumResponseBytes,
                timeoutSource.Token);
            var dataset = mapper.Map(json);
            LogSuccess(logger, endpointRole, endpoint.Url);
            return dataset;
        }
        catch (ExternalDataSourceException)
        {
            throw;
        }
        catch (TransientOverpassException)
        {
            throw;
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogTimeout(
                logger,
                exception,
                endpointRole,
                endpoint.Url,
                endpoint.TimeoutSeconds);
            throw new TransientOverpassException(
                $"Overpass API request to {endpoint.Url} timed out.",
                exception);
        }
        catch (HttpRequestException exception)
        {
            LogNetworkFailure(logger, exception, endpointRole, endpoint.Url);
            throw new TransientOverpassException(
                $"Overpass API endpoint {endpoint.Url} could not be reached.",
                exception);
        }
        catch (System.Text.Json.JsonException exception)
        {
            throw new ExternalDataSourceException(
                "Overpass API returned invalid JSON.",
                exception);
        }
    }

    [LoggerMessage(
        EventId = 3000,
        Level = LogLevel.Information,
        Message = "Sending request to {EndpointRole} Overpass endpoint {Endpoint} with timeout {TimeoutSeconds} seconds")]
    private static partial void LogAttempt(
        ILogger logger,
        string endpointRole,
        string endpoint,
        int timeoutSeconds);

    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Warning,
        Message = "{EndpointRole} Overpass endpoint {Endpoint} returned HTTP {StatusCode} ({ReasonPhrase})")]
    private static partial void LogHttpFailure(
        ILogger logger,
        string endpointRole,
        string endpoint,
        int statusCode,
        string reasonPhrase);

    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Warning,
        Message = "{EndpointRole} Overpass endpoint {Endpoint} timed out after {TimeoutSeconds} seconds")]
    private static partial void LogTimeout(
        ILogger logger,
        Exception exception,
        string endpointRole,
        string endpoint,
        int timeoutSeconds);

    [LoggerMessage(
        EventId = 3003,
        Level = LogLevel.Warning,
        Message = "{EndpointRole} Overpass endpoint {Endpoint} could not be reached due to a network error")]
    private static partial void LogNetworkFailure(
        ILogger logger,
        Exception exception,
        string endpointRole,
        string endpoint);

    [LoggerMessage(
        EventId = 3004,
        Level = LogLevel.Warning,
        Message = "Primary Overpass endpoint {PrimaryEndpoint} failed transiently; switching once to fallback endpoint {FallbackEndpoint}. Failure: {FailureMessage}")]
    private static partial void LogFallbackTransition(
        ILogger logger,
        string primaryEndpoint,
        string fallbackEndpoint,
        string failureMessage);

    [LoggerMessage(
        EventId = 3005,
        Level = LogLevel.Information,
        Message = "Request to {EndpointRole} Overpass endpoint {Endpoint} completed successfully")]
    private static partial void LogSuccess(
        ILogger logger,
        string endpointRole,
        string endpoint);

    private static async Task<byte[]> ReadWithLimitAsync(
        HttpContent content,
        int maximumBytes,
        CancellationToken cancellationToken)
    {
        await using var source = await content.ReadAsStreamAsync(cancellationToken);
        using var destination = new MemoryStream();
        var buffer = new byte[16 * 1024];
        int bytesRead;

        while ((bytesRead = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (destination.Length + bytesRead > maximumBytes)
            {
                throw new ExternalDataSourceException(
                    "Overpass response exceeded the configured size limit.");
            }

            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
        }

        return destination.ToArray();
    }

    private sealed class TransientOverpassException(
        string message,
        Exception? innerException = null)
        : Exception(message, innerException);
}
