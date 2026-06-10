namespace Dashboard.Domain.Weather;

/// <summary>
/// Liefert eine vollständige, aggregierte Wetterlage. Die konkrete API-Anbindung
/// steckt in der Infrastructure-Schicht; die Domain kennt nur diesen Vertrag.
/// </summary>
public interface IWeatherProvider
{
    Task<WeatherSnapshot> GetWeatherAsync(CancellationToken ct = default);
}
