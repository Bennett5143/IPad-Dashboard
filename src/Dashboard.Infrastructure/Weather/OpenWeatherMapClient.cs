using System.Globalization;
using System.Net.Http.Json;

using Dashboard.Domain.Time;
using Dashboard.Domain.Weather;

using Microsoft.Extensions.Options;

namespace Dashboard.Infrastructure.Weather;

/// <summary>
/// <see cref="IWeatherProvider"/> auf Basis der kostenlosen OpenWeatherMap-Endpunkte
/// <c>data/2.5/weather</c> (aktuell) und <c>data/2.5/forecast</c> (5 Tage / 3-Stunden-Raster).
/// Typed <see cref="HttpClient"/>; BaseAddress und Timeout werden bei der Registrierung gesetzt.
/// </summary>
public sealed class OpenWeatherMapClient : IWeatherProvider
{
    private static readonly TimeZoneInfo BerlinTz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    private readonly HttpClient _http;
    private readonly IClock _clock;
    private readonly WeatherOptions _options;

    public OpenWeatherMapClient(HttpClient http, IClock clock, IOptions<WeatherOptions> options)
    {
        _http = http;
        _clock = clock;
        _options = options.Value;
    }

    public async Task<WeatherSnapshot> GetWeatherAsync(CancellationToken ct = default)
    {
        var current = await GetCurrentAsync(ct);
        var steps = await GetForecastAsync(ct);

        return WeatherSnapshotFactory.Create(
            current, steps, _clock.UtcNow, BerlinTz, _options.HourlyCount);
    }

    private async Task<CurrentWeather> GetCurrentAsync(CancellationToken ct)
    {
        var response = await _http.GetFromJsonAsync<OwmCurrentResponse>(BuildUri("data/2.5/weather"), ct)
            ?? throw new InvalidOperationException("Leere Antwort vom Wetter-Endpoint (current).");

        var weather = response.Weather.FirstOrDefault();

        return new CurrentWeather(
            response.Main.Temp,
            response.Main.FeelsLike,
            response.Main.Humidity,
            response.Wind?.Speed ?? 0d,
            OpenWeatherConditionMapper.Map(weather?.Id ?? 0),
            Capitalize(weather?.Description),
            WindDirectionDeg: response.Wind?.Deg,
            WindGustMs: response.Wind?.Gust,
            SunriseUtc: response.Sys?.Sunrise is { } sunrise ? DateTimeOffset.FromUnixTimeSeconds(sunrise) : null,
            SunsetUtc: response.Sys?.Sunset is { } sunset ? DateTimeOffset.FromUnixTimeSeconds(sunset) : null);
    }

    private async Task<IReadOnlyList<ForecastStep>> GetForecastAsync(CancellationToken ct)
    {
        var response = await _http.GetFromJsonAsync<OwmForecastResponse>(BuildUri("data/2.5/forecast"), ct)
            ?? throw new InvalidOperationException("Leere Antwort vom Wetter-Endpoint (forecast).");

        return response.List
            .Select(static item =>
            {
                var weather = item.Weather.FirstOrDefault();
                return new ForecastStep(
                    DateTimeOffset.FromUnixTimeSeconds(item.Dt),
                    item.Main.Temp,
                    item.Pop,
                    OpenWeatherConditionMapper.Map(weather?.Id ?? 0),
                    Capitalize(weather?.Description));
            })
            .ToList();
    }

    private string BuildUri(string path)
    {
        var lat = _options.Latitude.ToString(CultureInfo.InvariantCulture);
        var lon = _options.Longitude.ToString(CultureInfo.InvariantCulture);

        return $"{path}?lat={lat}&lon={lon}&units={_options.Units}" +
               $"&lang={_options.Language}&appid={_options.ApiKey}";
    }

    private static string Capitalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return string.Create(text.Length, text, static (span, source) =>
        {
            source.AsSpan().CopyTo(span);
            span[0] = char.ToUpper(span[0], CultureInfo.GetCultureInfo("de-DE"));
        });
    }
}
