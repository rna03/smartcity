using SmartCity.Application.Models;

namespace SmartCity.Infrastructure.Tests.Application;

public sealed class CoverageClassifierTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(2_000)]
    public void DistanceAtOrBelowTwoKilometersIsGood(double distanceMeters)
    {
        Assert.Equal(CoverageLevel.Good, CoverageClassifier.Classify(distanceMeters));
    }

    [Theory]
    [InlineData(2_000.01)]
    [InlineData(5_000)]
    public void DistanceBetweenTwoAndFiveKilometersIsModerate(double distanceMeters)
    {
        Assert.Equal(
            CoverageLevel.Moderate,
            CoverageClassifier.Classify(distanceMeters));
    }

    [Fact]
    public void DistanceAboveFiveKilometersIsPoor()
    {
        Assert.Equal(CoverageLevel.Poor, CoverageClassifier.Classify(5_000.01));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(double.NaN)]
    [InlineData(-1d)]
    public void MissingOrInvalidDistanceIsUnavailable(double? distanceMeters)
    {
        Assert.Equal(
            CoverageLevel.Unavailable,
            CoverageClassifier.Classify(distanceMeters));
    }

    [Theory]
    [InlineData(CoverageLevel.Good, CoverageLevel.Good, CoverageLevel.Good)]
    [InlineData(CoverageLevel.Good, CoverageLevel.Moderate, CoverageLevel.Moderate)]
    [InlineData(CoverageLevel.Poor, CoverageLevel.Good, CoverageLevel.Poor)]
    [InlineData(CoverageLevel.Good, CoverageLevel.Unavailable, CoverageLevel.Unavailable)]
    public void OverallCoverageUsesWorstAvailableLevel(
        CoverageLevel hospital,
        CoverageLevel fireStation,
        CoverageLevel expected)
    {
        Assert.Equal(expected, CoverageClassifier.Aggregate(hospital, fireStation));
    }
}
