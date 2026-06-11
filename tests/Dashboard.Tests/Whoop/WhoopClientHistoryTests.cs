using System.Net;

using Dashboard.Domain.Whoop;
using Dashboard.Infrastructure.Whoop;

namespace Dashboard.Tests.Whoop;

public class WhoopClientHistoryTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 6, 11, 12, 0, 0, TimeSpan.Zero);

    private const string RecoveryJson =
        """
        { "records": [
          { "created_at": "2026-06-10T05:00:00.000Z", "score_state": "SCORED",
            "score": { "recovery_score": 70, "resting_heart_rate": 50, "hrv_rmssd_milli": 60 } },
          { "created_at": "2026-06-11T05:00:00.000Z", "score_state": "SCORED",
            "score": { "recovery_score": 40, "resting_heart_rate": 55, "hrv_rmssd_milli": 45 } }
        ] }
        """;

    private const string SleepJson =
        """
        { "records": [
          { "nap": false, "start": "2026-06-10T22:00:00.000Z", "end": "2026-06-11T06:00:00.000Z", "score_state": "SCORED",
            "score": { "sleep_performance_percentage": 90,
              "stage_summary": { "total_light_sleep_time_milli": 14400000, "total_slow_wave_sleep_time_milli": 5400000, "total_rem_sleep_time_milli": 7200000 } } }
        ] }
        """;

    private const string CycleJson =
        """
        { "records": [
          { "start": "2026-06-11T05:30:00.000Z", "score_state": "SCORED", "score": { "strain": 11.2 } }
        ] }
        """;

    private sealed class FixedTokenProvider : IWhoopAccessTokenProvider
    {
        public Task<string?> GetValidAccessTokenAsync(CancellationToken ct = default) =>
            Task.FromResult<string?>("access-token");
    }

    private static WhoopClient Create()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path.Contains("recovery", StringComparison.Ordinal)) return StubHttpMessageHandler.Json(RecoveryJson);
            if (path.Contains("sleep", StringComparison.Ordinal)) return StubHttpMessageHandler.Json(SleepJson);
            if (path.Contains("cycle", StringComparison.Ordinal)) return StubHttpMessageHandler.Json(CycleJson);
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.prod.whoop.com/") };

        return new WhoopClient(http, new FixedTokenProvider(), new FakeClock { UtcNow = NowUtc });
    }

    [Fact]
    public async Task GetHistoryAsync_MergesByBerlinDate_SortedAscending()
    {
        var days = await Create().GetHistoryAsync(NowUtc.AddDays(-7), NowUtc);

        Assert.Equal(2, days.Count);
        Assert.Equal(new DateOnly(2026, 6, 10), days[0].Date);
        Assert.Equal(new DateOnly(2026, 6, 11), days[1].Date);
    }

    [Fact]
    public async Task GetHistoryAsync_MapsRecoverySleepAndStrainOntoTheRightDay()
    {
        var days = await Create().GetHistoryAsync(NowUtc.AddDays(-7), NowUtc);
        var day10 = days[0];
        var day11 = days[1];

        // 10.06.: nur Recovery vorhanden
        Assert.Equal(70, day10.RecoveryScore);
        Assert.Equal(60d, day10.HrvMillis);
        Assert.Equal(50, day10.RestingHeartRate);
        Assert.Null(day10.SleepHours);

        // 11.06.: Recovery + Schlaf (end-Tag) + Strain (cycle-start)
        Assert.Equal(40, day11.RecoveryScore);
        Assert.Equal(WhoopRecoveryLevel.Medium, day11.RecoveryLevel);
        Assert.Equal(7.5, day11.SleepHours); // 4 + 1,5 + 2 h
        Assert.Equal(90, day11.SleepPerformance);
        Assert.Equal(11.2, day11.DayStrain);
    }
}
