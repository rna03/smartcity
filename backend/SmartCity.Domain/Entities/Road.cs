using NetTopologySuite.Geometries;

namespace SmartCity.Domain.Entities;

public sealed class Road
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string RoadType { get; set; }
    public required string Source { get; set; }
    public required string ExternalId { get; set; }
    public required LineString Geometry { get; set; }
}
