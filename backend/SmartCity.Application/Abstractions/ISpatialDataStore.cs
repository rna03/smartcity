using SmartCity.Application.Models;

namespace SmartCity.Application.Abstractions;

public interface ISpatialDataStore
{
    Task<SpatialImportCounts> AddNewAsync(
        OpenStreetMapDataset dataset,
        CancellationToken cancellationToken);
}
