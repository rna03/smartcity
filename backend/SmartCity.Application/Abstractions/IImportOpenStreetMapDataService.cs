using SmartCity.Application.Models;

namespace SmartCity.Application.Abstractions;

public interface IImportOpenStreetMapDataService
{
    Task<OpenStreetMapImportResult> ImportAsync(CancellationToken cancellationToken);
}
