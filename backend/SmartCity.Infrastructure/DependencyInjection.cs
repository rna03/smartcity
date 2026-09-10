using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SmartCity.Application.Abstractions;
using SmartCity.Infrastructure.OpenStreetMap;
using SmartCity.Infrastructure.Persistence;

namespace SmartCity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<SmartCityDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsqlOptions => npgsqlOptions.UseNetTopologySuite()));

        services.AddSingleton<OverpassResponseMapper>();
        services.AddHttpClient<IOpenStreetMapDataSource, OverpassClient>(
            (serviceProvider, client) =>
            {
                var options = serviceProvider
                    .GetRequiredService<IOptions<OpenStreetMapOptions>>()
                    .Value;

                client.BaseAddress = new Uri(options.OverpassUrl);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
                client.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "SmartCityLocationIntelligence/1.0");
            });

        services.AddScoped<OpenStreetMapSpatialDataStore>();
        services.AddScoped<ISpatialDataStore>(serviceProvider =>
            serviceProvider.GetRequiredService<OpenStreetMapSpatialDataStore>());
        services.AddScoped<ISpatialDataQueryService>(serviceProvider =>
            serviceProvider.GetRequiredService<OpenStreetMapSpatialDataStore>());

        return services;
    }
}
