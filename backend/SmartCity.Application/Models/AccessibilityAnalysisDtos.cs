namespace SmartCity.Application.Models;

public enum AccessibilityLevel
{
    Excellent = 1,
    Good = 2,
    Moderate = 3,
    Poor = 4,
    Critical = 5
}

public sealed record AccessibilityServiceScore(
    double? DistanceMeters,
    int Score);

public sealed record AccessibilityAnalysisResponse(
    SelectedLocationDto SelectedLocation,
    AccessibilityServiceScore Hospital,
    AccessibilityServiceScore FireStation,
    int TotalScore,
    AccessibilityLevel AccessibilityLevel);
