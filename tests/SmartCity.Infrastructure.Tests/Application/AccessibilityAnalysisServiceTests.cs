using SmartCity.Application.Abstractions;
using SmartCity.Application.Models;
using SmartCity.Application.Services;

namespace SmartCity.Infrastructure.Tests.Application;

public sealed class AccessibilityAnalysisServiceTests
{
    [Fact]
    public async Task AnalyzeReusesNearestDistancesAndCalculatesTotalScore()
    {
        var nearestService = new StubLocationAnalysisService(new(
            new SelectedLocationDto(41.04, 29.01),
            CreateNearestService(1_697.68),
            CreateNearestService(1_266.68)));
        var service = new AccessibilityAnalysisService(nearestService);

        var result = await service.AnalyzeAsync(
            41.04,
            29.01,
            CancellationToken.None);

        Assert.Equal(1_697.68, result.Hospital.DistanceMeters);
        Assert.Equal(45, result.Hospital.Score);
        Assert.Equal(1_266.68, result.FireStation.DistanceMeters);
        Assert.Equal(45, result.FireStation.Score);
        Assert.Equal(90, result.TotalScore);
        Assert.Equal(AccessibilityLevel.Excellent, result.AccessibilityLevel);
        Assert.Equal(1, nearestService.CallCount);
    }

    [Fact]
    public async Task AnalyzeReturnsZeroAndCriticalForUnavailableServices()
    {
        var nearestService = new StubLocationAnalysisService(new(
            new SelectedLocationDto(41.04, 29.01),
            null,
            null));
        var service = new AccessibilityAnalysisService(nearestService);

        var result = await service.AnalyzeAsync(
            41.04,
            29.01,
            CancellationToken.None);

        Assert.Null(result.Hospital.DistanceMeters);
        Assert.Equal(0, result.Hospital.Score);
        Assert.Null(result.FireStation.DistanceMeters);
        Assert.Equal(0, result.FireStation.Score);
        Assert.Equal(0, result.TotalScore);
        Assert.Equal(AccessibilityLevel.Critical, result.AccessibilityLevel);
        Assert.Equal(1, nearestService.CallCount);
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
