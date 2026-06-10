using Microsoft.Extensions.Options;

namespace Dashboard.Tests.Strava;

public class StravaTokenServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 10, 12, 0, 0, TimeSpan.Zero);

    private static StravaOptions DefaultOptions => new()
    {
        ClientId = "cid",
        ClientSecret = "secret",
        RedirectUri = "http://localhost:5235/strava/callback",
        Scope = "activity:read_all",
        BaseUrl = "https://www.strava.com/"
    };

    private static string TokenJson(string access, string refresh, DateTimeOffset expires) =>
        $$"""{"access_token":"{{access}}","refresh_token":"{{refresh}}","expires_at":{{expires.ToUnixTimeSeconds()}}}""";

    private static StravaTokenService Service(FakeStravaTokenStore store, HttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://www.strava.com/") },
            store, new FakeClock { UtcNow = Now }, Options.Create(DefaultOptions));

    [Fact]
    public async Task GetValidAccessToken_ReturnsNull_WhenNoTokens()
    {
        var service = Service(
            new FakeStravaTokenStore(),
            new StubHttpMessageHandler(_ => throw new InvalidOperationException("kein HTTP erwartet")));

        Assert.Null(await service.GetValidAccessTokenAsync());
    }

    [Fact]
    public async Task GetValidAccessToken_ReturnsExisting_WhenNotExpiring()
    {
        var store = new FakeStravaTokenStore(new StravaTokenSet("good", "r", Now.AddHours(5)));
        var service = Service(
            store, new StubHttpMessageHandler(_ => throw new InvalidOperationException("kein Refresh erwartet")));

        Assert.Equal("good", await service.GetValidAccessTokenAsync());
    }

    [Fact]
    public async Task GetValidAccessToken_RefreshesAndPersists_WhenExpiringSoon()
    {
        var store = new FakeStravaTokenStore(new StravaTokenSet("old", "oldr", Now.AddMinutes(30)));
        var service = Service(
            store, new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json(TokenJson("new", "newr", Now.AddHours(6)))));

        var token = await service.GetValidAccessTokenAsync();

        Assert.Equal("new", token);
        Assert.Equal("new", store.Current!.AccessToken);
        Assert.Equal("newr", store.Current.RefreshToken); // neues Refresh-Token persistiert
    }

    [Fact]
    public async Task ExchangeCode_PersistsTokens()
    {
        var store = new FakeStravaTokenStore();
        var service = Service(
            store, new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json(TokenJson("acc", "ref", Now.AddHours(6)))));

        await service.ExchangeCodeAsync("auth-code");

        Assert.NotNull(store.Current);
        Assert.Equal("acc", store.Current!.AccessToken);
    }

    [Fact]
    public void BuildAuthorizeUrl_ContainsClientRedirectScopeAndState()
    {
        var service = Service(new FakeStravaTokenStore(), new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json("{}")));

        var url = service.BuildAuthorizeUrl("abc123").ToString();

        Assert.Contains("client_id=cid", url, StringComparison.Ordinal);
        Assert.Contains("redirect_uri=", url, StringComparison.Ordinal);
        Assert.Contains("activity%3Aread_all", url, StringComparison.Ordinal); // Scope url-codiert
        Assert.Contains("state=abc123", url, StringComparison.Ordinal);        // Anti-CSRF
    }
}
