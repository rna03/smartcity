using SmartCity.Application.Models;
using SmartCity.Domain;

namespace SmartCity.Application.Abstractions;

public interface IIncidentPriorityService
{
    Task<IncidentPriorityAnalysis> AnalyzeAsync(
        double latitude,
        double longitude,
        IncidentType incidentType,
        CancellationToken cancellationToken);
}
