using SmartCity.Application.Configuration;
using SmartCity.Application.Models;

namespace SmartCity.Application.Abstractions;

public interface IOpenStreetMapDataSource
{
    Task<OpenStreetMapDataset> FetchAsync(
        BoundingBox boundingBox,
        CancellationToken cancellationToken);
}
