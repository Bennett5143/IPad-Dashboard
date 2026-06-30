namespace Dashboard.Domain.Crypto;

/// <summary>
/// Marktstimmung aus dem Fear-&amp;-Greed-Index (0–100) samt abgeleiteter
/// <see cref="MarketMood"/>. Schwellen: ≥55 bullisch, ≤44 bärisch, dazwischen neutral.
/// </summary>
public sealed record MarketSentiment(int Value, string Classification)
{
    public MarketMood Mood => Value switch
    {
        >= 55 => MarketMood.Bullish,
        <= 44 => MarketMood.Bearish,
        _ => MarketMood.Neutral
    };
}
