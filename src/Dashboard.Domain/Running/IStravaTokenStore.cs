namespace Dashboard.Domain.Running;

/// <summary>Serverseitige Persistenz der OAuth-Tokens (FA-8.02). Single-User: genau ein Token-Satz.</summary>
public interface IStravaTokenStore
{
    Task<StravaTokenSet?> GetAsync(CancellationToken ct = default);
    Task SaveAsync(StravaTokenSet tokens, CancellationToken ct = default);
    Task<bool> HasTokensAsync(CancellationToken ct = default);
}
