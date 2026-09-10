using SmartCity.Application.Configuration;

namespace SmartCity.Application.Models;

public sealed record PointFeatureDto(
    int Id,
    string Name,
    string Source,
    string ExternalId,
    double Longitude,
    double Latitude);

public sealed record RoadFeatureDto(
    int Id,
    string Name,
    string RoadType,
    string Source,
    string ExternalId,
    IReadOnlyList<CoordinateDto> Coordinates);

public readonly record struct CoordinateDto(double Longitude, double Latitude);

public sealed record MapConfigurationDto(
    string PilotArea,
    double CenterLatitude,
    double CenterLongitude,
    MapBoundsDto Bounds)
{
    public static MapConfigurationDto From(PilotAreaOptions options) =>
        new(
            options.Name,
            (options.South + options.North) / 2,
            (options.West + options.East) / 2,
            new MapBoundsDto(
                options.South,
                options.West,
                options.North,
                options.East));
}

public readonly record struct MapBoundsDto(
    double South,
    double West,
    double North,
    double East);
