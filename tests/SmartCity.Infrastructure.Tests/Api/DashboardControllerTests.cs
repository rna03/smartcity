using Microsoft.AspNetCore.Mvc;
using SmartCity.Api.Controllers;
using SmartCity.Application.Abstractions;
using SmartCity.Application.Models;
using SmartCity.Domain;

namespace SmartCity.Infrastructure.Tests.Api;

public sealed class DashboardControllerTests
{
    [Fact]
    public async Task SummaryReturnsSuccessfulResponse()
    {
        var expected = new DashboardSummaryResponse(
            3,
            new IncidentTypeCount(1, 1, 1, 0),
            [
                new DashboardIncidentItem(
                    3,
                    IncidentType.Medical,
                    41.04,
                    29.01,
                    "Medical dashboard test",
                    new DateTimeOffset(2026, 9, 13, 12, 0, 0, TimeSpan.Zero))
            ]);
        var service = new StubDashboardAnalyticsService(expected);
        var controller = new DashboardController(service);

        var response = await controller.GetSummary(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        Assert.Equal(expected, ok.Value);
        Assert.Equal(1, service.CallCount);
    }

    private sealed class StubDashboardAnalyticsService(
        DashboardSummaryResponse response)
        : IDashboardAnalyticsService
    {
        public int CallCount { get; private set; }

        public Task<DashboardSummaryResponse> GetSummaryAsync(
            CancellationToken cancellationToken)
        {
            CallCount += 1;
            return Task.FromResult(response);
        }
    }
}
