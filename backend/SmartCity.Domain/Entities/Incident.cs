using NetTopologySuite.Geometries;

namespace SmartCity.Domain.Entities;

public sealed class Incident
{
    public int Id { get; set; }
    public IncidentType IncidentType { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public required Point Geometry { get; set; }

    public double Latitude => Geometry.Y;
    public double Longitude => Geometry.X;
}
