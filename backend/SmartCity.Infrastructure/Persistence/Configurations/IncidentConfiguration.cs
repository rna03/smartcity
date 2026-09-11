using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartCity.Domain.Entities;

namespace SmartCity.Infrastructure.Persistence.Configurations;

internal sealed class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.ToTable("incidents");
        builder.HasKey(incident => incident.Id);
        builder.Property(incident => incident.IncidentType)
            .HasConversion<string>()
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(incident => incident.Description).HasMaxLength(500);
        builder.Property(incident => incident.OccurredAt).IsRequired();
        builder.Ignore(incident => incident.Latitude);
        builder.Ignore(incident => incident.Longitude);
        builder.Property(incident => incident.Geometry)
            .HasColumnType("geometry(Point,4326)")
            .IsRequired();
        builder.HasIndex(incident => incident.Geometry).HasMethod("gist");
        builder.HasIndex(incident => incident.OccurredAt);
    }
}
