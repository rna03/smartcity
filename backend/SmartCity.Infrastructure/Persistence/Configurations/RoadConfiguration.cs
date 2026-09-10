using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartCity.Domain.Entities;

namespace SmartCity.Infrastructure.Persistence.Configurations;

internal sealed class RoadConfiguration : IEntityTypeConfiguration<Road>
{
    public void Configure(EntityTypeBuilder<Road> builder)
    {
        builder.ToTable("roads");
        builder.HasKey(road => road.Id);
        builder.Property(road => road.Name).HasMaxLength(250).IsRequired();
        builder.Property(road => road.RoadType).HasMaxLength(100).IsRequired();
        builder.Property(road => road.Source).HasMaxLength(50).IsRequired();
        builder.Property(road => road.ExternalId).HasMaxLength(100).IsRequired();
        builder.Property(road => road.Geometry)
            .HasColumnType("geometry(LineString,4326)")
            .IsRequired();
        builder.HasIndex(road => road.Geometry).HasMethod("gist");
        builder.HasIndex(road => new { road.Source, road.ExternalId }).IsUnique();
    }
}
