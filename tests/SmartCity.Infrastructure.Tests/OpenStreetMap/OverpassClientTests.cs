using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SmartCity.Application.Configuration;
using SmartCity.Application.Exceptions;
using SmartCity.Infrastructure.OpenStreetMap;

namespace SmartCity.Infrastructure.Tests.OpenStreetMap;

public sealed class OverpassClientTests
{
    private const string PrimaryUrl =
        "https://overpass.kumi.systems/api/interpreter";
    private const string FallbackUrl =
        "https://overpass-api.de/api/interpreter";

    [Fact]
    public async Task PrimarySuccessDoesNotCallFallback()
    {
        using var handler = new SequenceHandler(Success);
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        var dataset = await client.FetchAsync(CreateBoundingBox(), CancellationToken.None);

        Assert.Empty(dataset.Hospitals);
        Assert.Single(handler.RequestUris);
        Assert.Equal(PrimaryUrl, handler.RequestUris[0].AbsoluteUri);
    }

    [Fact]
    public async Task PrimaryServerErrorCallsFallbackOnce()
    {
        using var handler = new SequenceHandler(
            (_, _) => Task.FromResult(new HttpResponseMessage(
                HttpStatusCode.ServiceUnavailable)),
            Success);
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        await client.FetchAsync(CreateBoundingBox(), CancellationToken.None);

        Assert.Equal(2, handler.RequestUris.Count);
        Assert.Equal(PrimaryUrl, handler.RequestUris[0].AbsoluteUri);
        Assert.Equal(FallbackUrl, handler.RequestUris[1].AbsoluteUri);
    }

    [Fact]
    public async Task PrimaryTimeoutCallsFallbackOnce()
    {
        using var handler = new SequenceHandler(
            (_, _) => Task.FromException<HttpResponseMessage>(
                new TaskCanceledException("Simulated endpoint timeout.")),
            Success);
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        await client.FetchAsync(CreateBoundingBox(), CancellationToken.None);

        Assert.Equal(2, handler.RequestUris.Count);
        Assert.Equal(PrimaryUrl, handler.RequestUris[0].AbsoluteUri);
        Assert.Equal(FallbackUrl, handler.RequestUris[1].AbsoluteUri);
    }

    [Fact]
    public async Task PrimaryClientErrorDoesNotCallFallback()
    {
        using var handler = new SequenceHandler(
            (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        await Assert.ThrowsAsync<ExternalDataSourceException>(
            () => client.FetchAsync(CreateBoundingBox(), CancellationToken.None));

        Assert.Single(handler.RequestUris);
        Assert.Equal(PrimaryUrl, handler.RequestUris[0].AbsoluteUri);
    }

    private static HttpClient CreateHttpClient(HttpMessageHandler handler) =>
        new(handler)
        {
            Timeout = Timeout.InfiniteTimeSpan
        };

    private static OverpassClient CreateClient(HttpClient httpClient) =>
        new(
            httpClient,
            new OverpassResponseMapper(),
            Options.Create(CreateOptions()),
            NullLogger<OverpassClient>.Instance);

    private static OpenStreetMapOptions CreateOptions() => new()
    {
        Primary = new OverpassEndpointOptions
        {
            Url = PrimaryUrl,
            TimeoutSeconds = 90
        },
        Fallback = new OverpassEndpointOptions
        {
            Url = FallbackUrl,
            TimeoutSeconds = 90
        },
        MaxResponseBytes = 25 * 1024 * 1024
    };

    private static BoundingBox CreateBoundingBox() =>
        new(41.035, 28.99, 41.055, 29.03);

    private static Task<HttpResponseMessage> Success(
        HttpRequestMessage _,
        CancellationToken __) =>
        Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "{\"elements\":[]}",
                Encoding.UTF8,
                "application/json")
        });

    private sealed class SequenceHandler(
        params Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>[] responses)
        : HttpMessageHandler
    {
        private readonly Queue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>>
            _responses = new(responses);

        public List<Uri> RequestUris { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUris.Add(request.RequestUri
                ?? throw new InvalidOperationException("The request URI was not set."));

            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No response was configured for the request.");
            }

            return _responses.Dequeue()(request, cancellationToken);
        }
    }
}
