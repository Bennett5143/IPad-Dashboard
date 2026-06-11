using System.Net;

using Dashboard.Domain.Whoop;
using Dashboard.Infrastructure.Whoop;

namespace Dashboard.Tests.Whoop;

public class WhoopClientTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 6, 11, 12, 0, 0, TimeSpan.Zero);

    private const string RecoveryJson =
        """
        { "records": [ { "score_state": "SCORED",
          "score": { "recovery_score": 72, "resting_heart_rate": 48, "hrv_rmssd_milli": 65.4 } } ] }
        """;

    // Erster Datensatz ist ein Nap (muss übersprungen werden), zweiter der Hauptschlaf.
    private const string SleepJson =
        """
        { "records": [
          { "nap": true, "score_state": "SCORED",
            "score": { "sleep_performance_percentage": 40,
              "stage_summary": { "total_light_sleep_time_milli": 1, "total_slow_wave_sleep_time_milli": 1, "total_rem_sleep_time_milli": 1 } } },
          { "nap": false, "score_state": "SCORED",
            "score": { "sleep_performance_percentage": 88,
              "stage_summary": { "total_light_sleep_time_milli": 14400000, "total_slow_wave_sleep_time_milli": 5400000, "total_rem_sleep_time_milli": 7200000 } } }
        ] }
        """;

    private const string CycleJson =
        """
        { "records": [ { "score_state": "SCORED", "score": { "strain": 12.7 } } ] }
        """;

    private sealed class FixedTokenProvider : IWhoopAccessTokenProvider
    {
        public string? Token = "access-token";
        public Task<string?> GetValidAccessTokenAsync(CancellationToken ct = default) => Task.FromResult(Token);
    }

    private static WhoopClient Create(IWhoopAccessTokenProvider? provider = null)
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

        return new WhoopClient(http, provider ?? new FixedTokenProvider(), new FakeClock { UtcNow = NowUtc });
    }

    [Fact]
    public async Task GetWhoopAsync_MapsLatestRecovery()
    {
        var snapshot = await Create().GetWhoopAsync();

        Assert.NotNull(snapshot.Recovery);
        Assert.Equal(72, snapshot.Recovery!.ScorePercent);
        Assert.Equal(48, snapshot.Recovery.RestingHeartRate);
        Assert.Equal(65.4, snapshot.Recovery.HrvMillis, 1);
        Assert.Equal(WhoopRecoveryLevel.High, snapshot.Recovery.Level);
    }

    [Fact]
    public async Task GetWhoopAsync_MapsMainSleep_SkippingNaps()
    {
        var snapshot = await Create().GetWhoopAsync();

        Assert.NotNull(snapshot.Sleep);
        Assert.Equal(88, snapshot.Sleep!.PerformancePercent);
        Assert.Equal(TimeSpan.FromHours(7.5), snapshot.Sleep.Asleep); // 4 + 1,5 + 2 h
    }

    [Fact]
    public async Task GetWhoopAsync_MapsDayStrain_AndStampsRetrievalTime()
    {
        var snapshot = await Create().GetWhoopAsync();

        Assert.Equal(12.7, snapshot.DayStrain);
        Assert.Equal(NowUtc, snapshot.RetrievedAtUtc);
    }

    [Fact]
    public async Task GetWhoopAsync_Throws_WhenNotConnected()
    {
        var client = Create(new FixedTokenProvider { Token = null });

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetWhoopAsync());
    }
}
