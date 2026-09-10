using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartCity.Domain.Entities;

namespace SmartCity.Infrastructure.Persistence.Configurations;

internal sealed class RegionConfiguration : IEntityTypeConfiguration<Region>
{
    public void Configure(EntityTypeBuilder<Region> builder)
    {
        builder.ToTable("regions", table =>
        {
            table.HasCheckConstraint("ck_regions_population", "\"Population\" >= 0");
            table.HasCheckConstraint("ck_regions_population_density", "\"PopulationDensity\" >= 0");
            table.HasCheckConstraint("ck_regions_incident_count", "\"IncidentCount\" >= 0");
        });

        builder.HasKey(region => region.Id);
        builder.Property(region => region.Name).HasMaxLength(200).IsRequired();
        builder.Property(region => region.Geometry)
            .HasColumnType("geometry(MultiPolygon,4326)")
            .IsRequired();
        builder.HasIndex(region => region.Geometry).HasMethod("gist");
    }
}
