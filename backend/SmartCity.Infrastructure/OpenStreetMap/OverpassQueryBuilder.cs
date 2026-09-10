using System.Globalization;
using SmartCity.Application.Configuration;

namespace SmartCity.Infrastructure.OpenStreetMap;

internal static class OverpassQueryBuilder
{
    private const string MainRoadPattern =
        "^(motorway|motorway_link|trunk|trunk_link|primary|primary_link|" +
        "secondary|secondary_link|tertiary|tertiary_link)$";

    public static string Build(BoundingBox bounds, int timeoutSeconds)
    {
        var bbox = string.Join(",",
            bounds.South.ToString(CultureInfo.InvariantCulture),
            bounds.West.ToString(CultureInfo.InvariantCulture),
            bounds.North.ToString(CultureInfo.InvariantCulture),
            bounds.East.ToString(CultureInfo.InvariantCulture));

        return $"""
            [out:json][timeout:{timeoutSeconds}];
            (
              nwr["amenity"="hospital"]({bbox});
              nwr["amenity"="fire_station"]({bbox});
            );
            out tags center;
            way["highway"~"{MainRoadPattern}"]({bbox});
            out tags geom;
            """;
    }
}
