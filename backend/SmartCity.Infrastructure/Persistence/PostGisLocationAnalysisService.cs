using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using SmartCity.Application.Abstractions;
using SmartCity.Application.Exceptions;
using SmartCity.Application.Models;

namespace SmartCity.Infrastructure.Persistence;

internal sealed class PostGisLocationAnalysisService(SmartCityDbContext dbContext)
    : ILocationAnalysisService
{
    public async Task<NearestEmergencyServicesResult> FindNearestAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;

        try
        {
            if (openedHere)
            {
                await connection.OpenAsync(cancellationToken);
            }

            var hospital = await QueryNearestAsync(
                connection,
                LocationAnalysisSql.NearestHospital,
                latitude,
                longitude,
                cancellationToken);
            var fireStation = await QueryNearestAsync(
                connection,
                LocationAnalysisSql.NearestFireStation,
                latitude,
                longitude,
                cancellationToken);

            return new NearestEmergencyServicesResult(
                new SelectedLocationDto(latitude, longitude),
                hospital,
                fireStation);
        }
        catch (DbException exception)
        {
            throw new DataPersistenceException(
                "The nearest emergency service analysis query failed.",
                exception);
        }
        finally
        {
            if (openedHere && connection.State != ConnectionState.Closed)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task<NearestEmergencyServiceDto?> QueryNearestAsync(
        DbConnection connection,
        string commandText,
        double latitude,
        double longitude,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        AddParameter(command, "latitude", latitude);
        AddParameter(command, "longitude", longitude);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new NearestEmergencyServiceDto(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetDouble(3),
            reader.GetDouble(4),
            reader.GetDouble(5));
    }

    private static void AddParameter(DbCommand command, string name, double value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = DbType.Double;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}

internal static class LocationAnalysisSql
{
    internal const string NearestHospital =
        """
        SELECT
            "Id",
            "Name",
            "ExternalId",
            ST_Y("Geometry") AS latitude,
            ST_X("Geometry") AS longitude,
            ST_Distance("Geometry"::geography,
                        ST_SetSRID(ST_MakePoint(@longitude, @latitude), 4326)::geography)
                AS distance_meters
        FROM hospitals
        ORDER BY distance_meters
        LIMIT 1;
        """;

    internal const string NearestFireStation =
        """
        SELECT
            "Id",
            "Name",
            "ExternalId",
            ST_Y("Geometry") AS latitude,
            ST_X("Geometry") AS longitude,
            ST_Distance("Geometry"::geography,
                        ST_SetSRID(ST_MakePoint(@longitude, @latitude), 4326)::geography)
                AS distance_meters
        FROM fire_stations
        ORDER BY distance_meters
        LIMIT 1;
        """;
}
