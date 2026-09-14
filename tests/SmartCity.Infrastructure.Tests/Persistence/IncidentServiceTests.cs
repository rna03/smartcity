using Microsoft.EntityFrameworkCore;
using SmartCity.Application.Abstractions;
using SmartCity.Application.Models;
using SmartCity.Application.Services;
using SmartCity.Domain;
using SmartCity.Infrastructure.Persistence;

namespace SmartCity.Infrastructure.Tests.Persistence;

public sealed class IncidentServiceTests
{
    private static readonly DateTimeOffset FixedUtc =
        new(2026, 9, 11, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(IncidentType.Fire, EmergencyServiceType.FireStation)]
    [InlineData(IncidentType.Medical, EmergencyServiceType.Hospital)]
    [InlineData(IncidentType.Accident, EmergencyServiceType.Hospital)]
    public async Task CreatePersistsPointAndSelectsRecommendation(
        IncidentType incidentType,
        EmergencyServiceType expectedServiceType)
    {
        await using var dbContext = CreateDbContext();
        var priorityService = new StubIncidentPriorityService(
            CreatePriorityAnalysis(incidentType));
        var service = new IncidentService(
            dbContext,
            priorityService,
            new FixedTimeProvider(FixedUtc));

        var result = await service.CreateAsync(
            incidentType,
            41.04,
            29.01,
            "  Emergency report  ",
            CancellationToken.None);

        var saved = Assert.Single(await dbContext.Incidents.ToListAsync());
        Assert.True(saved.Id > 0);
        Assert.Equal(incidentType, saved.IncidentType);
        Assert.Equal("Emergency report", saved.Description);
        Assert.Equal(FixedUtc, saved.OccurredAt);
        Assert.Equal(4326, saved.Geometry.SRID);
        Assert.Equal(29.01, saved.Geometry.X);
        Assert.Equal(41.04, saved.Geometry.Y);
        Assert.Equal(29.01, result.Incident.Longitude);
        Assert.Equal(41.04, result.Incident.Latitude);
        Assert.Equal(expectedServiceType, result.RecommendedService!.ServiceType);
        Assert.True(result.RecommendedService.DistanceMeters > 0);
        Assert.Equal(result.Priority.Score, saved.PriorityScore);
        Assert.Equal(result.Priority.Level, saved.PriorityLevel);
        Assert.Equal(saved.PriorityScore, result.Incident.PriorityScore);
        Assert.Equal(saved.PriorityLevel, result.Incident.PriorityLevel);
        Assert.Equal(1, priorityService.CallCount);
    }

    [Fact]
    public async Task OtherSelectsClosestAvailableService()
    {
        await using var dbContext = CreateDbContext();
        var service = new IncidentService(
            dbContext,
            new StubIncidentPriorityService(
                CreatePriorityAnalysis(IncidentType.Other)),
            new FixedTimeProvider(FixedUtc));

        var result = await service.CreateAsync(
            IncidentType.Other,
            41.04,
            29.01,
            null,
            CancellationToken.None);

        Assert.Equal(EmergencyServiceType.Hospital, result.RecommendedService!.ServiceType);
    }

    [Fact]
    public async Task MissingRequiredServiceReturnsNullAndStillPersistsIncident()
    {
        await using var dbContext = CreateDbContext();
        var analysis = CreateAnalysis() with { NearestFireStation = null };
        var service = new IncidentService(
            dbContext,
            new StubIncidentPriorityService(
                CreatePriorityAnalysis(IncidentType.Fire, analysis)),
            new FixedTimeProvider(FixedUtc));

        var result = await service.CreateAsync(
            IncidentType.Fire,
            41.04,
            29.01,
            null,
            CancellationToken.None);

        Assert.Null(result.RecommendedService);
        var saved = Assert.Single(await dbContext.Incidents.ToListAsync());
        Assert.Equal(100, saved.PriorityScore);
        Assert.Equal(PriorityLevel.Critical, saved.PriorityLevel);
    }

    [Fact]
    public async Task GetAllReturnsPersistedIncidentsNewestFirst()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Incidents.AddRange(
            CreateIncident(IncidentType.Fire, FixedUtc.AddMinutes(-1)),
            CreateIncident(IncidentType.Medical, FixedUtc));
        await dbContext.SaveChangesAsync();
        var service = new IncidentService(
            dbContext,
            new StubIncidentPriorityService(
                CreatePriorityAnalysis(IncidentType.Fire)),
            new FixedTimeProvider(FixedUtc));

        var result = await service.GetAllAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal(IncidentType.Medical, result[0].Type);
        Assert.Equal(IncidentType.Fire, result[1].Type);
        Assert.Equal(80, result[0].PriorityScore);
        Assert.Equal(PriorityLevel.Critical, result[0].PriorityLevel);
        Assert.Equal(55, result[1].PriorityScore);
        Assert.Equal(PriorityLevel.High, result[1].PriorityLevel);
    }

    private static SmartCityDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SmartCityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new SmartCityDbContext(options);
    }

    private static SmartCity.Domain.Entities.Incident CreateIncident(
        IncidentType incidentType,
        DateTimeOffset occurredAt)
    {
        var isMedical = incidentType == IncidentType.Medical;
        return
        new()
        {
            IncidentType = incidentType,
            PriorityScore = isMedical ? 80 : 55,
            PriorityLevel = isMedical ? PriorityLevel.Critical : PriorityLevel.High,
            OccurredAt = occurredAt,
            Geometry = new NetTopologySuite.Geometries.Point(29.01, 41.04)
            {
                SRID = 4326
            }
        };
    }

    private static IncidentPriorityAnalysis CreatePriorityAnalysis(
        IncidentType incidentType,
        NearestEmergencyServicesResult? nearestServices = null)
    {
        var nearest = nearestServices ?? CreateAnalysis();
        var recommendation = IncidentRecommendationSelector.Select(
            incidentType,
            nearest);
        var accessibilityLevel = recommendation is null
            ? AccessibilityLevel.Critical
            : AccessibilityLevel.Excellent;
        var calculation = IncidentPriorityCalculator.Calculate(
            incidentType,
            recommendation?.DistanceMeters,
            accessibilityLevel);

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
                IncidentRecommendationSelector.ResolveRelevantServiceType(
                    incidentType,
                    recommendation),
                recommendation?.DistanceMeters,
                accessibilityLevel));
    }

    private static NearestEmergencyServicesResult CreateAnalysis() =>
        new(
            new SelectedLocationDto(41.04, 29.01),
            new NearestEmergencyServiceDto(
                3,
                "Hospital",
                "way/3",
                41.041,
                29.011,
                150),
            new NearestEmergencyServiceDto(
                2,
                "Fire Station",
                "way/2",
                41.045,
                29.015,
                600));

    private sealed class StubIncidentPriorityService(
        IncidentPriorityAnalysis result)
        : IIncidentPriorityService
    {
        public int CallCount { get; private set; }

        public Task<IncidentPriorityAnalysis> AnalyzeAsync(
            double latitude,
            double longitude,
            IncidentType incidentType,
            CancellationToken cancellationToken)
        {
            CallCount += 1;
            return Task.FromResult(result);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
