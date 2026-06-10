namespace Dashboard.Tests.TestDoubles;

internal sealed class FakeStravaTokenStore : IStravaTokenStore
{
    private StravaTokenSet? _tokens;

    public FakeStravaTokenStore(StravaTokenSet? initial = null) => _tokens = initial;

    public StravaTokenSet? Current => _tokens;

    public Task<StravaTokenSet?> GetAsync(CancellationToken ct = default) => Task.FromResult(_tokens);

    public Task SaveAsync(StravaTokenSet tokens, CancellationToken ct = default)
    {
        _tokens = tokens;
        return Task.CompletedTask;
    }

    public Task<bool> HasTokensAsync(CancellationToken ct = default) => Task.FromResult(_tokens is not null);
}
