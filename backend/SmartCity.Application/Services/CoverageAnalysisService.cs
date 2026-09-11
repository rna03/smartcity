using SmartCity.Application.Abstractions;
using SmartCity.Application.Models;

namespace SmartCity.Application.Services;

public sealed class CoverageAnalysisService(
    ILocationAnalysisService locationAnalysisService)
    : ICoverageAnalysisService
{
    public async Task<CoverageAnalysisResult> AnalyzeAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken)
    {
        var nearest = await locationAnalysisService.FindNearestAsync(
            latitude,
            longitude,
            cancellationToken);
        var hospital = CreateServiceCoverage(
            nearest.NearestHospital?.DistanceMeters);
        var fireStation = CreateServiceCoverage(
            nearest.NearestFireStation?.DistanceMeters);

        return new CoverageAnalysisResult(
            nearest.SelectedLocation,
            hospital,
            fireStation,
            CoverageClassifier.Aggregate(
                hospital.CoverageLevel,
                fireStation.CoverageLevel));
    }

    private static ServiceCoverageDto CreateServiceCoverage(
        double? distanceMeters) =>
        new(distanceMeters, CoverageClassifier.Classify(distanceMeters));
}
