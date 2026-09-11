namespace SmartCity.Application.Models;

public enum CoverageLevel
{
    Good = 1,
    Moderate = 2,
    Poor = 3,
    Unavailable = 4
}

public sealed record ServiceCoverageDto(
    double? DistanceMeters,
    CoverageLevel CoverageLevel);

public sealed record CoverageAnalysisResult(
    SelectedLocationDto SelectedLocation,
    ServiceCoverageDto Hospital,
    ServiceCoverageDto FireStation,
    CoverageLevel OverallCoverageLevel);

public static class CoverageClassifier
{
    public const double GoodMaximumMeters = 2_000;
    public const double ModerateMaximumMeters = 5_000;

    public static CoverageLevel Classify(double? distanceMeters)
    {
        if (!distanceMeters.HasValue ||
            !double.IsFinite(distanceMeters.Value) ||
            distanceMeters.Value < 0)
        {
            return CoverageLevel.Unavailable;
        }

        if (distanceMeters.Value <= GoodMaximumMeters)
        {
            return CoverageLevel.Good;
        }

        return distanceMeters.Value <= ModerateMaximumMeters
            ? CoverageLevel.Moderate
            : CoverageLevel.Poor;
    }

    public static CoverageLevel Aggregate(
        CoverageLevel hospital,
        CoverageLevel fireStation)
    {
        if (hospital == CoverageLevel.Unavailable ||
            fireStation == CoverageLevel.Unavailable)
        {
            return CoverageLevel.Unavailable;
        }

        if (hospital == CoverageLevel.Poor || fireStation == CoverageLevel.Poor)
        {
            return CoverageLevel.Poor;
        }

        return hospital == CoverageLevel.Moderate ||
               fireStation == CoverageLevel.Moderate
            ? CoverageLevel.Moderate
            : CoverageLevel.Good;
    }
}
