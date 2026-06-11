using Dashboard.Domain.Whoop;
using Dashboard.Infrastructure.Whoop;

using Microsoft.Extensions.Options;

namespace Dashboard.Tests.Whoop;

public class WhoopTokenServiceTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 6, 11, 12, 0, 0, TimeSpan.Zero);

    private const string TokenJson =
        """
        { "access_token": "new-access", "refresh_token": "new-refresh", "expires_in": 3600,
          "token_type": "bearer", "scope": "offline read:recovery" }
        """;

    private sealed class InMemoryTokenStore : IWhoopTokenStore
    {
        public WhoopTokenSet? Tokens;
        public Task<WhoopTokenSet?> GetAsync(CancellationToken ct = default) => Task.FromResult(Tokens);
        public Task SaveAsync(WhoopTokenSet tokens, CancellationToken ct = default)
        {
            Tokens = tokens;
            return Task.CompletedTask;
        }
        public Task<bool> HasTokensAsync(CancellationToken ct = default) => Task.FromResult(Tokens is not null);
    }

    private static (WhoopTokenService Service, InMemoryTokenStore Store) Create(
        Func<HttpRequestMessage, HttpResponseMessage>? responder = null)
    {
        var handler = new StubHttpMessageHandler(responder ?? (_ => StubHttpMessageHandler.Json(TokenJson)));
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.prod.whoop.com/") };
        var store = new InMemoryTokenStore();
        var options = Options.Create(new WhoopOptions
        {
            ClientId = "client-123",
            ClientSecret = "secret-xyz",
            RedirectUri = "http://localhost:5235/whoop/callback",
            Scope = "offline read:recovery",
            BaseUrl = "https://api.prod.whoop.com/"
        });

        return (new WhoopTokenService(http, store, new FakeClock { UtcNow = NowUtc }, options), store);
    }

    [Fact]
    public void BuildAuthorizeUrl_ContainsClientRedirectScopeAndState()
    {
        var url = Create().Service.BuildAuthorizeUrl("state-abc").ToString();

        Assert.Contains("oauth/oauth2/auth", url, StringComparison.Ordinal);
        Assert.Contains("client_id=client-123", url, StringComparison.Ordinal);
        Assert.Contains("response_type=code", url, StringComparison.Ordinal);
        Assert.Contains("state=state-abc", url, StringComparison.Ordinal);
        Assert.Contains("offline read:recovery", Uri.UnescapeDataString(url), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExchangeCodeAsync_StoresTokens_WithAbsoluteExpiry()
    {
        var (service, store) = Create();

        await service.ExchangeCodeAsync("auth-code");

        Assert.NotNull(store.Tokens);
        Assert.Equal("new-access", store.Tokens!.AccessToken);
        Assert.Equal("new-refresh", store.Tokens.RefreshToken);
        Assert.Equal(NowUtc.AddSeconds(3600), store.Tokens.ExpiresAtUtc);
    }

    [Fact]
    public async Task GetValidAccessTokenAsync_ReturnsStored_WhenNotExpiring()
    {
        var (service, store) = Create(_ => throw new InvalidOperationException("darf nicht refreshen"));
        store.Tokens = new WhoopTokenSet("still-good", "r", NowUtc.AddHours(2));

        Assert.Equal("still-good", await service.GetValidAccessTokenAsync());
    }

    [Fact]
    public async Task GetValidAccessTokenAsync_Refreshes_WhenNearExpiry_AndRotatesBothTokens()
    {
        var (service, store) = Create();
        store.Tokens = new WhoopTokenSet("old", "old-refresh", NowUtc.AddMinutes(2));

        var token = await service.GetValidAccessTokenAsync();

        Assert.Equal("new-access", token);
        Assert.Equal("new-refresh", store.Tokens!.RefreshToken);
    }

    [Fact]
    public async Task GetValidAccessTokenAsync_ReturnsNull_WhenNotConnected()
    {
        Assert.Null(await Create().Service.GetValidAccessTokenAsync());
    }
}
