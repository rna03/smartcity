using SmartCity.Application.Models;
using SmartCity.Domain;

namespace SmartCity.Application.Services;

public static class IncidentPriorityCalculator
{
    public const int MaximumPriorityScore = 100;

    public static IncidentPriorityCalculation Calculate(
        IncidentType incidentType,
        double? relevantServiceDistanceMeters,
        AccessibilityLevel accessibilityLevel)
    {
        var incidentTypeBaseScore = CalculateIncidentTypeBaseScore(incidentType);
        var serviceDistanceScore = CalculateServiceDistanceScore(
            relevantServiceDistanceMeters);
        var accessibilityPenalty = CalculateAccessibilityPenalty(accessibilityLevel);
        var score = Math.Clamp(
            incidentTypeBaseScore + serviceDistanceScore + accessibilityPenalty,
            0,
            MaximumPriorityScore);

        return new IncidentPriorityCalculation(
            score,
            CalculatePriorityLevel(score),
            incidentTypeBaseScore,
            serviceDistanceScore,
            accessibilityPenalty);
    }

    public static int CalculateIncidentTypeBaseScore(IncidentType incidentType) =>
        incidentType switch
        {
            IncidentType.Fire => 40,
            IncidentType.Medical => 35,
            IncidentType.Accident => 30,
            IncidentType.Other => 20,
            _ => throw new ArgumentOutOfRangeException(nameof(incidentType))
        };

    public static int CalculateServiceDistanceScore(double? distanceMeters)
    {
        if (!distanceMeters.HasValue ||
            !double.IsFinite(distanceMeters.Value) ||
            distanceMeters.Value < 0)
        {
            return 50;
        }

        return distanceMeters.Value switch
        {
            <= 1_000 => 0,
            <= 2_000 => 10,
            <= 3_000 => 20,
            <= 5_000 => 30,
            _ => 40
        };
    }

    public static int CalculateAccessibilityPenalty(
        AccessibilityLevel accessibilityLevel) =>
        accessibilityLevel switch
        {
            AccessibilityLevel.Excellent => 0,
            AccessibilityLevel.Good => 5,
            AccessibilityLevel.Moderate => 10,
            AccessibilityLevel.Poor => 15,
            AccessibilityLevel.Critical => 20,
            _ => throw new ArgumentOutOfRangeException(nameof(accessibilityLevel))
        };

    public static PriorityLevel CalculatePriorityLevel(int priorityScore)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(priorityScore);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            priorityScore,
            MaximumPriorityScore);

        return priorityScore switch
        {
            >= 70 => PriorityLevel.Critical,
            >= 50 => PriorityLevel.High,
            >= 30 => PriorityLevel.Medium,
            _ => PriorityLevel.Low
        };
    }
}
