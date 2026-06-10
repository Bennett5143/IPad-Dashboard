using System.Text.Json.Serialization;

namespace Dashboard.Infrastructure.Weather;

// Interne DTOs, die exakt das JSON der OpenWeatherMap-Endpunkte abbilden.
// Bewusst minimal gehalten – nur die Felder, die die App tatsächlich verwendet.

internal sealed record OwmCurrentResponse(
    [property: JsonPropertyName("main")] OwmMain Main,
    [property: JsonPropertyName("weather")] IReadOnlyList<OwmWeather> Weather);

internal sealed record OwmForecastResponse(
    [property: JsonPropertyName("list")] IReadOnlyList<OwmForecastItem> List);

internal sealed record OwmForecastItem(
    [property: JsonPropertyName("dt")] long Dt,
    [property: JsonPropertyName("main")] OwmMain Main,
    [property: JsonPropertyName("weather")] IReadOnlyList<OwmWeather> Weather,
    [property: JsonPropertyName("pop")] double Pop);

internal sealed record OwmMain(
    [property: JsonPropertyName("temp")] double Temp,
    [property: JsonPropertyName("feels_like")] double FeelsLike);

internal sealed record OwmWeather(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("description")] string Description);
