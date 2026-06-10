using System.Net;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Dashboard.Tests.Strava;

public class StravaClientTests
{
    private const string RunJson =
        """{"id":111,"name":"Morgenlauf","type":"Run","sport_type":"Run","distance":5000.0,"moving_time":1500,"start_date":"2026-06-01T06:30:00Z","map":{"summary_polyline":"_p~iF~ps|U_ulLnnqC_mqNvxq`@"}}""";

    private const string RideJson =
        """{"id":222,"name":"Radtour","type":"Ride","sport_type":"Ride","distance":20000,"moving_time":3600,"start_date":"2026-06-02T06:30:00Z","map":{"summary_polyline":""}}""";

    private const string TrailJson =
        """{"id":333,"name":"Trail","type":"TrailRun","sport_type":"TrailRun","distance":8000,"moving_time":2700,"start_date":"2026-05-30T06:30:00Z","map":{"summary_polyline":"_p~iF~ps|U"}}""";

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
}
