namespace SmartCity.Infrastructure.OpenStreetMap;

public sealed class OpenStreetMapOptions
{
    public const string SectionName = "OpenStreetMap";

    public OverpassEndpointOptions Primary { get; set; } = new();
    public OverpassEndpointOptions Fallback { get; set; } = new();
    public int MaxResponseBytes { get; set; } = 25 * 1024 * 1024;

    public bool IsValid() =>
        Primary.IsValid() &&
        Fallback.IsValid() &&
        MaxResponseBytes is >= 1_048_576 and <= 104_857_600;
}

public sealed class OverpassEndpointOptions
{
    public string Url { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 60;

    public bool IsValid() =>
        Uri.TryCreate(Url, UriKind.Absolute, out var uri) &&
        uri.Scheme is "http" or "https" &&
        TimeoutSeconds is >= 5 and <= 180;
}
