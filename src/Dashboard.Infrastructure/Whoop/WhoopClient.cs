using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using Dashboard.Domain.Time;
using Dashboard.Domain.Whoop;

namespace Dashboard.Infrastructure.Whoop;

/// <summary>
/// <see cref="IWhoopProvider"/> auf Basis der WHOOP-API v2. Holt den jüngsten Recovery-, Schlaf-
/// und Zyklus-Datensatz und löst sie in einen <see cref="WhoopSnapshot"/> auf. GPS/Geodaten gibt
/// es bei WHOOP nicht – hier nur biometrische Tageswerte.
/// </summary>
public sealed class WhoopClient : IWhoopProvider
{
    private readonly HttpClient _http;
    private readonly IWhoopAccessTokenProvider _tokens;
    private readonly IClock _clock;

    public WhoopClient(HttpClient http, IWhoopAccessTokenProvider tokens, IClock clock)
    {
        _http = http;
        _tokens = tokens;
        _clock = clock;
    }

    public async Task<WhoopSnapshot> GetWhoopAsync(CancellationToken ct = default)
    {
        var token = await _tokens.GetValidAccessTokenAsync(ct)
            ?? throw new InvalidOperationException("WHOOP nicht verbunden (kein Token).");

        var recovery = await GetLatestRecoveryAsync(token, ct);
        var sleep = await GetLatestSleepAsync(token, ct);
        var strain = await GetLatestStrainAsync(token, ct);

        return new WhoopSnapshot(recovery, sleep, strain, _clock.UtcNow);
    }

    public async Task<IReadOnlyList<WhoopWorkout>> GetWorkoutsAsync(
        DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct = default)
    {
        var token = await _tokens.GetValidAccessTokenAsync(ct)
            ?? throw new InvalidOperationException("WHOOP nicht verbunden (kein Token).");

        var from = Iso(fromUtc);
        var to = Iso(toUtc);
        var resp = await GetAsync<WhoopCollection<WhoopWorkoutRecord>>(
            $"developer/v2/activity/workout?start={from}&end={to}&limit=25", token, ct);

        return resp.Records
            .Where(r => !string.IsNullOrEmpty(r.Id))
            .Select(r => new WhoopWorkout(
                r.Id,
                r.SportName ?? string.Empty,
                r.Start,
                r.End,
                r.Score?.DistanceMeter,
                r.Score?.ZoneDurations?.HighIntensityShare ?? 0))
            .ToList();
    }

    private static string Iso(DateTimeOffset value) => Uri.EscapeDataString(
        value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture));

    private async Task<WhoopRecovery?> GetLatestRecoveryAsync(string token, CancellationToken ct)
    {
        var resp = await GetAsync<WhoopCollection<WhoopRecoveryRecord>>(
            "developer/v2/recovery?limit=1", token, ct);

        var score = resp.Records.FirstOrDefault(r => r.ScoreState == "SCORED")?.Score;
        return score is null
            ? null
            : new WhoopRecovery(
                (int)Math.Round(score.RecoveryScore),
                score.HrvRmssdMilli,
                (int)Math.Round(score.RestingHeartRate));
    }

    private async Task<WhoopSleepSummary?> GetLatestSleepAsync(string token, CancellationToken ct)
    {
        var resp = await GetAsync<WhoopCollection<WhoopSleepRecord>>(
            "developer/v2/activity/sleep?limit=5", token, ct);

        var sleep = resp.Records.FirstOrDefault(r => !r.Nap && r.ScoreState == "SCORED");
        if (sleep?.Score is not { StageSummary: { } stages } score)
        {
            return null;
        }

        var asleep = TimeSpan.FromMilliseconds(stages.LightMilli + stages.SlowWaveMilli + stages.RemMilli);
        return new WhoopSleepSummary((int)Math.Round(score.SleepPerformancePercentage), asleep);
    }

    private async Task<double?> GetLatestStrainAsync(string token, CancellationToken ct)
    {
        var resp = await GetAsync<WhoopCollection<WhoopCycleRecord>>(
            "developer/v2/cycle?limit=1", token, ct);

        return resp.Records.FirstOrDefault(r => r.ScoreState == "SCORED")?.Score?.Strain;
    }

    private async Task<T> GetAsync<T>(string url, string token, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"WHOOP {(int)response.StatusCode} bei '{url}': {body}");
        }

        return await response.Content.ReadFromJsonAsync<T>(ct)
            ?? throw new InvalidOperationException($"Leere Antwort von '{url}'.");
    }
}
