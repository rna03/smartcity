using SmartCity.Application.Models;

namespace SmartCity.Application.Abstractions;

public interface IAccessibilityAnalysisService
{
    Task<AccessibilityAnalysisResponse> AnalyzeAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken);
}
