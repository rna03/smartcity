using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using SmartCity.Domain;
using SmartCity.Domain.Entities;
using SmartCity.Infrastructure.Persistence;

namespace SmartCity.Infrastructure.Tests.Persistence;

public sealed class DashboardAnalyticsServiceTests
{
    private static readonly DateTimeOffset BaselineUtc =
        new(2026, 9, 13, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SummaryReturnsTotalIncidentCount()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Incidents.AddRange(
            CreateIncident(IncidentType.Fire, BaselineUtc),
            CreateIncident(IncidentType.Medical, BaselineUtc.AddMinutes(1)),
            CreateIncident(IncidentType.Accident, BaselineUtc.AddMinutes(2)));
        await dbContext.SaveChangesAsync();
        var service = new DashboardAnalyticsService(dbContext);

        var result = await service.GetSummaryAsync(CancellationToken.None);

        Assert.Equal(3, result.TotalIncidents);
    }

    [Fact]
    public async Task SummaryGroupsAllSupportedIncidentTypes()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Incidents.AddRange(
            CreateIncident(IncidentType.Fire, BaselineUtc),
            CreateIncident(IncidentType.Fire, BaselineUtc.AddMinutes(1)),
            CreateIncident(IncidentType.Medical, BaselineUtc.AddMinutes(2)),
            CreateIncident(IncidentType.Accident, BaselineUtc.AddMinutes(3)),
            CreateIncident(IncidentType.Other, BaselineUtc.AddMinutes(4)),
            CreateIncident(IncidentType.Other, BaselineUtc.AddMinutes(5)),
            CreateIncident(IncidentType.Other, BaselineUtc.AddMinutes(6)));
        await dbContext.SaveChangesAsync();
        var service = new DashboardAnalyticsService(dbContext);

        var result = await service.GetSummaryAsync(CancellationToken.None);

        Assert.Equal(2, result.IncidentCounts.Fire);
        Assert.Equal(1, result.IncidentCounts.Medical);
        Assert.Equal(1, result.IncidentCounts.Accident);
        Assert.Equal(3, result.IncidentCounts.Other);
    }

    [Fact]
    public async Task SummaryGroupsAllSupportedPriorityLevels()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Incidents.AddRange(
            CreateIncident(
                IncidentType.Fire,
                BaselineUtc,
                PriorityLevel.Critical,
                90),
            CreateIncident(
                IncidentType.Medical,
                BaselineUtc.AddMinutes(1),
                PriorityLevel.Critical,
                80),
            CreateIncident(
                IncidentType.Accident,
                BaselineUtc.AddMinutes(2),
                PriorityLevel.High,
                60),
            CreateIncident(
                IncidentType.Other,
                BaselineUtc.AddMinutes(3),
                PriorityLevel.High,
                55),
            CreateIncident(
                IncidentType.Fire,
                BaselineUtc.AddMinutes(4),
                PriorityLevel.High,
                50),
            CreateIncident(
                IncidentType.Medical,
                BaselineUtc.AddMinutes(5),
                PriorityLevel.Medium,
                40),
            CreateIncident(
                IncidentType.Accident,
                BaselineUtc.AddMinutes(6),
                PriorityLevel.Low,
                20),
            CreateIncident(
                IncidentType.Other,
                BaselineUtc.AddMinutes(7),
                PriorityLevel.Low,
                25));
        await dbContext.SaveChangesAsync();
        var service = new DashboardAnalyticsService(dbContext);

        var result = await service.GetSummaryAsync(CancellationToken.None);

        Assert.Equal(2, result.PriorityCounts.Critical);
        Assert.Equal(3, result.PriorityCounts.High);
        Assert.Equal(1, result.PriorityCounts.Medium);
        Assert.Equal(2, result.PriorityCounts.Low);
    }

    [Fact]
    public async Task LatestIncidentsAreOrderedNewestFirst()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Incidents.AddRange(
            CreateIncident(IncidentType.Fire, BaselineUtc.AddMinutes(-5)),
            CreateIncident(IncidentType.Medical, BaselineUtc.AddMinutes(5)),
            CreateIncident(IncidentType.Accident, BaselineUtc));
        await dbContext.SaveChangesAsync();
        var service = new DashboardAnalyticsService(dbContext);

        var result = await service.GetSummaryAsync(CancellationToken.None);

        Assert.Equal(
            [IncidentType.Medical, IncidentType.Accident, IncidentType.Fire],
            result.LatestIncidents.Select(incident => incident.Type));
        Assert.True(result.LatestIncidents
            .Select(incident => incident.CreatedAtUtc)
            .SequenceEqual(result.LatestIncidents
                .Select(incident => incident.CreatedAtUtc)
                .OrderByDescending(createdAt => createdAt)));
    }

    [Fact]
    public async Task LatestIncidentsIncludeStoredPriority()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Incidents.Add(CreateIncident(
            IncidentType.Fire,
            BaselineUtc,
            PriorityLevel.High,
            55));
        await dbContext.SaveChangesAsync();
        var service = new DashboardAnalyticsService(dbContext);

        var result = await service.GetSummaryAsync(CancellationToken.None);

        var latest = Assert.Single(result.LatestIncidents);
        Assert.Equal(55, latest.PriorityScore);
        Assert.Equal(PriorityLevel.High, latest.PriorityLevel);
    }

    [Fact]
    public async Task LatestIncidentsAreLimitedToFive()
    {
        await using var dbContext = CreateDbContext();
        for (var index = 0; index < 7; index += 1)
        {
            dbContext.Incidents.Add(CreateIncident(
                IncidentType.Other,
                BaselineUtc.AddMinutes(index)));
        }

        await dbContext.SaveChangesAsync();
        var service = new DashboardAnalyticsService(dbContext);

        var result = await service.GetSummaryAsync(CancellationToken.None);

        Assert.Equal(DashboardAnalyticsService.LatestIncidentLimit, result.LatestIncidents.Count);
        Assert.Equal(BaselineUtc.AddMinutes(6), result.LatestIncidents[0].CreatedAtUtc);
        Assert.Equal(BaselineUtc.AddMinutes(2), result.LatestIncidents[^1].CreatedAtUtc);
    }

    [Fact]
    public async Task EmptyDatabaseReturnsZeroCountsAndNoLatestIncidents()
    {
        await using var dbContext = CreateDbContext();
        var service = new DashboardAnalyticsService(dbContext);

        var result = await service.GetSummaryAsync(CancellationToken.None);

        Assert.Equal(0, result.TotalIncidents);
        Assert.Equal(0, result.IncidentCounts.Fire);
        Assert.Equal(0, result.IncidentCounts.Medical);
        Assert.Equal(0, result.IncidentCounts.Accident);
        Assert.Equal(0, result.IncidentCounts.Other);
        Assert.Equal(0, result.PriorityCounts.Critical);
        Assert.Equal(0, result.PriorityCounts.High);
        Assert.Equal(0, result.PriorityCounts.Medium);
        Assert.Equal(0, result.PriorityCounts.Low);
        Assert.Empty(result.LatestIncidents);
    }

    private static SmartCityDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SmartCityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new SmartCityDbContext(options);
    }

    private static Incident CreateIncident(
        IncidentType incidentType,
        DateTimeOffset occurredAt,
        PriorityLevel priorityLevel = PriorityLevel.Low,
        int priorityScore = 0) =>
        new()
        {
            IncidentType = incidentType,
            Description = $"{incidentType} dashboard test",
            PriorityScore = priorityScore,
            PriorityLevel = priorityLevel,
            OccurredAt = occurredAt,
            Geometry = new Point(29.01, 41.04) { SRID = 4326 }
        };
}
