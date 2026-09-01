namespace Dashboard.Tests.Components.Tiles;

public class WeatherFormatterTests
{
    [Theory]
    [InlineData(WeatherCondition.Clear, "☀️")]
    [InlineData(WeatherCondition.Clouds, "☁️")]
    [InlineData(WeatherCondition.Rain, "🌧️")]
    [InlineData(WeatherCondition.Thunderstorm, "⛈️")]
    [InlineData(WeatherCondition.Snow, "❄️")]
    [InlineData(WeatherCondition.Unknown, "⛅")]
    public void Emoji_MapsCondition(WeatherCondition condition, string expected)
    {
        Assert.Equal(expected, WeatherFormatter.Emoji(condition));
    }

    [Theory]
    [InlineData(17.4, "17°")]
    [InlineData(17.5, "18°")]   // kaufmännisch gerundet
    [InlineData(-2.6, "-3°")]
    [InlineData(0.0, "0°")]
    public void Temperature_RoundsToWholeDegrees(double celsius, string expected)
    {
        Assert.Equal(expected, WeatherFormatter.Temperature(celsius));
    }

    [Theory]
    [InlineData(0.0, "0 %")]
    [InlineData(0.65, "65 %")]
    [InlineData(1.0, "100 %")]
    [InlineData(0.123, "12 %")]
    public void Precipitation_FormatsAsPercent(double probability, string expected)
    {
        Assert.Equal(expected, WeatherFormatter.Precipitation(probability));
    }

    [Theory]
    [InlineData(0.0, false)]
    [InlineData(0.004, false)]   // rundet auf 0 % — trägt keine Aussage
    [InlineData(0.005, true)]    // rundet auf 1 % — die kleinste echte Angabe
    [InlineData(0.29, true)]
    [InlineData(1.0, true)]
    public void ShowsPrecipitation_SuppressesOnlyWhatRoundsToZero(double probability, bool expected)
    {
        Assert.Equal(expected, WeatherFormatter.ShowsPrecipitation(probability));
    }

    [Fact]
    public void Hour_FormatsLocalTimeAsHourMinute()
    {
        var time = new DateTimeOffset(2026, 6, 10, 15, 0, 0, TimeSpan.FromHours(2));

        Assert.Equal("15:00", WeatherFormatter.Hour(time));
    }

    [Theory]
    [InlineData(0, "N")]
    [InlineData(45, "NO")]
    [InlineData(90, "O")]
    [InlineData(200, "S")]      // 200/45 = 4,4 → S
    [InlineData(350, "N")]      // wrap-around
    public void WindDirection_MapsToCompassSector(int degrees, string expected)
    {
        Assert.Equal(expected, WeatherFormatter.WindDirection(degrees));
    }

    [Fact]
    public void WindDirection_NullWithoutDegrees()
    {
        Assert.Null(WeatherFormatter.WindDirection(null));
    }

    [Fact]
    public void Sun_ConvertsUtcToBerlin_OrDash()
    {
        // 04:00 UTC → 06:00 Berlin (CEST)
        Assert.Equal("06:00", WeatherFormatter.Sun(new DateTimeOffset(2026, 6, 10, 4, 0, 0, TimeSpan.Zero)));
        Assert.Equal("–", WeatherFormatter.Sun(null));
    }

    [Fact]
    public void Gust_FormatsKmhOrDash()
    {
        Assert.Equal("26 km/h", WeatherFormatter.Gust(7.2)); // 7,2 m/s → 25,9 → 26
        Assert.Equal("–", WeatherFormatter.Gust(null));
    }
}
