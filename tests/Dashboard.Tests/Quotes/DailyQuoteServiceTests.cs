namespace Dashboard.Tests.Quotes;

public class DailyQuoteServiceTests
{
    [Fact]
    public async Task GetTodaysQuoteAsync_ReturnsNull_WhenPoolIsEmpty()
    {
        var service = new DailyQuoteService(
            new FakeQuoteRepository(Array.Empty<Quote>()),
            new FakeClock { UtcNow = new DateTimeOffset(2026, 5, 19, 12, 0, 0, TimeSpan.Zero) });

        var result = await service.GetTodaysQuoteAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetTodaysQuoteAsync_SelectsQuoteByDayOfYear()
    {
        var quotes = new[]
        {
            new Quote { Id = 1, Text = "Erstes Zitat" },
            new Quote { Id = 2, Text = "Zweites Zitat" },
            new Quote { Id = 3, Text = "Drittes Zitat" }
        };
        // 3. Januar 2026, 12:00 UTC = 13:00 Berlin → Tag 3 → Index (3-1) % 3 = 2 → Id 3
        var service = new DailyQuoteService(
            new FakeQuoteRepository(quotes),
            new FakeClock { UtcNow = new DateTimeOffset(2026, 1, 3, 12, 0, 0, TimeSpan.Zero) });

        var result = await service.GetTodaysQuoteAsync();

        Assert.NotNull(result);
        Assert.Equal("Drittes Zitat", result.Text);
    }

    [Fact]
    public async Task GetTodaysQuoteAsync_UsesBerlinDate_NotUtcDate()
    {
        var quotes = new[]
        {
            new Quote { Id = 1, Text = "Tag 1" },
            new Quote { Id = 2, Text = "Tag 2" }
        };
        // 1. Januar 2026, 23:30 UTC = 2. Januar, 00:30 Berlin (CET +01:00)
        // → Tag 2, nicht Tag 1
        var service = new DailyQuoteService(
            new FakeQuoteRepository(quotes),
            new FakeClock { UtcNow = new DateTimeOffset(2026, 1, 1, 23, 30, 0, TimeSpan.Zero) });

        var result = await service.GetTodaysQuoteAsync();

        Assert.NotNull(result);
        Assert.Equal("Tag 2", result.Text);
    }
}