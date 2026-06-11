namespace Dashboard.Domain.Whoop;

/// <summary>Liefert ein gültiges Access-Token (transparenter Refresh) für WHOOP-API-Aufrufe.</summary>
public interface IWhoopAccessTokenProvider
{
    /// <returns>Gültiges Access-Token oder <c>null</c>, wenn WHOOP nicht verbunden ist.</returns>
    Task<string?> GetValidAccessTokenAsync(CancellationToken ct = default);
}
