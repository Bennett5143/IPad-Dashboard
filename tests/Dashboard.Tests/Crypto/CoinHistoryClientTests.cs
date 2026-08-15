using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Dashboard.Tests.Crypto;

/// <summary>
/// Die Kursverläufe der Detailseite. Entscheidend ist nicht nur, dass sie geparst werden, sondern
/// dass sie <em>nicht</em> abgerufen werden, solange niemand hinsieht — die Quelle ist frei und
/// entsprechend hart gedrosselt.
/// </summary>
public class CoinHistoryClientTests
{
    private static long Ms(int day, int hour, int minute = 0) =>
        new DateTimeOffset(2026, 8, day, hour, minute, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();

    private static string Chart(params (long Ms, decimal Price)[] points) =>
        "{\"prices\":[" + string.Join(',', points.Select(p =>
            $"[{p.Ms},{p.Price.ToString(System.Globalization.CultureInfo.InvariantCulture)}]")) + "]}";

    private static CoinHistoryClient Client(
        string json, out List<string> paths, System.Net.HttpStatusCode? status = null)
    {
        var requested = paths = [];
        var handler = new StubHttpMessageHandler(request =>
        {
            requested.Add(request.RequestUri!.PathAndQuery);
            return status is { } code
                ? new HttpResponseMessage(code)
                : StubHttpMessageHandler.Json(json);
        });

        return new CoinHistoryClient(
            new HttpClient(handler) { BaseAddress = new Uri("https://test.local/") },
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new CryptoOptions { VsCurrency = "eur" }),
            NullLogger<CoinHistoryClient>.Instance);
    }

    [Theory]
    [InlineData(CoinRange.Hour, "days=1")]
    [InlineData(CoinRange.Day, "days=1")]
    [InlineData(CoinRange.Week, "days=7")]
    [InlineData(CoinRange.Month, "days=30")]
    [InlineData(CoinRange.Year, "days=365")]
    public async Task GetHistoryAsync_AsksForTheWindowOfItsRange(CoinRange range, string expected)
    {
        var json = Chart((Ms(10, 12), 100m), (Ms(11, 12), 110m));

        await Client(json, out var paths).GetHistoryAsync("bitcoin", range);

        var path = Assert.Single(paths);
        Assert.Contains("coins/bitcoin/market_chart", path, StringComparison.Ordinal);
        Assert.Contains(expected, path, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetHistoryAsync_SecondCallForTheSameRange_IsServedFromTheCache()
    {
        var json = Chart((Ms(10, 12), 100m), (Ms(11, 12), 110m));
        var client = Client(json, out var paths);

        await client.GetHistoryAsync("bitcoin", CoinRange.Week);
        var again = await client.GetHistoryAsync("bitcoin", CoinRange.Week);

        Assert.Single(paths);          // kein zweiter HTTP-Aufruf
        Assert.True(again.HasPoints);
    }

    [Fact]
    public async Task GetHistoryAsync_ADifferentRange_IsFetchedSeparately()
    {
        var json = Chart((Ms(10, 12), 100m), (Ms(11, 12), 110m));
        var client = Client(json, out var paths);

        await client.GetHistoryAsync("bitcoin", CoinRange.Week);
        await client.GetHistoryAsync("bitcoin", CoinRange.Month);

        Assert.Equal(2, paths.Count);
    }

    /// <summary>Die Stunde schneidet aus dem Tagesfenster; CoinGecko kennt kein Stundenfenster.</summary>
    [Fact]
    public async Task GetHistoryAsync_Hour_KeepsOnlyTheLastHourOfTheDayWindow()
    {
        var json = Chart(
            (Ms(11, 8), 100m), (Ms(11, 9), 101m), (Ms(11, 10), 102m),
            (Ms(11, 11, 30), 103m), (Ms(11, 12), 104m));

        var history = await Client(json, out _).GetHistoryAsync("bitcoin", CoinRange.Hour);

        Assert.Equal(2, history.Points.Count);   // 11:30 und 12:00
        Assert.Equal(103m, history.Points[0].Price);
    }

    /// <summary>Weniger als zwei Punkte ergeben keine Linie — dann lieber der ganze Tag.</summary>
    [Fact]
    public async Task GetHistoryAsync_Hour_WithoutEnoughPoints_KeepsTheWholeDay()
    {
        var json = Chart((Ms(11, 6), 100m), (Ms(11, 7), 101m), (Ms(11, 12), 104m));

        var history = await Client(json, out _).GetHistoryAsync("bitcoin", CoinRange.Hour);

        Assert.Equal(3, history.Points.Count);
    }

    [Fact]
    public async Task GetHistoryAsync_WhenTheSourceFails_ReturnsEmptyInsteadOfThrowing()
    {
        var client = Client("", out _, System.Net.HttpStatusCode.TooManyRequests);

        var history = await client.GetHistoryAsync("bitcoin", CoinRange.Day);

        Assert.False(history.HasPoints);
        Assert.Equal("bitcoin", history.CoinId);
    }

    [Fact]
    public async Task GetHistoryAsync_AFailedFetchIsNotCached()
    {
        var client = Client("", out var paths, System.Net.HttpStatusCode.TooManyRequests);

        await client.GetHistoryAsync("bitcoin", CoinRange.Day);
        await client.GetHistoryAsync("bitcoin", CoinRange.Day);

        Assert.Equal(2, paths.Count); // der nächste Aufruf versucht es erneut
    }

    [Fact]
    public async Task GetHistoryAsync_WithoutACoinId_SkipsTheCall()
    {
        var client = Client("", out var paths);

        var history = await client.GetHistoryAsync("", CoinRange.Day);

        Assert.False(history.HasPoints);
        Assert.Empty(paths);
    }

    [Fact]
    public async Task GetHistoryAsync_ExposesTheExtremesForTheAxes()
    {
        var json = Chart((Ms(10, 12), 100m), (Ms(11, 12), 130m), (Ms(12, 12), 90m));

        var history = await Client(json, out _).GetHistoryAsync("bitcoin", CoinRange.Week);

        Assert.Equal(90m, history.Minimum);
        Assert.Equal(130m, history.Maximum);
        Assert.Equal(3, history.Points.Count);
    }
}
