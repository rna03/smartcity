using Microsoft.AspNetCore.Mvc;
using SmartCity.Api.Controllers;
using SmartCity.Application.Abstractions;
using SmartCity.Application.Models;
using SmartCity.Domain;

namespace SmartCity.Infrastructure.Tests.Api;

public sealed class IncidentsControllerTests
{
    [Theory]
    [InlineData(IncidentType.Fire)]
    [InlineData(IncidentType.Medical)]
    public async Task ValidIncidentReturnsCreated(IncidentType incidentType)
    {
        var expected = CreateResult(incidentType);
        var service = new StubIncidentService(expected);
        var controller = new IncidentsController(service);
        var request = new CreateIncidentRequest(
            incidentType,
            41.04,
            29.01,
            "Emergency report");

        var response = await controller.Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(response.Result);
        Assert.Equal(201, created.StatusCode);
        Assert.Equal(expected, created.Value);
        Assert.Equal(1, service.CreateCallCount);
    }

    [Fact]
    public async Task InvalidLatitudeReturnsBadRequest()
    {
        await AssertBadRequestAsync(new CreateIncidentRequest(
            IncidentType.Fire,
            90.01,
            29.01,
            null));
    }

    [Fact]
    public async Task InvalidLongitudeReturnsBadRequest()
    {
        await AssertBadRequestAsync(new CreateIncidentRequest(
            IncidentType.Medical,
            41.04,
            double.PositiveInfinity,
            null));
    }

    [Fact]
    public async Task InvalidIncidentTypeReturnsBadRequest()
    {
        await AssertBadRequestAsync(new CreateIncidentRequest(
            (IncidentType)999,
            41.04,
            29.01,
            null));
    }

    [Fact]
    public async Task DescriptionOverMaximumLengthReturnsBadRequest()
    {
        await AssertBadRequestAsync(new CreateIncidentRequest(
            IncidentType.Other,
            41.04,
            29.01,
            new string('x', IncidentRequestValidation.MaxDescriptionLength + 1)));
    }

    private static async Task AssertBadRequestAsync(CreateIncidentRequest request)
    {
        var service = new StubIncidentService(CreateResult(IncidentType.Fire));
        var controller = new IncidentsController(service);

        var response = await controller.Create(request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(response.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal(400, problem.Status);
        Assert.Equal(0, service.CreateCallCount);
    }

    private static IncidentCreationResult CreateResult(IncidentType incidentType) =>
        new(
            new IncidentDto(
                7,
                incidentType,
                41.04,
                29.01,
                "Emergency report",
                new DateTimeOffset(2026, 9, 11, 0, 0, 0, TimeSpan.Zero)),
            null);

    private sealed class StubIncidentService(IncidentCreationResult createResult)
        : IIncidentService
    {
        public int CreateCallCount { get; private set; }

        public Task<IncidentCreationResult> CreateAsync(
            IncidentType incidentType,
            double latitude,
            double longitude,
            string? description,
            CancellationToken cancellationToken)
        {
            CreateCallCount += 1;
            return Task.FromResult(createResult);
        }

        public Task<IReadOnlyList<IncidentDto>> GetAllAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<IncidentDto>>([createResult.Incident]);
    }
}
