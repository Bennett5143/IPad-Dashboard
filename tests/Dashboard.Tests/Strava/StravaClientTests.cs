using System.Net;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Dashboard.Tests.Strava;

public class StravaClientTests
{
    private const string RunJson =
        """{"id":111,"name":"Morgenlauf","type":"Run","sport_type":"Run","distance":5000.0,"moving_time":1500,"start_date":"2026-06-01T06:30:00Z","total_elevation_gain":42.5,"average_heartrate":151.6,"max_heartrate":178.0,"map":{"summary_polyline":"_p~iF~ps|U_ulLnnqC_mqNvxq`@"}}""";

    private const string RideJson =
        """{"id":222,"name":"Radtour","type":"Ride","sport_type":"Ride","distance":20000,"moving_time":3600,"start_date":"2026-06-02T06:30:00Z","map":{"summary_polyline":""}}""";

    private const string TrailJson =
        """{"id":333,"name":"Trail","type":"TrailRun","sport_type":"TrailRun","distance":8000,"moving_time":2700,"start_date":"2026-05-30T06:30:00Z","map":{"summary_polyline":"_p~iF~ps|U"}}""";

    private const string StreamsJson =
        """{"latlng":{"data":[[53.55,9.99],[53.551,9.991],[53.552,9.992]]},"time":{"data":[0,10,20]},"altitude":{"data":[5.0,7.5,6.0]},"heartrate":{"data":[120,135,140]}}""";

    private static StravaClient Client(HttpMessageHandler handler, string? token = "test-token", int perPage = 2)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://www.strava.com/") };
        return new StravaClient(
            http, new StubAccessTokenProvider(token),
            Options.Create(new StravaOptions { PerPage = perPage }),
            NullLogger<StravaClient>.Instance);
    }

    private static string Page(HttpRequestMessage request)
    {
        foreach (var part in request.RequestUri!.Query.TrimStart('?').Split('&'))
        {
            if (part.StartsWith("page=", StringComparison.Ordinal))
            {
                return part["page=".Length..];
            }
        }

        return string.Empty;
    }

    [Fact]
    public async Task GetActivitiesAsync_Paginates_FiltersNonRuns_AndDecodesPolyline()
    {
        var handler = new StubHttpMessageHandler(request => Page(request) == "1"
            ? StubHttpMessageHandler.Json($"[{RunJson},{RideJson}]") // 2 == perPage → weitere Seite
            : StubHttpMessageHandler.Json($"[{TrailJson}]"));        // 1 < perPage → Ende

        var runs = await Client(handler).GetActivitiesAsync(null);

        Assert.Equal(2, runs.Count); // Ride herausgefiltert
        Assert.Equal(111, runs[0].Id);
        Assert.Equal("Morgenlauf", runs[0].Name);
        Assert.Equal("Run", runs[0].Type);
        Assert.Equal(5000, runs[0].DistanceMeters);
        Assert.Equal(TimeSpan.FromSeconds(1500), runs[0].MovingTime);
        Assert.Equal(3, runs[0].Track.Count); // Polyline dekodiert
        Assert.Equal(333, runs[1].Id);
        Assert.Equal("TrailRun", runs[1].Type);
    }

    [Fact]
    public async Task GetActivitiesAsync_MapsElevationAndHeartRates()
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json($"[{RunJson}]"));

        var run = (await Client(handler).GetActivitiesAsync(null)).Single(r => r.Id == 111);

        Assert.Equal(42.5, run.ElevationGainMeters);
        Assert.Equal(152, run.AverageHeartRate); // 151.6 gerundet
        Assert.Equal(178, run.MaxHeartRate);

        // Trail-Fixture ohne die Felder → bleiben leer.
        var trailHandler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json($"[{TrailJson}]"));
        var trail = (await Client(trailHandler).GetActivitiesAsync(null)).Single();
        Assert.Null(trail.ElevationGainMeters);
        Assert.Null(trail.AverageHeartRate);
    }

    [Fact]
    public async Task GetActivitiesAsync_PausesNotAborts_OnRateLimit()
    {
        var handler = new StubHttpMessageHandler(request => Page(request) == "1"
            ? StubHttpMessageHandler.Json($"[{RunJson},{TrailJson}]")        // 2 == perPage → weiter
            : new HttpResponseMessage(HttpStatusCode.TooManyRequests));      // 429 → pausieren

        var runs = await Client(handler).GetActivitiesAsync(null);

        Assert.Equal(2, runs.Count); // Seite 1 erhalten, kein Throw
    }

    [Fact]
    public async Task GetActivitiesAsync_Throws_WhenNotConnected()
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json("[]"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Client(handler, token: null).GetActivitiesAsync(null));
    }

    [Fact]
    public async Task GetStreamsAsync_MapsLatLngTimeAltitudeAndHeartRate()
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json(StreamsJson));

        var streams = await Client(handler).GetStreamsAsync(111);

        Assert.NotNull(streams);
        Assert.Equal(3, streams!.Track.Count);
        Assert.Equal(53.55, streams.Track[0].Latitude);
        Assert.Equal(9.99, streams.Track[0].Longitude);
        Assert.Equal(new[] { 0, 10, 20 }, streams.TimeOffsetsSeconds);
        Assert.Equal(new[] { 5.0, 7.5, 6.0 }, streams.AltitudesMeters);
        Assert.Equal(new[] { 120, 135, 140 }, streams.HeartRates);
    }

    [Fact]
    public async Task GetStreamsAsync_Throws_OnRateLimit()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests));

        await Assert.ThrowsAsync<StravaRateLimitException>(() => Client(handler).GetStreamsAsync(111));
    }

    [Fact]
    public async Task GetStreamsAsync_ReturnsNull_WhenNoLatLngStream()
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json("""{"time":{"data":[0,10]}}"""));

        Assert.Null(await Client(handler).GetStreamsAsync(111));
    }
}
