using SmartCity.Application.Abstractions;
using SmartCity.Application.Models;
using SmartCity.Domain;

namespace SmartCity.Application.Services;

public sealed class IncidentPriorityService(
    ILocationAnalysisService locationAnalysisService,
    IAccessibilityAnalysisService accessibilityAnalysisService)
    : IIncidentPriorityService
{
    public async Task<IncidentPriorityAnalysis> AnalyzeAsync(
        double latitude,
        double longitude,
        IncidentType incidentType,
        CancellationToken cancellationToken)
    {
        var nearest = await locationAnalysisService.FindNearestAsync(
            latitude,
            longitude,
            cancellationToken);
        var accessibility = accessibilityAnalysisService.Analyze(nearest);
        var recommendation = IncidentRecommendationSelector.Select(
            incidentType,
            nearest);
        var calculation = IncidentPriorityCalculator.Calculate(
            incidentType,
            recommendation?.DistanceMeters,
            accessibility.AccessibilityLevel);
        var relevantServiceType =
            IncidentRecommendationSelector.ResolveRelevantServiceType(
                incidentType,
                recommendation);

        return new IncidentPriorityAnalysis(
            nearest.SelectedLocation,
            incidentType,
            recommendation,
            new IncidentPriorityResult(
                calculation.Score,
                calculation.Level,
                calculation.IncidentTypeBaseScore,
                calculation.ServiceDistanceScore,
                calculation.AccessibilityPenalty,
                relevantServiceType,
                recommendation?.DistanceMeters,
                accessibility.AccessibilityLevel));
    }
}
