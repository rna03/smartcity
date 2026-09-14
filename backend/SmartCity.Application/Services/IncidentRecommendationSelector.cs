using SmartCity.Application.Models;
using SmartCity.Domain;

namespace SmartCity.Application.Services;

public static class IncidentRecommendationSelector
{
    public static RecommendedEmergencyServiceDto? Select(
        IncidentType incidentType,
        NearestEmergencyServicesResult analysis) =>
        incidentType switch
        {
            IncidentType.Fire => ToRecommendation(
                EmergencyServiceType.FireStation,
                analysis.NearestFireStation),
            IncidentType.Medical or IncidentType.Accident => ToRecommendation(
                EmergencyServiceType.Hospital,
                analysis.NearestHospital),
            IncidentType.Other => SelectClosestAvailable(analysis),
            _ => throw new ArgumentOutOfRangeException(nameof(incidentType))
        };

    public static EmergencyServiceType? ResolveRelevantServiceType(
        IncidentType incidentType,
        RecommendedEmergencyServiceDto? recommendation) =>
        recommendation?.ServiceType ?? incidentType switch
        {
            IncidentType.Fire => EmergencyServiceType.FireStation,
            IncidentType.Medical or IncidentType.Accident =>
                EmergencyServiceType.Hospital,
            IncidentType.Other => null,
            _ => throw new ArgumentOutOfRangeException(nameof(incidentType))
        };

    private static RecommendedEmergencyServiceDto? SelectClosestAvailable(
        NearestEmergencyServicesResult analysis)
    {
        var hospital = analysis.NearestHospital;
        var fireStation = analysis.NearestFireStation;

        if (hospital is null)
        {
            return ToRecommendation(EmergencyServiceType.FireStation, fireStation);
        }

        if (fireStation is null ||
            hospital.DistanceMeters <= fireStation.DistanceMeters)
        {
            return ToRecommendation(EmergencyServiceType.Hospital, hospital);
        }

        return ToRecommendation(EmergencyServiceType.FireStation, fireStation);
    }

    private static RecommendedEmergencyServiceDto? ToRecommendation(
        EmergencyServiceType serviceType,
        NearestEmergencyServiceDto? service) =>
        service is null
            ? null
            : new RecommendedEmergencyServiceDto(
                serviceType,
                service.Id,
                service.Name,
                service.ExternalId,
                service.Latitude,
                service.Longitude,
                service.DistanceMeters);
}
