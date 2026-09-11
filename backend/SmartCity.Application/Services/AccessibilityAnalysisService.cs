using SmartCity.Application.Abstractions;
using SmartCity.Application.Models;

namespace SmartCity.Application.Services;

public sealed class AccessibilityAnalysisService(
    ILocationAnalysisService locationAnalysisService)
    : IAccessibilityAnalysisService
{
    public async Task<AccessibilityAnalysisResponse> AnalyzeAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken)
    {
        var nearest = await locationAnalysisService.FindNearestAsync(
            latitude,
            longitude,
            cancellationToken);
        var hospital = CreateServiceScore(
            nearest.NearestHospital?.DistanceMeters);
        var fireStation = CreateServiceScore(
            nearest.NearestFireStation?.DistanceMeters);
        var totalScore = hospital.Score + fireStation.Score;

        return new AccessibilityAnalysisResponse(
            nearest.SelectedLocation,
            hospital,
            fireStation,
            totalScore,
            AccessibilityScoreCalculator.CalculateLevel(totalScore));
    }

    private static AccessibilityServiceScore CreateServiceScore(
        double? distanceMeters) =>
        new(
            distanceMeters,
            AccessibilityScoreCalculator.CalculateServiceScore(distanceMeters));
}
