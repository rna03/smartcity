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
        return Analyze(nearest);
    }

    public AccessibilityAnalysisResponse Analyze(
        NearestEmergencyServicesResult nearestServices)
    {
        var hospital = CreateServiceScore(
            nearestServices.NearestHospital?.DistanceMeters);
        var fireStation = CreateServiceScore(
            nearestServices.NearestFireStation?.DistanceMeters);
        var totalScore = hospital.Score + fireStation.Score;

        return new AccessibilityAnalysisResponse(
            nearestServices.SelectedLocation,
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
