using SmartCity.Application.Models;

namespace SmartCity.Application.Abstractions;

public interface IAccessibilityAnalysisService
{
    AccessibilityAnalysisResponse Analyze(
        NearestEmergencyServicesResult nearestServices);

    Task<AccessibilityAnalysisResponse> AnalyzeAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken);
}
