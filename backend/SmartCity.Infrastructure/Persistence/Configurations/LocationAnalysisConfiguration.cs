using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartCity.Domain.Entities;

namespace SmartCity.Infrastructure.Persistence.Configurations;

internal sealed class LocationAnalysisConfiguration
    : IEntityTypeConfiguration<LocationAnalysis>
{
    public void Configure(EntityTypeBuilder<LocationAnalysis> builder)
    {
        builder.ToTable("location_analyses", table =>
        {
            table.HasCheckConstraint(
                "ck_location_analyses_scores",
                "\"HospitalDistanceScore\" BETWEEN 0 AND 100 AND " +
                "\"FireStationDistanceScore\" BETWEEN 0 AND 100 AND " +
                "\"RoadAccessibilityScore\" BETWEEN 0 AND 100 AND " +
                "\"PopulationScore\" BETWEEN 0 AND 100 AND " +
                "\"IncidentDensityScore\" BETWEEN 0 AND 100 AND " +
                "\"FinalNeedScore\" BETWEEN 0 AND 100");
        });

        builder.HasKey(analysis => analysis.Id);
        builder.Property(analysis => analysis.HospitalDistanceScore).HasPrecision(5, 2);
        builder.Property(analysis => analysis.FireStationDistanceScore).HasPrecision(5, 2);
        builder.Property(analysis => analysis.RoadAccessibilityScore).HasPrecision(5, 2);
        builder.Property(analysis => analysis.PopulationScore).HasPrecision(5, 2);
        builder.Property(analysis => analysis.IncidentDensityScore).HasPrecision(5, 2);
        builder.Property(analysis => analysis.FinalNeedScore).HasPrecision(5, 2);
        builder.Property(analysis => analysis.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.HasOne(analysis => analysis.Region)
            .WithMany(region => region.LocationAnalyses)
            .HasForeignKey(analysis => analysis.RegionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(analysis => new { analysis.RegionId, analysis.CreatedAt });
    }
}
