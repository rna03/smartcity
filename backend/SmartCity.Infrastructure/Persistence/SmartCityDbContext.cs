using Microsoft.EntityFrameworkCore;
using SmartCity.Domain.Entities;

namespace SmartCity.Infrastructure.Persistence;

public sealed class SmartCityDbContext(DbContextOptions<SmartCityDbContext> options)
    : DbContext(options)
{
    public DbSet<Region> Regions => Set<Region>();
    public DbSet<Hospital> Hospitals => Set<Hospital>();
    public DbSet<FireStation> FireStations => Set<FireStation>();
    public DbSet<Road> Roads => Set<Road>();
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<LocationAnalysis> LocationAnalyses => Set<LocationAnalysis>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SmartCityDbContext).Assembly);
    }
}
