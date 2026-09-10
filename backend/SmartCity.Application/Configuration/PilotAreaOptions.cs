namespace SmartCity.Application.Configuration;

public sealed class PilotAreaOptions
{
    public const string SectionName = "PilotArea";

    public string Name { get; set; } = string.Empty;
    public double South { get; set; }
    public double West { get; set; }
    public double North { get; set; }
    public double East { get; set; }

    public BoundingBox ToBoundingBox() => new(South, West, North, East);

    public bool IsValid() =>
        !string.IsNullOrWhiteSpace(Name) &&
        South is >= -90 and <= 90 &&
        North is >= -90 and <= 90 &&
        West is >= -180 and <= 180 &&
        East is >= -180 and <= 180 &&
        South < North &&
        West < East;
}

public readonly record struct BoundingBox(
    double South,
    double West,
    double North,
    double East);
