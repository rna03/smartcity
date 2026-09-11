using SmartCity.Application.Abstractions;
using SmartCity.Application.Models;
using SmartCity.Application.Services;

namespace SmartCity.Infrastructure.Tests.Application;

public sealed class CoverageAnalysisServiceTests
{
    [Fact]
    public async Task AnalyzeReusesNearestDistancesAndClassifiesCoverage()
    {
        var nearestService = new StubLocationAnalysisService(new(
            new SelectedLocationDto(41.04, 29.01),
            CreateNearestService(1_500),
            CreateNearestService(3_000)));
        var service = new CoverageAnalysisService(nearestService);

        var result = await service.AnalyzeAsync(
            41.04,
            29.01,
            CancellationToken.None);

        Assert.Equal(1_500d, result.Hospital.DistanceMeters);
        Assert.Equal(CoverageLevel.Good, result.Hospital.CoverageLevel);
        Assert.Equal(3_000d, result.FireStation.DistanceMeters);
        Assert.Equal(CoverageLevel.Moderate, result.FireStation.CoverageLevel);
        Assert.Equal(CoverageLevel.Moderate, result.OverallCoverageLevel);
        Assert.Equal(1, nearestService.CallCount);
    }

    [Fact]
    public async Task AnalyzeReturnsUnavailableWhenAServiceDoesNotExist()
    {
        var nearestService = new StubLocationAnalysisService(new(
            new SelectedLocationDto(41.04, 29.01),
            null,
            CreateNearestService(1_000)));
        var service = new CoverageAnalysisService(nearestService);

        var result = await service.AnalyzeAsync(
            41.04,
            29.01,
            CancellationToken.None);

        Assert.Null(result.Hospital.DistanceMeters);
        Assert.Equal(CoverageLevel.Unavailable, result.Hospital.CoverageLevel);
        Assert.Equal(CoverageLevel.Good, result.FireStation.CoverageLevel);
        Assert.Equal(CoverageLevel.Unavailable, result.OverallCoverageLevel);
    }

    private static NearestEmergencyServiceDto CreateNearestService(
        double distanceMeters) =>
        new(
            1,
            "Emergency service",
            "node/1",
            41.041,
            29.011,
            distanceMeters);

    private sealed class StubLocationAnalysisService(
        NearestEmergencyServicesResult result)
        : ILocationAnalysisService
    {
        public int CallCount { get; private set; }

        public Task<NearestEmergencyServicesResult> FindNearestAsync(
            double latitude,
            double longitude,
            CancellationToken cancellationToken)
        {
            CallCount += 1;
            return Task.FromResult(result);
        }
    }
}
