using Microsoft.EntityFrameworkCore;
using SmartCity.Application.Abstractions;
using SmartCity.Application.Models;
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
        var analysisService = new StubLocationAnalysisService(CreateAnalysis());
        var service = new IncidentService(
            dbContext,
            analysisService,
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
        Assert.Equal(1, analysisService.CallCount);
    }

    [Fact]
    public async Task OtherSelectsClosestAvailableService()
    {
        await using var dbContext = CreateDbContext();
        var service = new IncidentService(
            dbContext,
            new StubLocationAnalysisService(CreateAnalysis()),
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
            new StubLocationAnalysisService(analysis),
            new FixedTimeProvider(FixedUtc));

        var result = await service.CreateAsync(
            IncidentType.Fire,
            41.04,
            29.01,
            null,
            CancellationToken.None);

        Assert.Null(result.RecommendedService);
        Assert.Single(await dbContext.Incidents.ToListAsync());
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
            new StubLocationAnalysisService(CreateAnalysis()),
            new FixedTimeProvider(FixedUtc));

        var result = await service.GetAllAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal(IncidentType.Medical, result[0].Type);
        Assert.Equal(IncidentType.Fire, result[1].Type);
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
        DateTimeOffset occurredAt) =>
        new()
        {
            IncidentType = incidentType,
            OccurredAt = occurredAt,
            Geometry = new NetTopologySuite.Geometries.Point(29.01, 41.04)
            {
                SRID = 4326
            }
        };

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

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
