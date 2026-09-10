namespace SmartCity.Domain.Entities;

public sealed class LocationAnalysis
{
    public int Id { get; set; }
    public int RegionId { get; set; }
    public decimal HospitalDistanceScore { get; set; }
    public decimal FireStationDistanceScore { get; set; }
    public decimal RoadAccessibilityScore { get; set; }
    public decimal PopulationScore { get; set; }
    public decimal IncidentDensityScore { get; set; }
    public decimal FinalNeedScore { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Region Region { get; set; } = null!;
}
