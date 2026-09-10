using SmartCity.Application.Configuration;
using SmartCity.Application.Models;

namespace SmartCity.Infrastructure.Tests.Configuration;

public sealed class MapConfigurationDtoTests
{
    [Fact]
    public void FromUsesPilotAreaCenterAndBounds()
    {
        var options = new PilotAreaOptions
        {
            Name = "Istanbul Besiktas Demo",
            South = 41.035,
            West = 28.99,
            North = 41.055,
            East = 29.03
        };

        var result = MapConfigurationDto.From(options);

        Assert.Equal("Istanbul Besiktas Demo", result.PilotArea);
        Assert.Equal(41.045, result.CenterLatitude, precision: 3);
        Assert.Equal(29.01, result.CenterLongitude, precision: 3);
        Assert.Equal(options.South, result.Bounds.South);
        Assert.Equal(options.West, result.Bounds.West);
        Assert.Equal(options.North, result.Bounds.North);
        Assert.Equal(options.East, result.Bounds.East);
    }
}
