using SmartCity.Application.Models;

namespace SmartCity.Application.Services;

public static class AccessibilityScoreCalculator
{
    public const int MaximumServiceScore = 50;
    public const int MaximumTotalScore = 100;

    public static int CalculateServiceScore(double? distanceMeters)
    {
        if (!distanceMeters.HasValue ||
            !double.IsFinite(distanceMeters.Value) ||
            distanceMeters.Value < 0)
        {
            return 0;
        }

        return distanceMeters.Value switch
        {
            <= 1_000 => 50,
            <= 2_000 => 45,
            <= 3_000 => 35,
            <= 5_000 => 25,
            _ => 10
        };
    }

    public static AccessibilityLevel CalculateLevel(int totalScore)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(totalScore);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            totalScore,
            MaximumTotalScore);

        return totalScore switch
        {
            >= 80 => AccessibilityLevel.Excellent,
            >= 60 => AccessibilityLevel.Good,
            >= 40 => AccessibilityLevel.Moderate,
            >= 20 => AccessibilityLevel.Poor,
            _ => AccessibilityLevel.Critical
        };
    }
}
