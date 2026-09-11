using SmartCity.Application.Models;
using SmartCity.Application.Services;

namespace SmartCity.Infrastructure.Tests.Application;

public sealed class AccessibilityScoreCalculatorTests
{
    [Theory]
    [InlineData(900d, 50)]
    [InlineData(1_500d, 45)]
    [InlineData(2_500d, 35)]
    [InlineData(4_000d, 25)]
    [InlineData(6_000d, 10)]
    [InlineData(null, 0)]
    public void ServiceDistanceReturnsExpectedScore(
        double? distanceMeters,
        int expectedScore)
    {
        Assert.Equal(
            expectedScore,
            AccessibilityScoreCalculator.CalculateServiceScore(distanceMeters));
    }

    [Theory]
    [InlineData(1_000, 50)]
    [InlineData(1_000.01, 45)]
    [InlineData(2_000, 45)]
    [InlineData(2_000.01, 35)]
    [InlineData(3_000, 35)]
    [InlineData(3_000.01, 25)]
    [InlineData(5_000, 25)]
    [InlineData(5_000.01, 10)]
    public void ServiceScoreThresholdsUseInclusiveUpperBounds(
        double distanceMeters,
        int expectedScore)
    {
        Assert.Equal(
            expectedScore,
            AccessibilityScoreCalculator.CalculateServiceScore(distanceMeters));
    }

    [Theory]
    [InlineData(90, AccessibilityLevel.Excellent)]
    [InlineData(70, AccessibilityLevel.Good)]
    [InlineData(50, AccessibilityLevel.Moderate)]
    [InlineData(30, AccessibilityLevel.Poor)]
    [InlineData(10, AccessibilityLevel.Critical)]
    [InlineData(0, AccessibilityLevel.Critical)]
    public void TotalScoreReturnsExpectedAccessibilityLevel(
        int totalScore,
        AccessibilityLevel expectedLevel)
    {
        Assert.Equal(
            expectedLevel,
            AccessibilityScoreCalculator.CalculateLevel(totalScore));
    }

    [Theory]
    [InlineData(80, AccessibilityLevel.Excellent)]
    [InlineData(79, AccessibilityLevel.Good)]
    [InlineData(60, AccessibilityLevel.Good)]
    [InlineData(59, AccessibilityLevel.Moderate)]
    [InlineData(40, AccessibilityLevel.Moderate)]
    [InlineData(39, AccessibilityLevel.Poor)]
    [InlineData(20, AccessibilityLevel.Poor)]
    [InlineData(19, AccessibilityLevel.Critical)]
    public void AccessibilityLevelThresholdsUseInclusiveLowerBounds(
        int totalScore,
        AccessibilityLevel expectedLevel)
    {
        Assert.Equal(
            expectedLevel,
            AccessibilityScoreCalculator.CalculateLevel(totalScore));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(-1d)]
    public void InvalidServiceDistanceReturnsZero(double distanceMeters)
    {
        Assert.Equal(
            0,
            AccessibilityScoreCalculator.CalculateServiceScore(distanceMeters));
    }
}
