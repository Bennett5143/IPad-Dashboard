namespace Dashboard.Domain.Weather;

/// <summary>
/// Aggregierte, UI-fertige Sicht auf die Wetterlage. Wird vom Background-Service
/// erzeugt, in <see cref="WeatherState"/> gehalten und von den Wetter-Tiles gelesen.
/// </summary>
public sealed record WeatherSnapshot(
    CurrentWeather Current,
    DailyForecast Today,
    DailyForecast? Tomorrow,
    IReadOnlyList<HourlyForecast> Hourly,
    DateTimeOffset RetrievedAtUtc) : Common.ISnapshot;
