namespace Dashboard.Tests.Weather;

public class OpenWeatherConditionMapperTests
{
    [Theory]
    [InlineData(210, WeatherCondition.Thunderstorm)]
    [InlineData(310, WeatherCondition.Drizzle)]
    [InlineData(500, WeatherCondition.Rain)]
    [InlineData(511, WeatherCondition.Rain)]   // 511 (freezing rain) bleibt im 5xx-Bereich → Rain
    [InlineData(601, WeatherCondition.Snow)]
    [InlineData(741, WeatherCondition.Mist)]
    [InlineData(800, WeatherCondition.Clear)]
    [InlineData(801, WeatherCondition.Clouds)]
    [InlineData(804, WeatherCondition.Clouds)]
    [InlineData(0, WeatherCondition.Unknown)]
    [InlineData(999, WeatherCondition.Unknown)]
    public void Map_TranslatesOwmIdRanges(int owmId, WeatherCondition expected)
    {
        Assert.Equal(expected, OpenWeatherConditionMapper.Map(owmId));
    }
}
