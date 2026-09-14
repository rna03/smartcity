using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartCity.Domain;
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
        builder.Property(incident => incident.PriorityScore)
            .HasDefaultValue(0)
            .IsRequired();
        builder.Property(incident => incident.PriorityLevel)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(PriorityLevel.Low)
            .IsRequired();
        builder.Property(incident => incident.OccurredAt).IsRequired();
        builder.Ignore(incident => incident.Latitude);
        builder.Ignore(incident => incident.Longitude);
        builder.Property(incident => incident.Geometry)
            .HasColumnType("geometry(Point,4326)")
            .IsRequired();
        builder.HasIndex(incident => incident.Geometry).HasMethod("gist");
        builder.HasIndex(incident => incident.OccurredAt);
        builder.ToTable(tableBuilder =>
            tableBuilder.HasCheckConstraint(
                "ck_incidents_priority_score",
                "\"PriorityScore\" BETWEEN 0 AND 100"));
    }
}
