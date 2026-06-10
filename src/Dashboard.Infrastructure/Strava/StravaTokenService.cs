using System.Net.Http.Json;

using Dashboard.Domain.Running;
using Dashboard.Domain.Time;

using Microsoft.Extensions.Options;

namespace Dashboard.Infrastructure.Strava;

/// <summary>
/// Verwaltet den OAuth2-Lebenszyklus: Authorize-URL bauen, Code gegen Tokens tauschen und
/// gültige Access-Tokens liefern (transparenter Refresh, neues Refresh-Token persistieren).
/// </summary>
public sealed class StravaTokenService : IStravaAccessTokenProvider
{
    private readonly HttpClient _http;
    private readonly IStravaTokenStore _store;
    private readonly IClock _clock;
    private readonly StravaOptions _options;

    public StravaTokenService(
        HttpClient http, IStravaTokenStore store, IClock clock, IOptions<StravaOptions> options)
    {
        _http = http;
        _store = store;
        _clock = clock;
        _options = options.Value;
    }

    /// <param name="state">Anti-CSRF-Token; muss im Callback gegen den vom Server gesetzten Wert geprüft werden.</param>
    public Uri BuildAuthorizeUrl(string state) => new(
        $"{_options.BaseUrl}oauth/authorize" +
        $"?client_id={Uri.EscapeDataString(_options.ClientId)}" +
        $"&redirect_uri={Uri.EscapeDataString(_options.RedirectUri)}" +
        "&response_type=code&approval_prompt=auto" +
        $"&scope={Uri.EscapeDataString(_options.Scope)}" +
        $"&state={Uri.EscapeDataString(state)}");

    public async Task ExchangeCodeAsync(string code, CancellationToken ct = default)
    {
        var tokens = await PostTokenAsync(new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["code"] = code,
            ["grant_type"] = "authorization_code"
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
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["refresh_token"] = tokens.RefreshToken,
            ["grant_type"] = "refresh_token"
        }, ct);

        await _store.SaveAsync(refreshed, ct);
        return refreshed.AccessToken;
    }

    private async Task<StravaTokenSet> PostTokenAsync(Dictionary<string, string> form, CancellationToken ct)
    {
        using var content = new FormUrlEncodedContent(form);
        using var response = await _http.PostAsync("oauth/token", content, ct);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<StravaTokenResponse>(ct)
            ?? throw new InvalidOperationException("Leere Token-Antwort von Strava.");

        return new StravaTokenSet(
            dto.AccessToken, dto.RefreshToken, DateTimeOffset.FromUnixTimeSeconds(dto.ExpiresAt));
    }
}
