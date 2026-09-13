using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using SmartCity.Application.Abstractions;
using SmartCity.Application.Exceptions;
using SmartCity.Application.Models;
using SmartCity.Domain;

namespace SmartCity.Infrastructure.Persistence;

internal sealed class DashboardAnalyticsService(SmartCityDbContext dbContext)
    : IDashboardAnalyticsService
{
    internal const int LatestIncidentLimit = 5;

    public async Task<DashboardSummaryResponse> GetSummaryAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var incidents = dbContext.Incidents.AsNoTracking();
            var totalIncidents = await incidents.CountAsync(cancellationToken);
            var groupedCounts = await incidents
                .GroupBy(incident => incident.IncidentType)
                .Select(group => new
                {
                    Type = group.Key,
                    Count = group.Count()
                })
                .ToDictionaryAsync(
                    item => item.Type,
                    item => item.Count,
                    cancellationToken);
            var latestIncidents = await incidents
                .OrderByDescending(incident => incident.OccurredAt)
                .ThenByDescending(incident => incident.Id)
                .Take(LatestIncidentLimit)
                .Select(incident => new DashboardIncidentItem(
                    incident.Id,
                    incident.IncidentType,
                    incident.Geometry.Y,
                    incident.Geometry.X,
                    incident.Description,
                    incident.OccurredAt))
                .ToListAsync(cancellationToken);

            return new DashboardSummaryResponse(
                totalIncidents,
                new IncidentTypeCount(
                    GetCount(groupedCounts, IncidentType.Fire),
                    GetCount(groupedCounts, IncidentType.Medical),
                    GetCount(groupedCounts, IncidentType.Accident),
                    GetCount(groupedCounts, IncidentType.Other)),
                latestIncidents);
        }
        catch (Exception exception) when (IsDatabaseFailure(exception))
        {
            throw new DataPersistenceException(
                "The dashboard analytics query failed.",
                exception);
        }
    }

    private static int GetCount(
        IReadOnlyDictionary<IncidentType, int> counts,
        IncidentType incidentType) =>
        counts.GetValueOrDefault(incidentType);

    private static bool IsDatabaseFailure(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is DbException or DbUpdateException)
            {
                return true;
            }
        }

        return false;
    }
}
