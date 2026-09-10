namespace SmartCity.Application.Models;

public sealed record OpenStreetMapImportResult(
    string PilotArea,
    int HospitalsFound,
    int HospitalsAdded,
    int FireStationsFound,
    int FireStationsAdded,
    int RoadsFound,
    int RoadsAdded);
