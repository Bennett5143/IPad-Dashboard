namespace Dashboard.Domain.Weather;

/// <summary>Aktuelle, in Echtzeit gemessene Wetterlage am konfigurierten Standort.</summary>
public sealed record CurrentWeather(
    double Temperature,
    double FeelsLike,
    int Humidity,
    double WindSpeedMs,
    WeatherCondition Condition,
    string Description);
