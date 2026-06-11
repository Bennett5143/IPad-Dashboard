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

    public async Task<StravaStreams?> GetStreamsAsync(long activityId, CancellationToken ct = default)
    {
        var token = await _tokenProvider.GetValidAccessTokenAsync(ct)
            ?? throw new InvalidOperationException("Strava ist nicht verbunden (kein Token).");

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/v3/activities/{activityId}/streams?keys=latlng,time,altitude,heartrate&key_by_type=true");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _http.SendAsync(request, ct);
        if (response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500)
        {
            throw new StravaRateLimitException($"Strava-Streams pausiert (HTTP {(int)response.StatusCode}).");
        }

        if (!response.IsSuccessStatusCode)
        {
            // z. B. 404 – Activity ohne Streams. Als „abgerufen, keine Daten" behandeln.
            _logger.LogDebug("Strava: Keine Streams für Activity {Id} (HTTP {Status}).", activityId, (int)response.StatusCode);
            return null;
        }

        var set = await response.Content.ReadFromJsonAsync<StravaStreamSet>(ct);
        var latlng = set?.LatLng?.Data;
        if (latlng is null || latlng.Count < 2)
        {
            return null; // ohne GPS-Spur keine nutzbaren Streams (z. B. Indoor-Lauf)
        }

        var track = latlng
            .Where(p => p is { Length: >= 2 })
            .Select(p => new GeoPoint(p[0], p[1]))
            .ToList();
        if (track.Count < 2)
        {
            return null;
        }

        // Höhe/HF nur übernehmen, wenn sie zur latlng-Länge passen (sonst nicht index-aligned).
        var time = Aligned(set!.Time?.Data, latlng.Count) ?? [];
        var altitude = Aligned(set.Altitude?.Data, latlng.Count);
        var heartRate = Aligned(set.HeartRate?.Data, latlng.Count);

        return new StravaStreams(track, time, altitude, heartRate);
    }

    private static IReadOnlyList<T>? Aligned<T>(IReadOnlyList<T>? data, int expectedLength) =>
        data is not null && data.Count == expectedLength ? data : null;

    private static Run Map(StravaActivityDto activity, string type) => new(
        activity.Id,
        activity.Name ?? string.Empty,
        type,
        activity.StartDate,
        activity.Distance,
        TimeSpan.FromSeconds(activity.MovingTime),
        PolylineDecoder.Decode(activity.Map?.SummaryPolyline));
}
