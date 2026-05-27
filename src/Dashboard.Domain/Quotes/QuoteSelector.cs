namespace Dashboard.Domain.Quotes;

public static class QuoteSelector
{
    public static int SelectOrdinal(DateOnly date, int quoteCount)
    {
        if (quoteCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(quoteCount),
                "Quote pool must contain at least one entry.");

        return (date.DayOfYear - 1) % quoteCount;
    }
}
