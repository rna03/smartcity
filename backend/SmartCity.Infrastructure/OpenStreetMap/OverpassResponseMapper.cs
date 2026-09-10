using System.Text.Json;
using System.Text.Json.Serialization;
using NetTopologySuite.Geometries;
using SmartCity.Application.Models;
using SmartCity.Domain;
using SmartCity.Domain.Entities;

namespace SmartCity.Infrastructure.OpenStreetMap;

internal sealed class OverpassResponseMapper
{
    private const int Wgs84Srid = 4326;
    private readonly GeometryFactory _geometryFactory =
        new(new PrecisionModel(), Wgs84Srid);

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public OpenStreetMapDataset Map(ReadOnlySpan<byte> json)
    {
        var response = JsonSerializer.Deserialize<OverpassResponse>(json, SerializerOptions)
            ?? throw new JsonException("Overpass returned an empty JSON document.");

        var hospitals = new Dictionary<string, Hospital>(StringComparer.Ordinal);
        var fireStations = new Dictionary<string, FireStation>(StringComparer.Ordinal);
        var roads = new Dictionary<string, Road>(StringComparer.Ordinal);

        foreach (var element in response.Elements)
        {
            var externalId = CreateExternalId(element);
            if (externalId is null)
            {
                continue;
            }

            var amenity = GetTag(element, "amenity");
            if (amenity is "hospital" or "fire_station")
            {
                var point = CreateRepresentativePoint(element);
                if (point is null)
                {
                    continue;
                }

                var name = GetNameOrFallback(element, amenity, externalId);
                if (amenity == "hospital")
                {
                    hospitals.TryAdd(externalId, new Hospital
                    {
                        Name = name,
                        Source = DataSourceNames.OpenStreetMap,
                        ExternalId = externalId,
                        Geometry = point
                    });
                }
                else
                {
                    fireStations.TryAdd(externalId, new FireStation
                    {
                        Name = name,
                        Source = DataSourceNames.OpenStreetMap,
                        ExternalId = externalId,
                        Geometry = point
                    });
                }
            }

            var roadType = GetTag(element, "highway");
            if (roadType is not null && element.Type == "way")
            {
                var lineString = CreateRoadLineString(element);
                if (lineString is null)
                {
                    continue;
                }

                roads.TryAdd(externalId, new Road
                {
                    Name = GetNameOrFallback(element, $"{roadType} road", externalId),
                    RoadType = roadType,
                    Source = DataSourceNames.OpenStreetMap,
                    ExternalId = externalId,
                    Geometry = lineString
                });
            }
        }

        return new OpenStreetMapDataset(
            hospitals.Values.ToArray(),
            fireStations.Values.ToArray(),
            roads.Values.ToArray());
    }

    private Point? CreateRepresentativePoint(OverpassElement element)
    {
        var latitude = element.Type == "node" ? element.Latitude : element.Center?.Latitude;
        var longitude = element.Type == "node" ? element.Longitude : element.Center?.Longitude;

        if (!IsValidCoordinate(latitude, longitude))
        {
            return null;
        }

        // EPSG:4326 uses X=longitude and Y=latitude in NetTopologySuite.
        return _geometryFactory.CreatePoint(new Coordinate(longitude!.Value, latitude!.Value));
    }

    private LineString? CreateRoadLineString(OverpassElement element)
    {
        if (element.Geometry is null)
        {
            return null;
        }

        var coordinates = element.Geometry
            .Where(coordinate => IsValidCoordinate(coordinate.Latitude, coordinate.Longitude))
            .Select(coordinate => new Coordinate(coordinate.Longitude, coordinate.Latitude))
            .RemoveConsecutiveDuplicates()
            .ToArray();

        if (coordinates.Length < 2 || coordinates.All(coordinate => coordinate.Equals2D(coordinates[0])))
        {
            return null;
        }

        var lineString = _geometryFactory.CreateLineString(coordinates);
        return lineString.IsValid && !lineString.IsEmpty ? lineString : null;
    }

    private static bool IsValidCoordinate(double? latitude, double? longitude) =>
        latitude is >= -90 and <= 90 && longitude is >= -180 and <= 180;

    private static string? CreateExternalId(OverpassElement element) =>
        element.Type is "node" or "way" or "relation" && element.Id > 0
            ? $"{element.Type}/{element.Id}"
            : null;

    private static string? GetTag(OverpassElement element, string key) =>
        element.Tags?.GetValueOrDefault(key);

    private static string GetNameOrFallback(
        OverpassElement element,
        string featureType,
        string externalId)
    {
        var name = GetTag(element, "name");
        return string.IsNullOrWhiteSpace(name)
            ? $"Unnamed {featureType} ({externalId})"
            : name;
    }
}

internal static class CoordinateEnumerableExtensions
{
    public static IEnumerable<Coordinate> RemoveConsecutiveDuplicates(
        this IEnumerable<Coordinate> coordinates)
    {
        Coordinate? previous = null;
        foreach (var coordinate in coordinates)
        {
            if (previous is null || !coordinate.Equals2D(previous))
            {
                yield return coordinate;
                previous = coordinate;
            }
        }
    }
}

internal sealed class OverpassResponse
{
    [JsonPropertyName("elements")]
    public List<OverpassElement> Elements { get; init; } = [];
}

internal sealed class OverpassElement
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("lat")]
    public double? Latitude { get; init; }

    [JsonPropertyName("lon")]
    public double? Longitude { get; init; }

    [JsonPropertyName("center")]
    public OverpassCoordinate? Center { get; init; }

    [JsonPropertyName("geometry")]
    public List<OverpassCoordinate>? Geometry { get; init; }

    [JsonPropertyName("tags")]
    public Dictionary<string, string>? Tags { get; init; }
}

internal sealed class OverpassCoordinate
{
    [JsonPropertyName("lat")]
    public double Latitude { get; init; }

    [JsonPropertyName("lon")]
    public double Longitude { get; init; }
}
