using SmartCity.Domain;

namespace SmartCity.Application.Models;

public sealed record CreateIncidentRequest(
    IncidentType? Type,
    double? Latitude,
    double? Longitude,
    string? Description);

public sealed record IncidentDto(
    int Id,
    IncidentType Type,
    double Latitude,
    double Longitude,
    string? Description,
    DateTimeOffset CreatedAtUtc);

public enum EmergencyServiceType
{
    Hospital = 1,
    FireStation = 2
}

public sealed record RecommendedEmergencyServiceDto(
    EmergencyServiceType ServiceType,
    int Id,
    string Name,
    string ExternalId,
    double Latitude,
    double Longitude,
    double DistanceMeters);

public sealed record IncidentCreationResult(
    IncidentDto Incident,
    RecommendedEmergencyServiceDto? RecommendedService);

public static class IncidentRequestValidation
{
    public const int MaxDescriptionLength = 500;

    public static bool IsValid(CreateIncidentRequest? request) =>
        request is not null &&
        request.Type.HasValue &&
        Enum.IsDefined(request.Type.Value) &&
        LocationCoordinateValidation.IsValid(request.Latitude, request.Longitude) &&
        (request.Description is null ||
         request.Description.Length <= MaxDescriptionLength);

    public static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
