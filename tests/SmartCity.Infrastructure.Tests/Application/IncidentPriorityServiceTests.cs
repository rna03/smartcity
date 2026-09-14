using SmartCity.Application.Abstractions;
using SmartCity.Application.Models;
using SmartCity.Application.Services;
using SmartCity.Domain;

namespace SmartCity.Infrastructure.Tests.Application;

public sealed class IncidentPriorityServiceTests
{
    private const double Latitude = 41.04;
    private const double Longitude = 29.01;

    [Theory]
    [InlineData(
        IncidentType.Fire,
        EmergencyServiceType.FireStation,
        22,
        4_000d,
        40,
        30)]
    [InlineData(
        IncidentType.Medical,
        EmergencyServiceType.Hospital,
        11,
        900d,
        35,
        0)]
    [InlineData(
        IncidentType.Accident,
        EmergencyServiceType.Hospital,
        11,
        900d,
        30,
        0)]
    [InlineData(
        IncidentType.Other,
        EmergencyServiceType.Hospital,
        11,
        900d,
        20,
        0)]
    public async Task AnalyzeSelectsRelevantServiceForIncidentType(
        IncidentType incidentType,
        EmergencyServiceType expectedServiceType,
        int expectedServiceId,
        double expectedDistanceMeters,
        int expectedBaseScore,
        int expectedDistanceScore)
    {
        var nearest = CreateAnalysis(
            CreateNearestService(11, "Hospital", 900),
            CreateNearestService(22, "Fire Station", 4_000));
        var locationService = new StubLocationAnalysisService(nearest);
        var accessibilityService = new StubAccessibilityAnalysisService(
            AccessibilityLevel.Excellent);
        var service = new IncidentPriorityService(
            locationService,
            accessibilityService);

        var result = await service.AnalyzeAsync(
            Latitude,
            Longitude,
            incidentType,
            CancellationToken.None);

        Assert.Equal(incidentType, result.IncidentType);
        Assert.Equal(expectedServiceType, result.RecommendedService!.ServiceType);
        Assert.Equal(expectedServiceId, result.RecommendedService.Id);
        Assert.Equal(expectedServiceType, result.Priority.RelevantServiceType);
        Assert.Equal(expectedDistanceMeters, result.Priority.RelevantServiceDistanceMeters);
        Assert.Equal(expectedBaseScore, result.Priority.IncidentTypeBaseScore);
        Assert.Equal(expectedDistanceScore, result.Priority.ServiceDistanceScore);
        Assert.Equal(expectedBaseScore + expectedDistanceScore, result.Priority.Score);
        Assert.Equal(AccessibilityLevel.Excellent, result.Priority.AccessibilityLevel);
        AssertSingleSharedNearestAnalysis(
            nearest,
            locationService,
            accessibilityService);
    }

    [Fact]
    public async Task OtherPrefersHospitalWhenDistancesTie()
    {
        var nearest = CreateAnalysis(
            CreateNearestService(11, "Hospital", 2_000),
            CreateNearestService(22, "Fire Station", 2_000));
        var locationService = new StubLocationAnalysisService(nearest);
        var accessibilityService = new StubAccessibilityAnalysisService(
            AccessibilityLevel.Good);
        var service = new IncidentPriorityService(
            locationService,
            accessibilityService);

        var result = await service.AnalyzeAsync(
            Latitude,
            Longitude,
            IncidentType.Other,
            CancellationToken.None);

        Assert.Equal(EmergencyServiceType.Hospital, result.RecommendedService!.ServiceType);
        Assert.Equal(11, result.RecommendedService.Id);
        Assert.Equal(2_000d, result.Priority.RelevantServiceDistanceMeters);
        AssertSingleSharedNearestAnalysis(
            nearest,
            locationService,
            accessibilityService);
    }

    [Fact]
    public async Task OtherSelectsFireStationWhenItIsCloser()
    {
        var nearest = CreateAnalysis(
            CreateNearestService(11, "Hospital", 2_500),
            CreateNearestService(22, "Fire Station", 1_500));
        var locationService = new StubLocationAnalysisService(nearest);
        var accessibilityService = new StubAccessibilityAnalysisService(
            AccessibilityLevel.Good);
        var service = new IncidentPriorityService(
            locationService,
            accessibilityService);

        var result = await service.AnalyzeAsync(
            Latitude,
            Longitude,
            IncidentType.Other,
            CancellationToken.None);

        Assert.Equal(EmergencyServiceType.FireStation, result.RecommendedService!.ServiceType);
        Assert.Equal(22, result.RecommendedService.Id);
        Assert.Equal(1_500d, result.Priority.RelevantServiceDistanceMeters);
        AssertSingleSharedNearestAnalysis(
            nearest,
            locationService,
            accessibilityService);
    }

    [Theory]
    [InlineData(true, EmergencyServiceType.Hospital, 11)]
    [InlineData(false, EmergencyServiceType.FireStation, 22)]
    public async Task OtherUsesTheOnlyAvailableService(
        bool hospitalAvailable,
        EmergencyServiceType expectedServiceType,
        int expectedServiceId)
    {
        var nearest = CreateAnalysis(
            hospitalAvailable
                ? CreateNearestService(11, "Hospital", 1_500)
                : null,
            hospitalAvailable
                ? null
                : CreateNearestService(22, "Fire Station", 1_500));
        var locationService = new StubLocationAnalysisService(nearest);
        var accessibilityService = new StubAccessibilityAnalysisService(
            AccessibilityLevel.Moderate);
        var service = new IncidentPriorityService(
            locationService,
            accessibilityService);

        var result = await service.AnalyzeAsync(
            Latitude,
            Longitude,
            IncidentType.Other,
            CancellationToken.None);

        Assert.Equal(expectedServiceType, result.RecommendedService!.ServiceType);
        Assert.Equal(expectedServiceId, result.RecommendedService.Id);
        Assert.Equal(expectedServiceType, result.Priority.RelevantServiceType);
        Assert.Equal(1_500d, result.Priority.RelevantServiceDistanceMeters);
        AssertSingleSharedNearestAnalysis(
            nearest,
            locationService,
            accessibilityService);
    }

    [Theory]
    [InlineData(IncidentType.Fire, EmergencyServiceType.FireStation)]
    [InlineData(IncidentType.Medical, EmergencyServiceType.Hospital)]
    [InlineData(IncidentType.Accident, EmergencyServiceType.Hospital)]
    public async Task RequiredServiceUnavailableUsesUnavailableDistancePenalty(
        IncidentType incidentType,
        EmergencyServiceType expectedRelevantServiceType)
    {
        var nearest = incidentType == IncidentType.Fire
            ? CreateAnalysis(CreateNearestService(11, "Hospital", 500), null)
            : CreateAnalysis(null, CreateNearestService(22, "Fire Station", 500));
        var locationService = new StubLocationAnalysisService(nearest);
        var accessibilityService = new StubAccessibilityAnalysisService(
            AccessibilityLevel.Critical);
        var service = new IncidentPriorityService(
            locationService,
            accessibilityService);

        var result = await service.AnalyzeAsync(
            Latitude,
            Longitude,
            incidentType,
            CancellationToken.None);

        Assert.Null(result.RecommendedService);
        Assert.Equal(expectedRelevantServiceType, result.Priority.RelevantServiceType);
        Assert.Null(result.Priority.RelevantServiceDistanceMeters);
        Assert.Equal(50, result.Priority.ServiceDistanceScore);
        AssertSingleSharedNearestAnalysis(
            nearest,
            locationService,
            accessibilityService);
    }

    [Fact]
    public async Task OtherWithNoAvailableServiceHasNoRelevantService()
    {
        var nearest = CreateAnalysis(null, null);
        var locationService = new StubLocationAnalysisService(nearest);
        var accessibilityService = new StubAccessibilityAnalysisService(
            AccessibilityLevel.Critical);
        var service = new IncidentPriorityService(
            locationService,
            accessibilityService);

        var result = await service.AnalyzeAsync(
            Latitude,
            Longitude,
            IncidentType.Other,
            CancellationToken.None);

        Assert.Null(result.RecommendedService);
        Assert.Null(result.Priority.RelevantServiceType);
        Assert.Null(result.Priority.RelevantServiceDistanceMeters);
        Assert.Equal(50, result.Priority.ServiceDistanceScore);
        Assert.Equal(90, result.Priority.Score);
        Assert.Equal(PriorityLevel.Critical, result.Priority.Level);
        AssertSingleSharedNearestAnalysis(
            nearest,
            locationService,
            accessibilityService);
    }

    [Fact]
    public async Task AnalyzeIntegratesAccessibilityWithoutAnotherNearestLookup()
    {
        var nearest = CreateAnalysis(
            CreateNearestService(11, "Hospital", 4_500),
            CreateNearestService(22, "Fire Station", 1_200));
        var locationService = new StubLocationAnalysisService(nearest);
        var accessibilityService = new AccessibilityAnalysisService(locationService);
        var service = new IncidentPriorityService(
            locationService,
            accessibilityService);
        using var cancellationSource = new CancellationTokenSource();

        var result = await service.AnalyzeAsync(
            Latitude,
            Longitude,
            IncidentType.Fire,
            cancellationSource.Token);

        Assert.Equal(1, locationService.CallCount);
        Assert.Equal(Latitude, locationService.Latitude);
        Assert.Equal(Longitude, locationService.Longitude);
        Assert.Equal(cancellationSource.Token, locationService.CancellationToken);
        Assert.Equal(AccessibilityLevel.Good, result.Priority.AccessibilityLevel);
        Assert.Equal(40, result.Priority.IncidentTypeBaseScore);
        Assert.Equal(10, result.Priority.ServiceDistanceScore);
        Assert.Equal(5, result.Priority.AccessibilityPenalty);
        Assert.Equal(55, result.Priority.Score);
        Assert.Equal(PriorityLevel.High, result.Priority.Level);
    }

    private static void AssertSingleSharedNearestAnalysis(
        NearestEmergencyServicesResult expectedNearest,
        StubLocationAnalysisService locationService,
        StubAccessibilityAnalysisService accessibilityService)
    {
        Assert.Equal(1, locationService.CallCount);
        Assert.Equal(1, accessibilityService.AnalyzeCallCount);
        Assert.Equal(0, accessibilityService.AnalyzeAsyncCallCount);
        Assert.Same(expectedNearest, accessibilityService.NearestServices);
    }

    private static NearestEmergencyServicesResult CreateAnalysis(
        NearestEmergencyServiceDto? hospital,
        NearestEmergencyServiceDto? fireStation) =>
        new(
            new SelectedLocationDto(Latitude, Longitude),
            hospital,
            fireStation);

    private static NearestEmergencyServiceDto CreateNearestService(
        int id,
        string name,
        double distanceMeters) =>
        new(
            id,
            name,
            $"node/{id}",
            Latitude + (id / 10_000d),
            Longitude + (id / 10_000d),
            distanceMeters);

    private sealed class StubLocationAnalysisService(
        NearestEmergencyServicesResult result)
        : ILocationAnalysisService
    {
        public int CallCount { get; private set; }
        public double? Latitude { get; private set; }
        public double? Longitude { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        public Task<NearestEmergencyServicesResult> FindNearestAsync(
            double latitude,
            double longitude,
            CancellationToken cancellationToken)
        {
            CallCount += 1;
            Latitude = latitude;
            Longitude = longitude;
            CancellationToken = cancellationToken;
            return Task.FromResult(result);
        }
    }

    private sealed class StubAccessibilityAnalysisService(
        AccessibilityLevel accessibilityLevel)
        : IAccessibilityAnalysisService
    {
        public int AnalyzeCallCount { get; private set; }
        public int AnalyzeAsyncCallCount { get; private set; }
        public NearestEmergencyServicesResult? NearestServices { get; private set; }

        public AccessibilityAnalysisResponse Analyze(
            NearestEmergencyServicesResult nearestServices)
        {
            AnalyzeCallCount += 1;
            NearestServices = nearestServices;
            return new AccessibilityAnalysisResponse(
                nearestServices.SelectedLocation,
                new AccessibilityServiceScore(
                    nearestServices.NearestHospital?.DistanceMeters,
                    0),
                new AccessibilityServiceScore(
                    nearestServices.NearestFireStation?.DistanceMeters,
                    0),
                0,
                accessibilityLevel);
        }

        public Task<AccessibilityAnalysisResponse> AnalyzeAsync(
            double latitude,
            double longitude,
            CancellationToken cancellationToken)
        {
            AnalyzeAsyncCallCount += 1;
            throw new InvalidOperationException(
                "Priority analysis must reuse the existing nearest-services result.");
        }
    }
}
