namespace Dashboard.Domain.Whoop;

/// <summary>Serverseitige Persistenz der WHOOP-OAuth-Tokens. Single-User: genau ein Token-Satz.</summary>
public interface IWhoopTokenStore
{
    Task<WhoopTokenSet?> GetAsync(CancellationToken ct = default);
    Task SaveAsync(WhoopTokenSet tokens, CancellationToken ct = default);
    Task<bool> HasTokensAsync(CancellationToken ct = default);
}
