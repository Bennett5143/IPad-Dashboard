namespace Dashboard.Domain.Crypto;

/// <summary>
/// Markt-Momentaufnahme einer Kryptowährung (reine Watchlist-Sicht, kein Portfolio).
/// Die 7-Tage-Sparkline wird serverseitig im Refresh mitgeholt (offline-iPad lädt nichts nach).
/// </summary>
public sealed record CoinQuote(
    string Id,
    string Symbol,
    string Name,
    // Kurs in der konfigurierten Währung (Crypto:VsCurrency). Bewusst ohne Währung im Namen:
    // ein Feld, das eine Währung behauptet, die es nicht mehr führt, ist ein Fehler in Wartestellung.
    decimal Price,
    double Change24hPct,
    // Logo des Coins beim Anbieter; geladen über den Bild-Proxy, nie direkt vom iPad.
    string? ImageUrl,
    IReadOnlyList<double?> Sparkline7d)
{
    /// <summary>24-h-Kursrichtung für Vorzeichen/Farbe.</summary>
    public MarketMood Direction => Change24hPct switch
    {
        > 0 => MarketMood.Bullish,
        < 0 => MarketMood.Bearish,
        _ => MarketMood.Neutral
    };
}
