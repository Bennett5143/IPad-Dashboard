namespace Dashboard.Domain.Crypto;

/// <summary>Liefert die aktuelle Markt-Watchlist – die Pflichtquelle des Krypto-Slices.</summary>
public interface ICryptoMarketProvider
{
    Task<IReadOnlyList<CoinQuote>> GetMarketAsync(CancellationToken ct = default);
}
