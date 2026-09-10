using NetTopologySuite.Geometries;

namespace SmartCity.Domain.Entities;

public sealed class FireStation
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Source { get; set; }
    public required string ExternalId { get; set; }
    public required Point Geometry { get; set; }

    public double Latitude => Geometry.Y;
    public double Longitude => Geometry.X;
}
