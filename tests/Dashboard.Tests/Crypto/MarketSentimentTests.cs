namespace Dashboard.Tests.Crypto;

public class MarketSentimentTests
{
    [Theory]
    [InlineData(100, MarketMood.Bullish)]
    [InlineData(55, MarketMood.Bullish)]   // untere Bullish-Grenze
    [InlineData(54, MarketMood.Neutral)]
    [InlineData(50, MarketMood.Neutral)]
    [InlineData(45, MarketMood.Neutral)]
    [InlineData(44, MarketMood.Bearish)]   // obere Bearish-Grenze
    [InlineData(0, MarketMood.Bearish)]
    public void Mood_MapsAcrossThresholds(int value, MarketMood expected)
    {
        var sentiment = new MarketSentiment(value, "egal");

        Assert.Equal(expected, sentiment.Mood);
    }
}
