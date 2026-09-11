using SmartCity.Application.Models;

namespace SmartCity.Infrastructure.Tests.Application;

public sealed class LocationAnalysisModelTests
{
    [Theory]
    [InlineData(-90.01)]
    [InlineData(90.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidLatitudeIsRejected(double latitude)
    {
        Assert.False(LocationCoordinateValidation.IsValid(latitude, 29.01));
    }

    [Theory]
    [InlineData(-180.01)]
    [InlineData(180.01)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    public void InvalidLongitudeIsRejected(double longitude)
    {
        Assert.False(LocationCoordinateValidation.IsValid(41.04, longitude));
    }

    [Fact]
    public void ResultRepresentsNearestServicesWithPositiveMeterDistances()
    {
        var result = new NearestEmergencyServicesResult(
            new SelectedLocationDto(41.04, 29.01),
            new NearestEmergencyServiceDto(
                12,
                "Nearest Hospital",
                "node/12",
                41.041,
                29.011,
                138.42),
            new NearestEmergencyServiceDto(
                31,
                "Nearest Fire Station",
                "way/31",
                41.045,
                29.015,
                695.71));

        var hospital = Assert.IsType<NearestEmergencyServiceDto>(result.NearestHospital);
        var fireStation = Assert.IsType<NearestEmergencyServiceDto>(result.NearestFireStation);
        Assert.Equal("Nearest Hospital", hospital.Name);
        Assert.Equal("Nearest Fire Station", fireStation.Name);
        Assert.True(hospital.DistanceMeters > 0);
        Assert.True(fireStation.DistanceMeters > 0);
    }

    [Fact]
    public void ResultRepresentsEmptyHospitalAndFireStationTables()
    {
        var result = new NearestEmergencyServicesResult(
            new SelectedLocationDto(41.04, 29.01),
            null,
            null);

        Assert.Null(result.NearestHospital);
        Assert.Null(result.NearestFireStation);
    }
}
