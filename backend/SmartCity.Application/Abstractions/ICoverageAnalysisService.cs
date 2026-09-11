using SmartCity.Application.Models;

namespace SmartCity.Application.Abstractions;

public interface ICoverageAnalysisService
{
    Task<CoverageAnalysisResult> AnalyzeAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken);
}
