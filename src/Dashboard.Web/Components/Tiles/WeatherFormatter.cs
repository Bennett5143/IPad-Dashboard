using System.Globalization;

namespace Dashboard.Web.Components.Tiles;

/// <summary>Reine Darstellungs-Helfer für die Wetter-Kacheln (Emoji, Temperatur, Regen, Uhrzeit).</summary>
public static class WeatherFormatter
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    private static readonly TimeZoneInfo BerlinTz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    public static string Emoji(WeatherCondition condition) => condition switch
    {
        WeatherCondition.Clear => "☀️",
        WeatherCondition.Clouds => "☁️",
        WeatherCondition.Rain => "🌧️",
        WeatherCondition.Drizzle => "🌦️",
        WeatherCondition.Thunderstorm => "⛈️",
        WeatherCondition.Snow => "❄️",
        WeatherCondition.Mist => "🌫️",
        _ => "⛅"
    };

    public static string Temperature(double celsius)
    {
        var rounded = Math.Round(celsius, MidpointRounding.AwayFromZero);
        return $"{rounded.ToString("0", German)}°";
    }

    public static string Precipitation(double probability)
    {
        var pct = Math.Round(Math.Clamp(probability, 0d, 1d) * 100d, MidpointRounding.AwayFromZero);
        return $"{pct.ToString("0", German)} %";
    }

    public static string Hour(DateTimeOffset localTime) =>
        localTime.ToString("HH:mm", CultureInfo.InvariantCulture);

    public static string Humidity(int percent) => $"{percent.ToString("0", German)} %";

    /// <summary>Windgeschwindigkeit von m/s in km/h gerundet.</summary>
    public static string Wind(double metersPerSecond)
    {
        var kmh = Math.Round(metersPerSecond * 3.6, MidpointRounding.AwayFromZero);
        return $"{kmh.ToString("0", German)} km/h";
    }

    /// <summary>Windrichtung (Grad → 8-Sektoren-Kompass); <c>null</c> ohne Richtung.</summary>
    public static string? WindDirection(int? degrees)
    {
        if (degrees is not { } deg)
        {
            return null;
        }

        string[] sectors = ["N", "NO", "O", "SO", "S", "SW", "W", "NW"];
        var index = (int)Math.Round((deg % 360) / 45.0, MidpointRounding.AwayFromZero) % 8;
        return sectors[index];
    }

    /// <summary>Böen-Geschwindigkeit in km/h; <c>–</c> ohne Wert.</summary>
    public static string Gust(double? metersPerSecond) =>
        metersPerSecond is { } ms ? Wind(ms) : "–";

    /// <summary>Sonnenauf-/untergangszeit (Berlin, HH:mm); <c>–</c> ohne Wert.</summary>
    public static string Sun(DateTimeOffset? utc) =>
        utc is { } time
            ? TimeZoneInfo.ConvertTime(time, BerlinTz).ToString("HH:mm", CultureInfo.InvariantCulture)
            : "–";

    public static string UpdatedAt(DateTimeOffset retrievedAtUtc) =>
        TimeZoneInfo.ConvertTime(retrievedAtUtc, BerlinTz)
            .ToString("HH:mm", CultureInfo.InvariantCulture);
}
