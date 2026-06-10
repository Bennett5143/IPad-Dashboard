namespace Dashboard.Domain.Weather;

/// <summary>
/// Einzelner Vorhersage-Datenpunkt, wie ihn die Wetter-API liefert (bei
/// OpenWeatherMap im 3-Stunden-Raster). Neutrale Zwischenrepräsentation, aus der
/// <see cref="WeatherSnapshotFactory"/> die UI-fertige Tages- und Stundensicht aggregiert.
/// </summary>
public sealed record ForecastStep(
    DateTimeOffset TimestampUtc,
    double Temperature,
    double PrecipitationProbability,
    WeatherCondition Condition,
    string Description);
