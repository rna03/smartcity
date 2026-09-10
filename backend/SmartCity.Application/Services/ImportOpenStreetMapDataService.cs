using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartCity.Application.Abstractions;
using SmartCity.Application.Configuration;
using SmartCity.Application.Models;

namespace SmartCity.Application.Services;

public sealed partial class ImportOpenStreetMapDataService(
    IOpenStreetMapDataSource dataSource,
    ISpatialDataStore dataStore,
    IOptions<PilotAreaOptions> pilotAreaOptions,
    ILogger<ImportOpenStreetMapDataService> logger)
    : IImportOpenStreetMapDataService
{
    public async Task<OpenStreetMapImportResult> ImportAsync(
        CancellationToken cancellationToken)
    {
        var pilotArea = pilotAreaOptions.Value;
        var boundingBox = pilotArea.ToBoundingBox();

        LogImportStarted(
            logger,
            pilotArea.Name,
            boundingBox.South,
            boundingBox.West,
            boundingBox.North,
            boundingBox.East);

        var dataset = await dataSource.FetchAsync(boundingBox, cancellationToken);

        LogFeaturesFound(
            logger,
            dataset.Hospitals.Count,
            dataset.FireStations.Count,
            dataset.Roads.Count);

        var added = await dataStore.AddNewAsync(dataset, cancellationToken);
        var result = new OpenStreetMapImportResult(
            pilotArea.Name,
            dataset.Hospitals.Count,
            added.HospitalsAdded,
            dataset.FireStations.Count,
            added.FireStationsAdded,
            dataset.Roads.Count,
            added.RoadsAdded);

        LogImportCompleted(
            logger,
            pilotArea.Name,
            result.HospitalsAdded,
            result.FireStationsAdded,
            result.RoadsAdded);

        return result;
    }

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "OpenStreetMap import started for {PilotArea}. Bounding box: " +
                  "South={South}, West={West}, North={North}, East={East}")]
    private static partial void LogImportStarted(
        ILogger logger,
        string pilotArea,
        double south,
        double west,
        double north,
        double east);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "OpenStreetMap returned {HospitalCount} hospitals, " +
                  "{FireStationCount} fire stations and {RoadCount} roads")]
    private static partial void LogFeaturesFound(
        ILogger logger,
        int hospitalCount,
        int fireStationCount,
        int roadCount);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Information,
        Message = "OpenStreetMap import completed for {PilotArea}. Added {HospitalsAdded} hospitals, " +
                  "{FireStationsAdded} fire stations and {RoadsAdded} roads")]
    private static partial void LogImportCompleted(
        ILogger logger,
        string pilotArea,
        int hospitalsAdded,
        int fireStationsAdded,
        int roadsAdded);
}
