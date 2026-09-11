using SmartCity.Application.Models;
using SmartCity.Domain;

namespace SmartCity.Application.Abstractions;

public interface IIncidentService
{
    Task<IncidentCreationResult> CreateAsync(
        IncidentType incidentType,
        double latitude,
        double longitude,
        string? description,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<IncidentDto>> GetAllAsync(CancellationToken cancellationToken);
}
