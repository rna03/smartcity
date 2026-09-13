using SmartCity.Domain;

namespace SmartCity.Application.Models;

public sealed record IncidentTypeCount(
    int Fire,
    int Medical,
    int Accident,
    int Other);

public sealed record DashboardIncidentItem(
    int Id,
    IncidentType Type,
    double Latitude,
    double Longitude,
    string? Description,
    DateTimeOffset CreatedAtUtc);

public sealed record DashboardSummaryResponse(
    int TotalIncidents,
    IncidentTypeCount IncidentCounts,
    IReadOnlyList<DashboardIncidentItem> LatestIncidents);
