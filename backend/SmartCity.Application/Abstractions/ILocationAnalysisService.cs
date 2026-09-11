using SmartCity.Application.Models;

namespace SmartCity.Application.Abstractions;

public interface ILocationAnalysisService
{
    Task<NearestEmergencyServicesResult> FindNearestAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken);
}
