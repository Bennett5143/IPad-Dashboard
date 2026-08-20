using Microsoft.Extensions.Options;

namespace Dashboard.Tests.Crypto;

public class CoinGeckoClientTests
{
    private const string MarketsJson =
        """
        [
          { "id": "bitcoin", "symbol": "btc", "name": "Bitcoin",
            "image": "https://coin-images.coingecko.com/coins/images/1/large/bitcoin.png",
            "current_price": 51248.0, "price_change_percentage_24h": -1.52,
            "sparkline_in_7d": { "price": [50000.0, 50500.0, 51248.0] } },
          { "id": "ethereum", "symbol": "eth", "name": "Ethereum",
            "current_price": null, "price_change_percentage_24h": 0.8,
            "sparkline_in_7d": { "price": [] } }
        ]
        """;

    private static CoinGeckoClient CreateClient(out List<string> requestedPaths)
    {
        var paths = requestedPaths = [];
        var handler = new StubHttpMessageHandler(request =>
        {
            paths.Add(request.RequestUri!.PathAndQuery);
            return StubHttpMessageHandler.Json(MarketsJson);
        });

        var http = new HttpClient(handler) { BaseAddress = new Uri("https://test.local/") };
        var options = Options.Create(new CryptoOptions
        {
            VsCurrency = "usd",
            CoinIds = ["bitcoin", "ethereum"]
        });

        return new CoinGeckoClient(http, options);
    }

    [Fact]
    public async Task GetMarketAsync_ParsesCoins_UppercasesSymbol_AndMapsSparkline()
    {
        var coins = await CreateClient(out _).GetMarketAsync();

        // Eintrag ohne Kurs (current_price=null) wird verworfen.
        var btc = Assert.Single(coins);
        Assert.Equal("bitcoin", btc.Id);
        Assert.Equal("BTC", btc.Symbol);
        Assert.Equal("Bitcoin", btc.Name);
        Assert.Equal(51248m, btc.Price);
        Assert.Equal(-1.52, btc.Change24hPct, precision: 3);
        Assert.Equal("https://coin-images.coingecko.com/coins/images/1/large/bitcoin.png", btc.ImageUrl);
        Assert.Equal(3, btc.Sparkline7d.Count);
        Assert.Equal(51248.0, btc.Sparkline7d[^1]);
    }

    [Fact]
    public async Task GetMarketAsync_RequestsConfiguredCurrencyAndIds()
    {
        await CreateClient(out var paths).GetMarketAsync();

        var path = Assert.Single(paths);
        Assert.Contains("vs_currency=usd", path, StringComparison.Ordinal);
        Assert.Contains("ids=bitcoin%2Cethereum", path, StringComparison.Ordinal);
        Assert.Contains("sparkline=true", path, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetMarketAsync_WithoutCoinIds_SkipsCallAndReturnsEmpty()
    {
        var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("darf nicht aufrufen"));
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://test.local/") };
        var client = new CoinGeckoClient(http, Options.Create(new CryptoOptions { CoinIds = [] }));

        Assert.Empty(await client.GetMarketAsync());
    }
}
