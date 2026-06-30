namespace Dashboard.Tests.Crypto;

public class FearGreedClientTests
{
    private static FearGreedClient ClientReturning(string json)
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json(json));
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://test.local/") };
        return new FearGreedClient(http);
    }

    [Fact]
    public async Task GetSentimentAsync_ParsesStringValueAndClassification()
    {
        var client = ClientReturning(
            """
            { "data": [ { "value": "15", "value_classification": "Extreme Fear", "timestamp": "1782777600" } ] }
            """);

        var sentiment = await client.GetSentimentAsync();

        Assert.NotNull(sentiment);
        Assert.Equal(15, sentiment!.Value);
        Assert.Equal("Extreme Fear", sentiment.Classification);
        Assert.Equal(MarketMood.Bearish, sentiment.Mood);
    }

    [Theory]
    [InlineData("""{ "data": [] }""")]
    [InlineData("""{ "data": [ { "value": "n/a", "value_classification": "?" } ] }""")]
    [InlineData("""{ "metadata": { "error": null } }""")]
    public async Task GetSentimentAsync_ReturnsNull_OnEmptyOrMalformed(string json)
    {
        Assert.Null(await ClientReturning(json).GetSentimentAsync());
    }
}
