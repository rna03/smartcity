using SmartCity.Infrastructure.OpenStreetMap;

namespace SmartCity.Infrastructure.Tests.Configuration;

public sealed class OpenStreetMapOptionsTests
{
    [Fact]
    public void KumiEndpointIsAccepted()
    {
        var options = CreateOptions();

        Assert.True(options.IsValid());
    }

    [Theory]
    [InlineData("api/interpreter")]
    [InlineData("ftp://overpass.example.com/api/interpreter")]
    public void NonHttpAbsoluteEndpointIsRejected(string endpoint)
    {
        var options = CreateOptions();
        options.Primary.Url = endpoint;

        Assert.False(options.IsValid());
    }

    private static OpenStreetMapOptions CreateOptions() => new()
    {
        Primary = new OverpassEndpointOptions
        {
            Url = "https://overpass.kumi.systems/api/interpreter",
            TimeoutSeconds = 90
        },
        Fallback = new OverpassEndpointOptions
        {
            Url = "https://overpass-api.de/api/interpreter",
            TimeoutSeconds = 90
        },
        MaxResponseBytes = 25 * 1024 * 1024
    };
}
