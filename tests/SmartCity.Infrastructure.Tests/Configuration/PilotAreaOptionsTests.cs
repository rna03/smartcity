using SmartCity.Application.Configuration;

namespace SmartCity.Infrastructure.Tests.Configuration;

public sealed class PilotAreaOptionsTests
{
    [Fact]
    public void SmallValidBoundingBoxIsAccepted()
    {
        var options = new PilotAreaOptions
        {
            Name = "Demo",
            South = 41.035,
            West = 28.99,
            North = 41.055,
            East = 29.03
        };

        Assert.True(options.IsValid());
    }

    [Theory]
    [InlineData(-91, 28, 41, 29)]
    [InlineData(41, -181, 42, 29)]
    [InlineData(42, 28, 41, 29)]
    [InlineData(41, 30, 42, 29)]
    public void InvalidBoundingBoxIsRejected(
        double south,
        double west,
        double north,
        double east)
    {
        var options = new PilotAreaOptions
        {
            Name = "Demo",
            South = south,
            West = west,
            North = north,
            East = east
        };

        Assert.False(options.IsValid());
    }
}
