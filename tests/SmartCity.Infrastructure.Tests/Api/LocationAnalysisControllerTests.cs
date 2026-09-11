using Microsoft.AspNetCore.Mvc;
using SmartCity.Api.Controllers;
using SmartCity.Application.Abstractions;
using SmartCity.Application.Models;

namespace SmartCity.Infrastructure.Tests.Api;

public sealed class LocationAnalysisControllerTests
{
    [Fact]
    public async Task InvalidLatitudeReturnsBadRequestWithoutCallingService()
    {
        var service = new StubLocationAnalysisService(CreateResult());
        var controller = new LocationAnalysisController(service);

        var response = await controller.GetNearest(90.01, 29.01, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(response.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal(400, problem.Status);
        Assert.Equal(0, service.CallCount);
    }

    [Fact]
    public async Task InvalidLongitudeReturnsBadRequestWithoutCallingService()
    {
        var service = new StubLocationAnalysisService(CreateResult());
        var controller = new LocationAnalysisController(service);

        var response = await controller.GetNearest(
            41.04,
            double.PositiveInfinity,
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(response.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal(400, problem.Status);
        Assert.Equal(0, service.CallCount);
    }

    [Fact]
    public async Task ValidCoordinatesReturnNearestHospitalAndFireStationInMeters()
    {
        var expected = CreateResult();
        var service = new StubLocationAnalysisService(expected);
        var controller = new LocationAnalysisController(service);

        var response = await controller.GetNearest(41.04, 29.01, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var result = Assert.IsType<NearestEmergencyServicesResult>(ok.Value);
        Assert.Equal(expected.NearestHospital, result.NearestHospital);
        Assert.Equal(expected.NearestFireStation, result.NearestFireStation);
        Assert.True(result.NearestHospital!.DistanceMeters > 0);
        Assert.True(result.NearestFireStation!.DistanceMeters > 0);
        Assert.Equal(1, service.CallCount);
    }

    [Fact]
    public async Task MissingHospitalReturnsControlledNull()
    {
        var expected = CreateResult() with { NearestHospital = null };
        var controller = new LocationAnalysisController(
            new StubLocationAnalysisService(expected));

        var response = await controller.GetNearest(41.04, 29.01, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var result = Assert.IsType<NearestEmergencyServicesResult>(ok.Value);
        Assert.Null(result.NearestHospital);
        Assert.NotNull(result.NearestFireStation);
    }

    [Fact]
    public async Task MissingFireStationReturnsControlledNull()
    {
        var expected = CreateResult() with { NearestFireStation = null };
        var controller = new LocationAnalysisController(
            new StubLocationAnalysisService(expected));

        var response = await controller.GetNearest(41.04, 29.01, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var result = Assert.IsType<NearestEmergencyServicesResult>(ok.Value);
        Assert.NotNull(result.NearestHospital);
        Assert.Null(result.NearestFireStation);
    }

    private static NearestEmergencyServicesResult CreateResult() =>
        new(
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

    private sealed class StubLocationAnalysisService(
        NearestEmergencyServicesResult result)
        : ILocationAnalysisService
    {
        public int CallCount { get; private set; }

        public Task<NearestEmergencyServicesResult> FindNearestAsync(
            double latitude,
            double longitude,
            CancellationToken cancellationToken)
        {
            CallCount += 1;
            return Task.FromResult(result);
        }
    }
}
