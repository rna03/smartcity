using SmartCity.Domain;

namespace SmartCity.Application.Models;

public sealed record IncidentTypeCount(
    int Fire,
    int Medical,
    int Accident,
    int Other);

public sealed record PriorityLevelCount(
    int Critical,
    int High,
    int Medium,
    int Low);

public sealed record DashboardIncidentItem(
    int Id,
    IncidentType Type,
    double Latitude,
    double Longitude,
    string? Description,
    int PriorityScore,
    PriorityLevel PriorityLevel,
    DateTimeOffset CreatedAtUtc);

public sealed record DashboardSummaryResponse(
    int TotalIncidents,
    IncidentTypeCount IncidentCounts,
    PriorityLevelCount PriorityCounts,
    IReadOnlyList<DashboardIncidentItem> LatestIncidents);
