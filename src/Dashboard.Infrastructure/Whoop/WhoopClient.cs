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

        var records = await GetPagedAsync<WhoopWorkoutRecord>(
            $"developer/v2/activity/workout?start={Iso(fromUtc)}&end={Iso(toUtc)}&limit=25", token, ct);

        return records
            .Where(r => !string.IsNullOrEmpty(r.Id))
            .Select(r => new WhoopWorkout(
                r.Id,
                r.SportName ?? string.Empty,
                r.Start,
                r.End,
                r.Score?.DistanceMeter,
                r.Score?.ZoneDurations?.HighIntensityShare ?? 0,
                Strain: r.Score?.Strain,
                Kilojoule: r.Score?.Kilojoule,
                AverageHeartRate: RoundToInt(r.Score?.AverageHeartRate),
                MaxHeartRate: RoundToInt(r.Score?.MaxHeartRate),
                Zones: r.Score?.ZoneDurations is { } zones
                    ? new WhoopZoneTimes(
                        zones.ZoneZero, zones.ZoneOne, zones.ZoneTwo,
                        zones.ZoneThree, zones.ZoneFour, zones.ZoneFive)
                    : null))
            .ToList();
    }

    private static int? RoundToInt(double? value) =>
        value is { } v ? (int)Math.Round(v) : null;

    public async Task<IReadOnlyList<WhoopDailyMetric>> GetHistoryAsync(
        DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct = default)
    {
        var token = await _tokens.GetValidAccessTokenAsync(ct)
            ?? throw new InvalidOperationException("WHOOP nicht verbunden (kein Token).");

        var window = $"start={Iso(fromUtc)}&end={Iso(toUtc)}&limit=25";
        var recoveries = await GetPagedAsync<WhoopRecoveryRecord>($"developer/v2/recovery?{window}", token, ct);
        var sleeps = await GetPagedAsync<WhoopSleepRecord>($"developer/v2/activity/sleep?{window}", token, ct);
        var cycles = await GetPagedAsync<WhoopCycleRecord>($"developer/v2/cycle?{window}", token, ct);

        var byDate = new Dictionary<DateOnly, MetricBuilder>();

        foreach (var r in recoveries)
        {
            if (r is { ScoreState: "SCORED", Score: { } score, CreatedAt: { } created })
            {
                var builder = GetOrAdd(byDate, BerlinDate(created));
                builder.RecoveryScore = (int)Math.Round(score.RecoveryScore);
                builder.Hrv = score.HrvRmssdMilli;
                builder.Rhr = (int)Math.Round(score.RestingHeartRate);
                builder.Spo2 = score.Spo2Percentage;
                builder.SkinTemp = score.SkinTempCelsius;
            }
        }

        foreach (var s in sleeps)
        {
            if (!s.Nap && s is { ScoreState: "SCORED", Score: { StageSummary: { } stages } score })
            {
                var builder = GetOrAdd(byDate, BerlinDate(s.End));
                builder.SleepHours = (stages.LightMilli + stages.SlowWaveMilli + stages.RemMilli) / 3_600_000.0;
                builder.SleepPerformance = (int)Math.Round(score.SleepPerformancePercentage);
                builder.LightHours = stages.LightMilli / 3_600_000.0;
                builder.DeepHours = stages.SlowWaveMilli / 3_600_000.0;
                builder.RemHours = stages.RemMilli / 3_600_000.0;
                builder.AwakeHours = stages.AwakeMilli is { } awake ? awake / 3_600_000.0 : null;
                builder.SleepStart = s.Start;
                builder.SleepEnd = s.End;
                builder.RespiratoryRate = score.RespiratoryRate;
            }
        }

        foreach (var c in cycles)
        {
            if (c is { ScoreState: "SCORED", Score: { } score })
            {
                GetOrAdd(byDate, BerlinDate(c.Start)).DayStrain = score.Strain;
            }
        }

        return byDate.Values
            .Select(b => b.Build())
            .OrderBy(m => m.Date)
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

    // Folgt next_token über mehrere Seiten (mit Sicherheits-Obergrenze).
    private async Task<List<T>> GetPagedAsync<T>(string url, string token, CancellationToken ct)
    {
        const int maxPages = 8;
        var all = new List<T>();
        string? next = null;

        for (var page = 0; page < maxPages; page++)
        {
            var pageUrl = next is null ? url : $"{url}&nextToken={Uri.EscapeDataString(next)}";
            var response = await GetAsync<WhoopCollection<T>>(pageUrl, token, ct);
            all.AddRange(response.Records);
            next = response.NextToken;
            if (string.IsNullOrEmpty(next))
            {
                break;
            }
        }

        return all;
    }

    private static readonly TimeZoneInfo BerlinTz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    private static DateOnly BerlinDate(DateTimeOffset value) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(value, BerlinTz).DateTime);

    private static MetricBuilder GetOrAdd(Dictionary<DateOnly, MetricBuilder> map, DateOnly date)
    {
        if (!map.TryGetValue(date, out var builder))
        {
            builder = new MetricBuilder(date);
            map[date] = builder;
        }
        return builder;
    }

    private sealed class MetricBuilder(DateOnly date)
    {
        public int? RecoveryScore;
        public double? Hrv;
        public int? Rhr;
        public double? SleepHours;
        public int? SleepPerformance;
        public double? DayStrain;
        public double? LightHours;
        public double? DeepHours;
        public double? RemHours;
        public double? AwakeHours;
        public DateTimeOffset? SleepStart;
        public DateTimeOffset? SleepEnd;
        public double? RespiratoryRate;
        public double? Spo2;
        public double? SkinTemp;

        public WhoopDailyMetric Build() => new(
            date, RecoveryScore, Hrv, Rhr, SleepHours, SleepPerformance, DayStrain,
            LightSleepHours: LightHours,
            DeepSleepHours: DeepHours,
            RemSleepHours: RemHours,
            AwakeHours: AwakeHours,
            SleepStartUtc: SleepStart,
            SleepEndUtc: SleepEnd,
            RespiratoryRate: RespiratoryRate,
            Spo2Percentage: Spo2,
            SkinTempCelsius: SkinTemp);
    }
}
