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
