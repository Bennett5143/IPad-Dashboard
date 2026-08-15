using System.Globalization;

using Microsoft.Extensions.Options;

namespace Dashboard.Tests.Crypto;

/// <summary>
/// Die Tagesreihe hinter dem Badge im Wochenkalender. CoinGecko liefert im Acht-Tage-Fenster
/// stündliche Punkte; daraus zieht der Client die Tagesgrenzen in Berliner Zeit.
/// </summary>
public class CoinGeckoHistoryTests
{
    private static long Ms(int year, int month, int day, int hourBerlin)
    {
        var berlin = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
        var local = new DateTime(year, month, day, hourBerlin, 0, 0, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, berlin.GetUtcOffset(local)).ToUnixTimeMilliseconds();
    }

    private static string Chart(params (long Ms, decimal Price)[] points) =>
        "{\"prices\":[" + string.Join(',', points.Select(p =>
            $"[{p.Ms},{p.Price.ToString(CultureInfo.InvariantCulture)}]")) + "]}";

    private static CoinGeckoClient Client(string json, out List<string> paths)
    {
        var requested = paths = [];
        var handler = new StubHttpMessageHandler(request =>
        {
            requested.Add(request.RequestUri!.PathAndQuery);
            return StubHttpMessageHandler.Json(json);
        });

        return new CoinGeckoClient(
            new HttpClient(handler) { BaseAddress = new Uri("https://test.local/") },
            Options.Create(new CryptoOptions { VsCurrency = "eur", SummaryCoinId = "bitcoin" }));
    }

    [Fact]
    public async Task GetDailyChangesAsync_DerivesOneChangePerDay_FromTheLastPriceOfEachDay()
    {
        // Zwei Punkte je Tag: der spätere ist der Tagesschluss.
        var json = Chart(
            (Ms(2026, 6, 8, 10), 100m), (Ms(2026, 6, 8, 23), 100m),
            (Ms(2026, 6, 9, 10), 105m), (Ms(2026, 6, 9, 23), 110m),
            (Ms(2026, 6, 10, 12), 99m));

        var days = await Client(json, out _).GetDailyChangesAsync();

        Assert.Equal(2, days.Count); // der erste Tag ist nur Vergleichswert

        Assert.Equal(new DateOnly(2026, 6, 9), days[0].Date);
        Assert.Equal(10m, Assert.Single(days[0].Changes).ChangePercent);   // 100 → 110

        Assert.Equal(new DateOnly(2026, 6, 10), days[1].Date);
        Assert.Equal(-10m, Assert.Single(days[1].Changes).ChangePercent);  // 110 → 99
    }

    [Fact]
    public async Task GetDailyChangesAsync_LabelsTheChangeWithTheCoinSymbol()
    {
        var json = Chart((Ms(2026, 6, 8, 12), 100m), (Ms(2026, 6, 9, 12), 101m));

        var days = await Client(json, out _).GetDailyChangesAsync();

        Assert.Equal("BTC", Assert.Single(days[0].Changes).Symbol);
    }

    /// <summary>
    /// <c>interval=daily</c> ist kostenpflichtigen Plänen vorbehalten — der Aufruf darf ihn nicht
    /// mitschicken, sonst kommt auf dem freien Zugang nichts zurück.
    /// </summary>
    [Fact]
    public async Task GetDailyChangesAsync_RequestsEightDays_WithoutTheIntervalParameter()
    {
        var json = Chart((Ms(2026, 6, 8, 12), 100m), (Ms(2026, 6, 9, 12), 101m));

        await Client(json, out var paths).GetDailyChangesAsync();

        var path = Assert.Single(paths);
        Assert.Contains("coins/bitcoin/market_chart", path, StringComparison.Ordinal);
        Assert.Contains("days=8", path, StringComparison.Ordinal);
        Assert.Contains("vs_currency=eur", path, StringComparison.Ordinal);
        Assert.DoesNotContain("interval", path, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetDailyChangesAsync_WithASingleDay_ReturnsNothingToCompare()
    {
        var json = Chart((Ms(2026, 6, 8, 10), 100m), (Ms(2026, 6, 8, 20), 102m));

        Assert.Empty(await Client(json, out _).GetDailyChangesAsync());
    }

    [Fact]
    public async Task GetDailyChangesAsync_WithoutASummaryCoin_SkipsTheCall()
    {
        var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("kein Aufruf erwartet"));
        var client = new CoinGeckoClient(
            new HttpClient(handler) { BaseAddress = new Uri("https://test.local/") },
            Options.Create(new CryptoOptions { SummaryCoinId = "" }));

        Assert.Empty(await client.GetDailyChangesAsync());
    }
}
