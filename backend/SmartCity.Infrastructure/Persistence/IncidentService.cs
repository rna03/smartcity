using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using SmartCity.Application.Abstractions;
using SmartCity.Application.Exceptions;
using SmartCity.Application.Models;
using SmartCity.Domain;
using SmartCity.Domain.Entities;

namespace SmartCity.Infrastructure.Persistence;

internal sealed class IncidentService(
    SmartCityDbContext dbContext,
    IIncidentPriorityService incidentPriorityService,
    TimeProvider timeProvider)
    : IIncidentService
{
    public async Task<IncidentCreationResult> CreateAsync(
        IncidentType incidentType,
        double latitude,
        double longitude,
        string? description,
        CancellationToken cancellationToken)
    {
        var analysis = await incidentPriorityService.AnalyzeAsync(
            latitude,
            longitude,
            incidentType,
            cancellationToken);
        var incident = new Incident
        {
            IncidentType = incidentType,
            Description = IncidentRequestValidation.NormalizeDescription(description),
            PriorityScore = analysis.Priority.Score,
            PriorityLevel = analysis.Priority.Level,
            OccurredAt = timeProvider.GetUtcNow(),
            // NetTopologySuite/PostGIS use X=longitude and Y=latitude.
            Geometry = new Point(longitude, latitude) { SRID = 4326 }
        };

        try
        {
            dbContext.Incidents.Add(incident);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (IsDatabaseFailure(exception))
        {
            throw new DataPersistenceException(
                "The incident could not be saved to the database.",
                exception);
        }

        return new IncidentCreationResult(
            ToDto(incident),
            analysis.RecommendedService,
            analysis.Priority);
    }

    public async Task<IReadOnlyList<IncidentDto>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var incidents = await dbContext.Incidents
                .AsNoTracking()
                .OrderByDescending(incident => incident.OccurredAt)
                .ToListAsync(cancellationToken);

            return incidents.Select(ToDto).ToArray();
        }
        catch (Exception exception) when (IsDatabaseFailure(exception))
        {
            throw new DataPersistenceException(
                "The incident query failed.",
                exception);
        }
    }

    private static IncidentDto ToDto(Incident incident) =>
        new(
            incident.Id,
            incident.IncidentType,
            incident.Geometry.Y,
            incident.Geometry.X,
            incident.Description,
            incident.PriorityScore,
            incident.PriorityLevel,
            incident.OccurredAt);

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
