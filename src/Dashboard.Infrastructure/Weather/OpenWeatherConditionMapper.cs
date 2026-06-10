using Dashboard.Domain.Weather;

namespace Dashboard.Infrastructure.Weather;

/// <summary>
/// Übersetzt die numerischen Condition-Codes von OpenWeatherMap in die anbieterneutrale
/// <see cref="WeatherCondition"/>. Siehe https://openweathermap.org/weather-conditions.
/// </summary>
public static class OpenWeatherConditionMapper
{
    public static WeatherCondition Map(int owmId) => owmId switch
    {
        >= 200 and < 300 => WeatherCondition.Thunderstorm,
        >= 300 and < 400 => WeatherCondition.Drizzle,
        >= 500 and < 600 => WeatherCondition.Rain,
        >= 600 and < 700 => WeatherCondition.Snow,
        >= 700 and < 800 => WeatherCondition.Mist,
        800 => WeatherCondition.Clear,
        > 800 and < 900 => WeatherCondition.Clouds,
        _ => WeatherCondition.Unknown
    };
}
