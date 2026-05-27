namespace Dashboard.Tests.Quotes;

public class QuoteSelectorTests
{
    [Theory]
    [InlineData(2026, 1, 1, 365, 0)]     // 1. Januar → Index 0
    [InlineData(2026, 12, 31, 365, 364)] // 31. Dezember (Nicht-Schaltjahr) → Index 364
    [InlineData(2026, 5, 19, 365, 138)]  // 19. Mai = Tag 139 → Index 138
    public void SelectOrdinal_MapsDayOfYearMinusOne(int year, int month, int day, int count, int expected)
    {
        var date = new DateOnly(year, month, day);
        Assert.Equal(expected, QuoteSelector.SelectOrdinal(date, count));
    }

    [Fact]
    public void SelectOrdinal_WrapsAroundOnLeapDay()
    {
        // 31.12.2024 ist Tag 366 im Schaltjahr; mit Pool von 365 wickelt es sich auf Index 0
        var date = new DateOnly(2024, 12, 31);
        Assert.Equal(0, QuoteSelector.SelectOrdinal(date, 365));
    }

    [Fact]
    public void SelectOrdinal_WrapsMultipleTimes_WhenPoolIsSmallerThanYear()
    {
        // Bei 30 Zitaten zyklisch durch jeden Monat
        var date = new DateOnly(2026, 2, 1); // Tag 32
        Assert.Equal(1, QuoteSelector.SelectOrdinal(date, 30));
    }

    [Fact]
    public void SelectOrdinal_ThrowsForEmptyOrNegativePool()
    {
        var date = new DateOnly(2026, 1, 1);
        Assert.Throws<ArgumentOutOfRangeException>(() => QuoteSelector.SelectOrdinal(date, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => QuoteSelector.SelectOrdinal(date, -1));
    }
}
