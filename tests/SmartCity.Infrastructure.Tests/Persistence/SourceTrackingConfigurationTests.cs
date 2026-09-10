using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SmartCity.Domain.Entities;
using SmartCity.Infrastructure.Persistence;

namespace SmartCity.Infrastructure.Tests.Persistence;

public sealed class SourceTrackingConfigurationTests
{
    [Theory]
    [InlineData(typeof(Hospital))]
    [InlineData(typeof(FireStation))]
    [InlineData(typeof(Road))]
    public void SourceAndExternalIdIndexIsUnique(Type entityType)
    {
        var options = new DbContextOptionsBuilder<SmartCityDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=model_validation;Username=test;Password=test",
                npgsql => npgsql.UseNetTopologySuite())
            .Options;
        using var context = new SmartCityDbContext(options);

        var modelEntity = context.Model.FindEntityType(entityType);
        Assert.NotNull(modelEntity);

        var index = modelEntity.GetIndexes().Single(IsSourceTrackingIndex);
        Assert.True(index.IsUnique);
    }

    private static bool IsSourceTrackingIndex(IReadOnlyIndex index) =>
        index.Properties.Select(property => property.Name)
            .SequenceEqual(["Source", "ExternalId"], StringComparer.Ordinal);
}
