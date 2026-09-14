using SmartCity.Domain;

namespace SmartCity.Application.Models;

public sealed record IncidentPriorityCalculation(
    int Score,
    PriorityLevel Level,
    int IncidentTypeBaseScore,
    int ServiceDistanceScore,
    int AccessibilityPenalty);

public sealed record IncidentPriorityResult(
    int Score,
    PriorityLevel Level,
    int IncidentTypeBaseScore,
    int ServiceDistanceScore,
    int AccessibilityPenalty,
    EmergencyServiceType? RelevantServiceType,
    double? RelevantServiceDistanceMeters,
    AccessibilityLevel AccessibilityLevel);

public sealed record IncidentPriorityAnalysis(
    SelectedLocationDto SelectedLocation,
    IncidentType IncidentType,
    RecommendedEmergencyServiceDto? RecommendedService,
    IncidentPriorityResult Priority);

public sealed record IncidentPriorityRelevantService(
    EmergencyServiceType? ServiceType,
    double? DistanceMeters);

public sealed record IncidentPriorityBreakdown(
    int IncidentTypeBaseScore,
    int ServiceDistanceScore,
    int AccessibilityPenalty);

public sealed record IncidentPriorityPreviewResponse(
    SelectedLocationDto SelectedLocation,
    IncidentType IncidentType,
    int PriorityScore,
    PriorityLevel PriorityLevel,
    IncidentPriorityRelevantService RelevantService,
    AccessibilityLevel AccessibilityLevel,
    IncidentPriorityBreakdown Breakdown);
