namespace Dashboard.Domain.Weather;

/// <summary>
/// Grob kategorisierter Wetterzustand, unabhängig vom konkreten API-Anbieter.
/// Die Übersetzung anbieterspezifischer Codes übernimmt die Infrastructure-Schicht.
/// </summary>
public enum WeatherCondition
{
    Unknown = 0,
    Clear,
    Clouds,
    Rain,
    Drizzle,
    Thunderstorm,
    Snow,
    Mist
}
