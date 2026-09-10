using SmartCity.Domain.Entities;

namespace SmartCity.Application.Models;

public sealed record OpenStreetMapDataset(
    IReadOnlyList<Hospital> Hospitals,
    IReadOnlyList<FireStation> FireStations,
    IReadOnlyList<Road> Roads);

public readonly record struct SpatialImportCounts(
    int HospitalsAdded,
    int FireStationsAdded,
    int RoadsAdded);
