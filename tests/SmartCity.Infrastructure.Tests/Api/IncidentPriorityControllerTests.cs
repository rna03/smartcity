using Microsoft.AspNetCore.Mvc;
using SmartCity.Api.Controllers;
using SmartCity.Application.Abstractions;
using SmartCity.Application.Models;
using SmartCity.Domain;

namespace SmartCity.Infrastructure.Tests.Api;

public sealed class IncidentPriorityControllerTests
{
    [Fact]
    public async Task ValidRequestReturnsExplainablePriorityPreview()
    {
        var expected = CreateAnalysis();
        var service = new StubIncidentPriorityService(expected);
        var controller = new IncidentPriorityController(service);

        var response = await controller.Get(
            41.04,
            29.01,
            IncidentType.Fire,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var result = Assert.IsType<IncidentPriorityPreviewResponse>(ok.Value);
        Assert.Equal(expected.SelectedLocation, result.SelectedLocation);
        Assert.Equal(IncidentType.Fire, result.IncidentType);
        Assert.Equal(55, result.PriorityScore);
        Assert.Equal(PriorityLevel.High, result.PriorityLevel);
        Assert.Equal(EmergencyServiceType.FireStation, result.RelevantService.ServiceType);
        Assert.Equal(1_200, result.RelevantService.DistanceMeters);
        Assert.Equal(AccessibilityLevel.Good, result.AccessibilityLevel);
        Assert.Equal(40, result.Breakdown.IncidentTypeBaseScore);
        Assert.Equal(10, result.Breakdown.ServiceDistanceScore);
        Assert.Equal(5, result.Breakdown.AccessibilityPenalty);
        Assert.Equal(1, service.CallCount);
    }

    [Theory]
    [InlineData(-90.01, 29.01, IncidentType.Fire)]
    [InlineData(41.04, 180.01, IncidentType.Fire)]
    [InlineData(41.04, 29.01, (IncidentType)999)]
    public async Task InvalidRequestReturnsBadRequestWithoutCallingService(
        double latitude,
        double longitude,
        IncidentType incidentType)
    {
        var service = new StubIncidentPriorityService(CreateAnalysis());
        var controller = new IncidentPriorityController(service);

        var response = await controller.Get(
            latitude,
            longitude,
            incidentType,
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(response.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal(400, problem.Status);
        Assert.Equal(0, service.CallCount);
    }

    [Fact]
    public async Task MissingIncidentTypeReturnsBadRequestWithoutCallingService()
    {
        var service = new StubIncidentPriorityService(CreateAnalysis());
        var controller = new IncidentPriorityController(service);

        var response = await controller.Get(
            41.04,
            29.01,
            null,
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(response.Result);
        Assert.Equal(0, service.CallCount);
    }

    private static IncidentPriorityAnalysis CreateAnalysis()
    {
        var selectedLocation = new SelectedLocationDto(41.04, 29.01);
        var recommendation = new RecommendedEmergencyServiceDto(
            EmergencyServiceType.FireStation,
            2,
            "Fire Station",
            "way/2",
            41.045,
            29.015,
            1_200);

        return new IncidentPriorityAnalysis(
            selectedLocation,
            IncidentType.Fire,
            recommendation,
            new IncidentPriorityResult(
                55,
                PriorityLevel.High,
                40,
                10,
                5,
                EmergencyServiceType.FireStation,
                1_200,
                AccessibilityLevel.Good));
    }

    private sealed class StubIncidentPriorityService(
        IncidentPriorityAnalysis result)
        : IIncidentPriorityService
    {
        public int CallCount { get; private set; }

        public Task<IncidentPriorityAnalysis> AnalyzeAsync(
            double latitude,
            double longitude,
            IncidentType incidentType,
            CancellationToken cancellationToken)
        {
            CallCount += 1;
            return Task.FromResult(result);
        }
    }
}
