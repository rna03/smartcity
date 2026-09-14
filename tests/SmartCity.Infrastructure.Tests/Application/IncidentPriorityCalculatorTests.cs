using SmartCity.Application.Models;
using SmartCity.Application.Services;
using SmartCity.Domain;

namespace SmartCity.Infrastructure.Tests.Application;

public sealed class IncidentPriorityCalculatorTests
{
    [Theory]
    [InlineData(IncidentType.Fire, 40)]
    [InlineData(IncidentType.Medical, 35)]
    [InlineData(IncidentType.Accident, 30)]
    [InlineData(IncidentType.Other, 20)]
    public void IncidentTypeReturnsExpectedBaseScore(
        IncidentType incidentType,
        int expectedScore)
    {
        Assert.Equal(
            expectedScore,
            IncidentPriorityCalculator.CalculateIncidentTypeBaseScore(incidentType));
    }

    [Theory]
    [InlineData(0d, 0)]
    [InlineData(500d, 0)]
    [InlineData(1_000d, 0)]
    [InlineData(1_000.01d, 10)]
    [InlineData(1_500d, 10)]
    [InlineData(2_000d, 10)]
    [InlineData(2_000.01d, 20)]
    [InlineData(2_500d, 20)]
    [InlineData(3_000d, 20)]
    [InlineData(3_000.01d, 30)]
    [InlineData(4_000d, 30)]
    [InlineData(5_000d, 30)]
    [InlineData(5_000.01d, 40)]
    [InlineData(6_000d, 40)]
    [InlineData(null, 50)]
    public void ServiceDistanceUsesExpectedInclusiveBoundaries(
        double? distanceMeters,
        int expectedScore)
    {
        Assert.Equal(
            expectedScore,
            IncidentPriorityCalculator.CalculateServiceDistanceScore(distanceMeters));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(-0.01d)]
    public void InvalidServiceDistanceIsTreatedAsUnavailable(double distanceMeters)
    {
        Assert.Equal(
            50,
            IncidentPriorityCalculator.CalculateServiceDistanceScore(distanceMeters));
    }

    [Theory]
    [InlineData(AccessibilityLevel.Excellent, 0)]
    [InlineData(AccessibilityLevel.Good, 5)]
    [InlineData(AccessibilityLevel.Moderate, 10)]
    [InlineData(AccessibilityLevel.Poor, 15)]
    [InlineData(AccessibilityLevel.Critical, 20)]
    public void AccessibilityLevelReturnsExpectedPenalty(
        AccessibilityLevel accessibilityLevel,
        int expectedPenalty)
    {
        Assert.Equal(
            expectedPenalty,
            IncidentPriorityCalculator.CalculateAccessibilityPenalty(accessibilityLevel));
    }

    [Theory]
    [InlineData(0, PriorityLevel.Low)]
    [InlineData(20, PriorityLevel.Low)]
    [InlineData(29, PriorityLevel.Low)]
    [InlineData(30, PriorityLevel.Medium)]
    [InlineData(35, PriorityLevel.Medium)]
    [InlineData(49, PriorityLevel.Medium)]
    [InlineData(50, PriorityLevel.High)]
    [InlineData(55, PriorityLevel.High)]
    [InlineData(69, PriorityLevel.High)]
    [InlineData(70, PriorityLevel.Critical)]
    [InlineData(80, PriorityLevel.Critical)]
    [InlineData(100, PriorityLevel.Critical)]
    public void PriorityLevelUsesExpectedInclusiveBoundaries(
        int score,
        PriorityLevel expectedLevel)
    {
        Assert.Equal(
            expectedLevel,
            IncidentPriorityCalculator.CalculatePriorityLevel(score));
    }

    [Fact]
    public void CalculateReturnsExplainableBreakdown()
    {
        var result = IncidentPriorityCalculator.Calculate(
            IncidentType.Fire,
            1_200,
            AccessibilityLevel.Good);

        Assert.Equal(55, result.Score);
        Assert.Equal(PriorityLevel.High, result.Level);
        Assert.Equal(40, result.IncidentTypeBaseScore);
        Assert.Equal(10, result.ServiceDistanceScore);
        Assert.Equal(5, result.AccessibilityPenalty);
    }

    [Fact]
    public void CalculateClampsScoreAboveMaximum()
    {
        var result = IncidentPriorityCalculator.Calculate(
            IncidentType.Fire,
            null,
            AccessibilityLevel.Critical);

        Assert.Equal(IncidentPriorityCalculator.MaximumPriorityScore, result.Score);
        Assert.Equal(PriorityLevel.Critical, result.Level);
        Assert.Equal(40, result.IncidentTypeBaseScore);
        Assert.Equal(50, result.ServiceDistanceScore);
        Assert.Equal(20, result.AccessibilityPenalty);
    }

    [Fact]
    public void InvalidIncidentTypeThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            IncidentPriorityCalculator.CalculateIncidentTypeBaseScore(
                (IncidentType)int.MaxValue));
    }

    [Fact]
    public void InvalidAccessibilityLevelThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            IncidentPriorityCalculator.CalculateAccessibilityPenalty(
                (AccessibilityLevel)int.MaxValue));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void OutOfRangePriorityScoreThrows(int score)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            IncidentPriorityCalculator.CalculatePriorityLevel(score));
    }
}
