using Microsoft.Extensions.Options;

namespace Dashboard.Tests.Weather;

public class OpenWeatherMapClientTests
{
    // FakeClock-Zeitpunkt = 10. Juni 2026, 12:00 UTC (14:00 Berlin).
    private static readonly DateTimeOffset NowUtc = new(2026, 6, 10, 12, 0, 0, TimeSpan.Zero);

    private const string CurrentJson =
        """{"main":{"temp":17.4,"feels_like":16.8,"humidity":72},"wind":{"speed":3.5},"weather":[{"id":800,"description":"klarer himmel"}]}""";

    private static string ForecastJson()
    {
        static long Unix(int day, int hour) =>
            new DateTimeOffset(2026, 6, day, hour, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();

        return $$"""
        {
          "list": [
            { "dt": {{Unix(10, 13)}}, "main": {"temp":18,"feels_like":18}, "weather":[{"id":800,"description":"klarer himmel"}], "pop":0.1 },
            { "dt": {{Unix(10, 16)}}, "main": {"temp":20,"feels_like":20}, "weather":[{"id":803,"description":"überwiegend bewölkt"}], "pop":0.2 },
            { "dt": {{Unix(11, 7)}},  "main": {"temp":12,"feels_like":11}, "weather":[{"id":500,"description":"leichter regen"}], "pop":0.6 }
          ]
        }
        """;
    }

    private static OpenWeatherMapClient CreateClient()
    {
        var handler = new StubHttpMessageHandler(request =>
            request.RequestUri!.AbsolutePath.Contains("forecast", StringComparison.Ordinal)
                ? StubHttpMessageHandler.Json(ForecastJson())
                : StubHttpMessageHandler.Json(CurrentJson));

        var http = new HttpClient(handler) { BaseAddress = new Uri("https://test.local/") };
        var clock = new FakeClock { UtcNow = NowUtc };
        var options = Options.Create(new WeatherOptions { ApiKey = "test-key", HourlyCount = 4 });

        return new OpenWeatherMapClient(http, clock, options);
    }

    [Fact]
    public async Task GetWeatherAsync_MapsCurrentWeather()
    {
        var snapshot = await CreateClient().GetWeatherAsync();

        Assert.Equal(17.4, snapshot.Current.Temperature);
        Assert.Equal(16.8, snapshot.Current.FeelsLike);
        Assert.Equal(72, snapshot.Current.Humidity);
        Assert.Equal(3.5, snapshot.Current.WindSpeedMs);
        Assert.Equal(WeatherCondition.Clear, snapshot.Current.Condition);
        Assert.Equal("Klarer himmel", snapshot.Current.Description); // erster Buchstabe großgeschrieben
    }

    [Fact]
    public async Task GetWeatherAsync_AggregatesTodayFromForecastSteps()
    {
        var snapshot = await CreateClient().GetWeatherAsync();

        // Min bezieht die aktuelle Temperatur (17,4) ein – niedriger als die Forecast-Schritte (18/20).
        Assert.Equal(17.4, snapshot.Today.MinTemperature);
        Assert.Equal(20, snapshot.Today.MaxTemperature);
        Assert.Equal(0.2, snapshot.Today.PrecipitationProbability);
        Assert.Equal(WeatherCondition.Clouds, snapshot.Today.Condition); // Clear vs Clouds → Gleichstand → schwerer
    }

    [Fact]
    public async Task GetWeatherAsync_AggregatesTomorrowFromForecastSteps()
    {
        var snapshot = await CreateClient().GetWeatherAsync();

        Assert.NotNull(snapshot.Tomorrow);
        Assert.Equal(WeatherCondition.Rain, snapshot.Tomorrow!.Condition);
        Assert.Equal(0.6, snapshot.Tomorrow.PrecipitationProbability);
        Assert.Equal(12, snapshot.Tomorrow.MaxTemperature);
    }

    [Fact]
    public async Task GetWeatherAsync_BuildsHourlyPreviewFromFutureSteps()
    {
        var snapshot = await CreateClient().GetWeatherAsync();

        Assert.Equal(3, snapshot.Hourly.Count);
        Assert.Equal(18, snapshot.Hourly[0].Temperature);
        Assert.Equal(15, snapshot.Hourly[0].Time.Hour); // 13:00 UTC → 15:00 Berlin
    }

    [Fact]
    public async Task GetWeatherAsync_SendsApiKeyAndCoordinates()
    {
        Uri? captured = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            captured ??= request.RequestUri;
            return request.RequestUri!.AbsolutePath.Contains("forecast", StringComparison.Ordinal)
                ? StubHttpMessageHandler.Json(ForecastJson())
                : StubHttpMessageHandler.Json(CurrentJson);
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://test.local/") };
        var client = new OpenWeatherMapClient(
            http,
            new FakeClock { UtcNow = NowUtc },
            Options.Create(new WeatherOptions { ApiKey = "secret-123", Latitude = 53.55, Longitude = 9.99 }));

        await client.GetWeatherAsync();

        Assert.NotNull(captured);
        Assert.Contains("appid=secret-123", captured!.Query, StringComparison.Ordinal);
        Assert.Contains("lat=53.55", captured.Query, StringComparison.Ordinal);
        Assert.Contains("lon=9.99", captured.Query, StringComparison.Ordinal);
    }
}
