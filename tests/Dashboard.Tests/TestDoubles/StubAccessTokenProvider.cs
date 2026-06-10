namespace Dashboard.Tests.TestDoubles;

internal sealed class StubAccessTokenProvider : IStravaAccessTokenProvider
{
    private readonly string? _token;

    public StubAccessTokenProvider(string? token) => _token = token;

    public Task<string?> GetValidAccessTokenAsync(CancellationToken ct = default) => Task.FromResult(_token);
}
