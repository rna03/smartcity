using SmartCity.Application.Models;

namespace SmartCity.Application.Abstractions;

public interface IDashboardAnalyticsService
{
    Task<DashboardSummaryResponse> GetSummaryAsync(
        CancellationToken cancellationToken);
}
