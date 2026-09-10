using NetTopologySuite.Geometries;

namespace SmartCity.Domain.Entities;

public sealed class Hospital
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Source { get; set; }
    public required string ExternalId { get; set; }
    public required Point Geometry { get; set; }

    // Longitude is X and latitude is Y in the EPSG:4326 coordinate system.
    public double Latitude => Geometry.Y;
    public double Longitude => Geometry.X;
}
