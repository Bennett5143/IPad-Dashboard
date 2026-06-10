using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using Dashboard.Domain.Running;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dashboard.Infrastructure.Strava;

/// <summary>
/// <see cref="IStravaActivityProvider"/> gegen die Strava-API v3. Paginiert die Aktivitätenliste,
/// filtert auf Läufe (FA-8.06) und dekodiert die Polyline. Bei HTTP 429 wird die Paginierung
/// abgebrochen und das bis dahin Geholte zurückgegeben (pausieren statt abbrechen, FA-8.08).
/// </summary>
public sealed class StravaClient : IStravaActivityProvider
{
    private readonly HttpClient _http;
    private readonly IStravaAccessTokenProvider _tokenProvider;
    private readonly StravaOptions _options;
    private readonly ILogger<StravaClient> _logger;

    public StravaClient(
        HttpClient http,
        IStravaAccessTokenProvider tokenProvider,
        IOptions<StravaOptions> options,
        ILogger<StravaClient> logger)
    {
        _http = http;
        _tokenProvider = tokenProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Run>> GetActivitiesAsync(DateTimeOffset? afterUtc, CancellationToken ct = default)
    {
        var token = await _tokenProvider.GetValidAccessTokenAsync(ct)
            ?? throw new InvalidOperationException("Strava ist nicht verbunden (kein Token).");

        var afterEpoch = afterUtc?.ToUnixTimeSeconds() ?? 0;
        var runs = new List<Run>();

        for (var page = 1; ; page++)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"api/v3/athlete/activities?after={afterEpoch}&page={page}&per_page={_options.PerPage}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _http.SendAsync(request, ct);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("Strava: Rate-Limit erreicht – Sync pausiert, {Count} Läufe bis hier geholt.", runs.Count);
                break;
            }

            response.EnsureSuccessStatusCode();

            var activities = await response.Content.ReadFromJsonAsync<List<StravaActivityDto>>(ct) ?? [];
            if (activities.Count == 0)
            {
                break;
            }

            foreach (var activity in activities)
            {
                var type = activity.Type ?? activity.SportType;
                if (RunTypes.IsRun(type))
                {
                    runs.Add(Map(activity, type!));
                }
            }

            if (activities.Count < _options.PerPage)
            {
                break; // letzte Seite
            }
        }

        return runs;
    }

    private static Run Map(StravaActivityDto activity, string type) => new(
        activity.Id,
        activity.Name ?? string.Empty,
        type,
        activity.StartDate,
        activity.Distance,
        TimeSpan.FromSeconds(activity.MovingTime),
        PolylineDecoder.Decode(activity.Map?.SummaryPolyline));
}
