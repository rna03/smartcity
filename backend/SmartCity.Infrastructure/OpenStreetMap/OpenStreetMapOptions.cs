namespace SmartCity.Infrastructure.OpenStreetMap;

public sealed class OpenStreetMapOptions
{
    public const string SectionName = "OpenStreetMap";

    public string OverpassUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 60;
    public int MaxResponseBytes { get; set; } = 25 * 1024 * 1024;

    public bool IsValid() =>
        Uri.TryCreate(OverpassUrl, UriKind.Absolute, out var uri) &&
        uri.Scheme is "http" or "https" &&
        TimeoutSeconds is >= 5 and <= 180 &&
        MaxResponseBytes is >= 1_048_576 and <= 104_857_600;
}
