using NetTopologySuite.Geometries;

namespace SmartCity.Domain.Entities;

public sealed class Region
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int Population { get; set; }
    public double PopulationDensity { get; set; }
    public int IncidentCount { get; set; }
    public required MultiPolygon Geometry { get; set; }

    public ICollection<LocationAnalysis> LocationAnalyses { get; set; } = [];
}
