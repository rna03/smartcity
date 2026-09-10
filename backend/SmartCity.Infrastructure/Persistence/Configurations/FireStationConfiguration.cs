using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartCity.Domain.Entities;

namespace SmartCity.Infrastructure.Persistence.Configurations;

internal sealed class FireStationConfiguration : IEntityTypeConfiguration<FireStation>
{
    public void Configure(EntityTypeBuilder<FireStation> builder)
    {
        builder.ToTable("fire_stations");
        builder.HasKey(station => station.Id);
        builder.Property(station => station.Name).HasMaxLength(200).IsRequired();
        builder.Property(station => station.Source).HasMaxLength(50).IsRequired();
        builder.Property(station => station.ExternalId).HasMaxLength(100).IsRequired();
        builder.Ignore(station => station.Latitude);
        builder.Ignore(station => station.Longitude);
        builder.Property(station => station.Geometry)
            .HasColumnType("geometry(Point,4326)")
            .IsRequired();
        builder.HasIndex(station => station.Geometry).HasMethod("gist");
        builder.HasIndex(station => new { station.Source, station.ExternalId }).IsUnique();
    }
}
