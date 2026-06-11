using System.Net.Http.Json;

using Dashboard.Domain.Time;
using Dashboard.Domain.Whoop;

using Microsoft.Extensions.Options;

namespace Dashboard.Infrastructure.Whoop;

/// <summary>
/// Verwaltet den WHOOP-OAuth2-Lebenszyklus: Authorize-URL bauen, Code gegen Tokens tauschen und
/// gültige Access-Tokens liefern (transparenter Refresh). WHOOP rotiert beide Tokens je Refresh –
/// der <c>scope=offline</c>-Parameter beim Refresh sorgt dafür, dass ein neues Refresh-Token kommt.
/// </summary>
public sealed class WhoopTokenService : IWhoopAccessTokenProvider
{
    private readonly HttpClient _http;
    private readonly IWhoopTokenStore _store;
    private readonly IClock _clock;
    private readonly WhoopOptions _options;

    public WhoopTokenService(
        HttpClient http, IWhoopTokenStore store, IClock clock, IOptions<WhoopOptions> options)
    {
        _http = http;
        _store = store;
        _clock = clock;
        _options = options.Value;
    }

    /// <param name="state">Anti-CSRF-Token; muss im Callback gegen den vom Server gesetzten Wert geprüft werden.</param>
    public Uri BuildAuthorizeUrl(string state) => new(
        $"{_options.BaseUrl}oauth/oauth2/auth" +
        $"?client_id={Uri.EscapeDataString(_options.ClientId)}" +
        $"&redirect_uri={Uri.EscapeDataString(_options.RedirectUri)}" +
        "&response_type=code" +
        $"&scope={Uri.EscapeDataString(_options.Scope)}" +
        $"&state={Uri.EscapeDataString(state)}");

    public async Task ExchangeCodeAsync(string code, CancellationToken ct = default)
    {
        var tokens = await PostTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["redirect_uri"] = _options.RedirectUri
        }, ct);

        await _store.SaveAsync(tokens, ct);
    }

    public async Task<string?> GetValidAccessTokenAsync(CancellationToken ct = default)
    {
        var tokens = await _store.GetAsync(ct);
        if (tokens is null)
        {
            return null;
        }

        if (!tokens.NeedsRefresh(_clock.UtcNow))
        {
            return tokens.AccessToken;
        }

        var refreshed = await PostTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = tokens.RefreshToken,
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["scope"] = "offline"
        }, ct);

        await _store.SaveAsync(refreshed, ct);
        return refreshed.AccessToken;
    }

    private async Task<WhoopTokenSet> PostTokenAsync(Dictionary<string, string> form, CancellationToken ct)
    {
        using var content = new FormUrlEncodedContent(form);
        using var response = await _http.PostAsync("oauth/oauth2/token", content, ct);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<WhoopTokenResponse>(ct)
            ?? throw new InvalidOperationException("Leere Token-Antwort von WHOOP.");

        return new WhoopTokenSet(dto.AccessToken, dto.RefreshToken, _clock.UtcNow.AddSeconds(dto.ExpiresIn));
    }
}
