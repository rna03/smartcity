using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartCity.Domain.Entities;

namespace SmartCity.Infrastructure.Persistence.Configurations;

internal sealed class HospitalConfiguration : IEntityTypeConfiguration<Hospital>
{
    public void Configure(EntityTypeBuilder<Hospital> builder)
    {
        builder.ToTable("hospitals");
        builder.HasKey(hospital => hospital.Id);
        builder.Property(hospital => hospital.Name).HasMaxLength(200).IsRequired();
        builder.Property(hospital => hospital.Source).HasMaxLength(50).IsRequired();
        builder.Property(hospital => hospital.ExternalId).HasMaxLength(100).IsRequired();
        builder.Ignore(hospital => hospital.Latitude);
        builder.Ignore(hospital => hospital.Longitude);
        builder.Property(hospital => hospital.Geometry)
            .HasColumnType("geometry(Point,4326)")
            .IsRequired();
        builder.HasIndex(hospital => hospital.Geometry).HasMethod("gist");
        builder.HasIndex(hospital => new { hospital.Source, hospital.ExternalId }).IsUnique();
    }
}
