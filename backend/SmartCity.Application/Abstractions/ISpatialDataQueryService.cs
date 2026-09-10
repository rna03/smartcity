using SmartCity.Application.Models;

namespace SmartCity.Application.Abstractions;

public interface ISpatialDataQueryService
{
    Task<IReadOnlyList<PointFeatureDto>> GetHospitalsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<PointFeatureDto>> GetFireStationsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<RoadFeatureDto>> GetRoadsAsync(CancellationToken cancellationToken);
}
