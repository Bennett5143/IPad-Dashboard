namespace Dashboard.Tests.Crypto;

public class CoinQuoteTests
{
    [Theory]
    [InlineData(2.5, MarketMood.Bullish)]
    [InlineData(-2.5, MarketMood.Bearish)]
    [InlineData(0.0, MarketMood.Neutral)]
    public void Direction_FollowsSignOfChange(double change, MarketMood expected)
    {
        var coin = new CoinQuote("bitcoin", "BTC", "Bitcoin", 50000m, change, null, []);

        Assert.Equal(expected, coin.Direction);
    }

    [Fact]
    public void Summary_PrefersConfiguredId_FallsBackToFirst()
    {
        var btc = new CoinQuote("bitcoin", "BTC", "Bitcoin", 50000m, 1, 2m, []);
        var eth = new CoinQuote("ethereum", "ETH", "Ethereum", 3000m, 1, 1m, []);

        var bySummary = new CryptoSnapshot([eth, btc], null, "bitcoin", default);
        Assert.Same(btc, bySummary.Summary);

        // Konfigurierte Id fehlt → erste (kapitalstärkste) Münze.
        var fallback = new CryptoSnapshot([eth, btc], null, "dogecoin", default);
        Assert.Same(eth, fallback.Summary);

        Assert.Null(new CryptoSnapshot([], null, "bitcoin", default).Summary);
    }
}
