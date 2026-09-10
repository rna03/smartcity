using System.Text;
using SmartCity.Application.Configuration;
using SmartCity.Infrastructure.OpenStreetMap;

namespace SmartCity.Infrastructure.Tests.OpenStreetMap;

public sealed class OverpassResponseMapperTests
{
    private readonly OverpassResponseMapper _mapper = new();

    [Fact]
    public void QueryUsesCenterForFacilitiesAndGeometryForRoads()
    {
        var query = OverpassQueryBuilder.Build(
            new BoundingBox(41.035, 28.99, 41.055, 29.03),
            60);

        Assert.Contains("out tags center;", query, StringComparison.Ordinal);
        Assert.Contains("out tags geom;", query, StringComparison.Ordinal);
        Assert.DoesNotContain("center geom", query, StringComparison.Ordinal);
    }

    [Fact]
    public void MapNodeHospitalCreatesPointWithLongitudeLatitudeOrderAndSrid4326()
    {
        var dataset = Map("""
            {
              "elements": [
                {
                  "type": "node",
                  "id": 123,
                  "lat": 41.05,
                  "lon": 29.01,
                  "tags": { "amenity": "hospital", "name": "Demo Hospital" }
                }
              ]
            }
            """);

        var hospital = Assert.Single(dataset.Hospitals);
        Assert.Equal("node/123", hospital.ExternalId);
        Assert.Equal(29.01, hospital.Geometry.X);
        Assert.Equal(41.05, hospital.Geometry.Y);
        Assert.Equal(4326, hospital.Geometry.SRID);
    }

    [Fact]
    public void MapWayFireStationUsesOverpassRepresentativeCenter()
    {
        var dataset = Map("""
            {
              "elements": [
                {
                  "type": "way",
                  "id": 456,
                  "center": { "lat": 41.04, "lon": 29.02 },
                  "tags": { "amenity": "fire_station", "name": "Demo Fire Station" }
                }
              ]
            }
            """);

        var station = Assert.Single(dataset.FireStations);
        Assert.Equal("way/456", station.ExternalId);
        Assert.Equal(4326, station.Geometry.SRID);
    }

    [Fact]
    public void MapDuplicateExternalIdReturnsSingleFeature()
    {
        var dataset = Map("""
            {
              "elements": [
                { "type": "node", "id": 123, "lat": 41.05, "lon": 29.01,
                  "tags": { "amenity": "hospital" } },
                { "type": "node", "id": 123, "lat": 41.05, "lon": 29.01,
                  "tags": { "amenity": "hospital" } }
              ]
            }
            """);

        Assert.Single(dataset.Hospitals);
    }

    [Fact]
    public void MapRoadWayCreatesValidLineString()
    {
        var dataset = Map("""
            {
              "elements": [
                {
                  "type": "way",
                  "id": 789,
                  "geometry": [
                    { "lat": 41.01, "lon": 29.01 },
                    { "lat": 41.02, "lon": 29.02 },
                    { "lat": 41.03, "lon": 29.03 }
                  ],
                  "tags": { "highway": "primary", "name": "Demo Road" }
                }
              ]
            }
            """);

        var road = Assert.Single(dataset.Roads);
        Assert.Equal("way/789", road.ExternalId);
        Assert.Equal("primary", road.RoadType);
        Assert.Equal(3, road.Geometry.NumPoints);
        Assert.True(road.Geometry.IsValid);
        Assert.Equal(4326, road.Geometry.SRID);
    }

    [Fact]
    public void MapMissingNameUsesStableFallbackName()
    {
        var dataset = Map("""
            {
              "elements": [
                { "type": "relation", "id": 321,
                  "center": { "lat": 41.04, "lon": 29.02 },
                  "tags": { "amenity": "hospital" } }
              ]
            }
            """);

        var hospital = Assert.Single(dataset.Hospitals);
        Assert.Equal("Unnamed hospital (relation/321)", hospital.Name);
    }

    private SmartCity.Application.Models.OpenStreetMapDataset Map(string json) =>
        _mapper.Map(Encoding.UTF8.GetBytes(json));
}
