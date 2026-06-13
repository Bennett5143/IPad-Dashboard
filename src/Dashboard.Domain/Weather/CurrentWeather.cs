namespace Dashboard.Domain.Weather;

/// <summary>
/// Aktuelle, in Echtzeit gemessene Wetterlage am konfigurierten Standort. Windrichtung, Böen
/// und Sonnenauf-/untergang sind optional (nicht jede Antwort liefert sie).
/// </summary>
public sealed record CurrentWeather(
    double Temperature,
    double FeelsLike,
    int Humidity,
    double WindSpeedMs,
    WeatherCondition Condition,
    string Description,
    int? WindDirectionDeg = null,
    double? WindGustMs = null,
    DateTimeOffset? SunriseUtc = null,
    DateTimeOffset? SunsetUtc = null);
