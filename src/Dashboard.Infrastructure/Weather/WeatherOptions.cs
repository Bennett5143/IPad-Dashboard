namespace Dashboard.Infrastructure.Weather;

/// <summary>
/// Konfiguration der Wetter-Anbindung. Standort und Intervall liegen in
/// <c>appsettings.json</c> (Sektion <see cref="SectionName"/>); der <see cref="ApiKey"/>
/// gehört als Geheimnis ausschließlich in User-Secrets bzw. Umgebungsvariablen.
/// </summary>
public sealed class WeatherOptions
{
    public const string SectionName = "Weather";

    public string ApiKey { get; init; } = string.Empty;

    /// <summary>Standort. Default: Hamburg.</summary>
    public double Latitude { get; init; } = 53.5511;
    public double Longitude { get; init; } = 9.9937;

    public string Language { get; init; } = "de";
    public string Units { get; init; } = "metric";

    public string BaseUrl { get; init; } = "https://api.openweathermap.org/";

    public TimeSpan RefreshInterval { get; init; } = TimeSpan.FromMinutes(15);

    /// <summary>Anzahl der Schritte in der stündlichen Vorschau (OWM-Raster: 3 h pro Schritt).</summary>
    public int HourlyCount { get; init; } = 4;
}
