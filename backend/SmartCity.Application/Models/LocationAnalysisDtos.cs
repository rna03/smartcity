namespace SmartCity.Application.Models;

public sealed record SelectedLocationDto(
    double Latitude,
    double Longitude);

public sealed record NearestEmergencyServiceDto(
    int Id,
    string Name,
    string ExternalId,
    double Latitude,
    double Longitude,
    double DistanceMeters);

public sealed record NearestEmergencyServicesResult(
    SelectedLocationDto SelectedLocation,
    NearestEmergencyServiceDto? NearestHospital,
    NearestEmergencyServiceDto? NearestFireStation);

public static class LocationCoordinateValidation
{
    public static bool IsValid(double? latitude, double? longitude) =>
        latitude.HasValue &&
        longitude.HasValue &&
        double.IsFinite(latitude.Value) &&
        double.IsFinite(longitude.Value) &&
        latitude.Value is >= -90 and <= 90 &&
        longitude.Value is >= -180 and <= 180;
}
