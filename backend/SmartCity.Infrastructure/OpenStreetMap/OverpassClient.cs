using System.Net;
using Microsoft.Extensions.Options;
using SmartCity.Application.Abstractions;
using SmartCity.Application.Configuration;
using SmartCity.Application.Exceptions;
using SmartCity.Application.Models;

namespace SmartCity.Infrastructure.OpenStreetMap;

internal sealed class OverpassClient(
    HttpClient httpClient,
    OverpassResponseMapper mapper,
    IOptions<OpenStreetMapOptions> options)
    : IOpenStreetMapDataSource
{
    public async Task<OpenStreetMapDataset> FetchAsync(
        BoundingBox boundingBox,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var query = OverpassQueryBuilder.Build(boundingBox, settings.TimeoutSeconds);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, string.Empty)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["data"] = query
                })
            };
            using var response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                throw new ExternalDataSourceException(
                    "Overpass API rate limit was reached. Wait before retrying the import.");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new ExternalDataSourceException(
                    $"Overpass API returned HTTP {(int)response.StatusCode} ({response.ReasonPhrase}).");
            }

            if (response.Content.Headers.ContentLength > settings.MaxResponseBytes)
            {
                throw new ExternalDataSourceException("Overpass response exceeded the configured size limit.");
            }

            var json = await ReadWithLimitAsync(
                response.Content,
                settings.MaxResponseBytes,
                cancellationToken);
            return mapper.Map(json);
        }
        catch (ExternalDataSourceException)
        {
            throw;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ExternalDataSourceException("Overpass API request timed out.");
        }
        catch (HttpRequestException exception)
        {
            throw new ExternalDataSourceException("Overpass API could not be reached.", exception);
        }
        catch (System.Text.Json.JsonException exception)
        {
            throw new ExternalDataSourceException("Overpass API returned invalid JSON.", exception);
        }
    }

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
}
