using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using SmartCity.Application.Abstractions;
using SmartCity.Application.Exceptions;
using SmartCity.Application.Models;
using SmartCity.Domain;

namespace SmartCity.Infrastructure.Persistence;

internal sealed class OpenStreetMapSpatialDataStore(SmartCityDbContext dbContext)
    : ISpatialDataStore, ISpatialDataQueryService
{
    public async Task<SpatialImportCounts> AddNewAsync(
        OpenStreetMapDataset dataset,
        CancellationToken cancellationToken)
    {
        try
        {
            var hospitalIds = dataset.Hospitals.Select(item => item.ExternalId).ToArray();
            var fireStationIds = dataset.FireStations.Select(item => item.ExternalId).ToArray();
            var roadIds = dataset.Roads.Select(item => item.ExternalId).ToArray();

            var existingHospitalIds = await dbContext.Hospitals
                .Where(item => item.Source == DataSourceNames.OpenStreetMap &&
                               hospitalIds.Contains(item.ExternalId))
                .Select(item => item.ExternalId)
                .ToHashSetAsync(cancellationToken);
            var existingFireStationIds = await dbContext.FireStations
                .Where(item => item.Source == DataSourceNames.OpenStreetMap &&
                               fireStationIds.Contains(item.ExternalId))
                .Select(item => item.ExternalId)
                .ToHashSetAsync(cancellationToken);
            var existingRoadIds = await dbContext.Roads
                .Where(item => item.Source == DataSourceNames.OpenStreetMap &&
                               roadIds.Contains(item.ExternalId))
                .Select(item => item.ExternalId)
                .ToHashSetAsync(cancellationToken);

            var hospitalsToAdd = dataset.Hospitals
                .Where(item => !existingHospitalIds.Contains(item.ExternalId))
                .ToArray();
            var fireStationsToAdd = dataset.FireStations
                .Where(item => !existingFireStationIds.Contains(item.ExternalId))
                .ToArray();
            var roadsToAdd = dataset.Roads
                .Where(item => !existingRoadIds.Contains(item.ExternalId))
                .ToArray();

            dbContext.Hospitals.AddRange(hospitalsToAdd);
            dbContext.FireStations.AddRange(fireStationsToAdd);
            dbContext.Roads.AddRange(roadsToAdd);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new SpatialImportCounts(
                hospitalsToAdd.Length,
                fireStationsToAdd.Length,
                roadsToAdd.Length);
        }
        catch (Exception exception) when (IsDatabaseFailure(exception))
        {
            throw new DataPersistenceException(
                "The database operation failed during OpenStreetMap import.",
                exception);
        }
    }

    public async Task<IReadOnlyList<PointFeatureDto>> GetHospitalsAsync(
        CancellationToken cancellationToken) =>
        await QueryPointsAsync(
            () => dbContext.Hospitals.AsNoTracking().OrderBy(item => item.Name)
                .Select(item => new PointFeatureDto(
                    item.Id,
                    item.Name,
                    item.Source,
                    item.ExternalId,
                    item.Geometry.X,
                    item.Geometry.Y))
                .ToListAsync(cancellationToken));

    public async Task<IReadOnlyList<PointFeatureDto>> GetFireStationsAsync(
        CancellationToken cancellationToken) =>
        await QueryPointsAsync(
            () => dbContext.FireStations.AsNoTracking().OrderBy(item => item.Name)
                .Select(item => new PointFeatureDto(
                    item.Id,
                    item.Name,
                    item.Source,
                    item.ExternalId,
                    item.Geometry.X,
                    item.Geometry.Y))
                .ToListAsync(cancellationToken));

    public async Task<IReadOnlyList<RoadFeatureDto>> GetRoadsAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var roads = await dbContext.Roads
                .AsNoTracking()
                .OrderBy(item => item.Name)
                .ToListAsync(cancellationToken);

            return roads.Select(road => new RoadFeatureDto(
                    road.Id,
                    road.Name,
                    road.RoadType,
                    road.Source,
                    road.ExternalId,
                    road.Geometry.Coordinates
                        .Select(coordinate => new CoordinateDto(coordinate.X, coordinate.Y))
                        .ToArray()))
                .ToArray();
        }
        catch (Exception exception) when (IsDatabaseFailure(exception))
        {
            throw new DataPersistenceException("The database query failed.", exception);
        }
    }

    private static async Task<IReadOnlyList<PointFeatureDto>> QueryPointsAsync(
        Func<Task<List<PointFeatureDto>>> query)
    {
        try
        {
            return await query();
        }
        catch (Exception exception) when (IsDatabaseFailure(exception))
        {
            throw new DataPersistenceException("The database query failed.", exception);
        }
    }

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
