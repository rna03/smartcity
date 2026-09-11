using SmartCity.Infrastructure.Persistence;

namespace SmartCity.Infrastructure.Tests.Persistence;

public sealed class LocationAnalysisSqlTests
{
    [Theory]
    [InlineData("hospitals")]
    [InlineData("fire_stations")]
    public void NearestQueryUsesParameterizedDatabaseSideGeographyDistance(
        string tableName)
    {
        var sql = tableName == "hospitals"
            ? LocationAnalysisSql.NearestHospital
            : LocationAnalysisSql.NearestFireStation;

        Assert.Contains($"FROM {tableName}", sql, StringComparison.Ordinal);
        Assert.Contains("ST_Distance(\"Geometry\"::geography", sql, StringComparison.Ordinal);
        Assert.Contains(
            "ST_MakePoint(@longitude, @latitude)",
            sql,
            StringComparison.Ordinal);
        Assert.Contains("ORDER BY distance_meters", sql, StringComparison.Ordinal);
        Assert.Contains("LIMIT 1", sql, StringComparison.Ordinal);
    }
}
