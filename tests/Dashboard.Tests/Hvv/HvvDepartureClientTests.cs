namespace Dashboard.Tests.Hvv;

public class HvvDepartureClientTests
{
    // 10. Juni 2026, 12:00 UTC = 14:00 Berlin (CEST)
    private static readonly DateTimeOffset NowUtc = new(2026, 6, 10, 12, 0, 0, TimeSpan.Zero);

    private const string OkResponse =
        """
        {
          "returnCode": "OK",
          "time": { "date": "10.06.2026", "time": "14:00" },
          "departures": [
            { "line": { "name": "189", "direction": "S Blankenese", "type": { "simpleType": "BUS", "shortInfo": "Bus" } },
              "timeOffset": 6, "delay": 120 },
            { "line": { "name": "S1", "direction": "Wedel", "type": { "simpleType": "STRAIN", "shortInfo": "S-Bahn" } },
              "timeOffset": 3, "delay": null },
            { "line": { "name": "U1", "direction": "Norderstedt", "type": { "simpleType": "UTRAIN", "shortInfo": "U-Bahn" } },
              "timeOffset": 10, "delay": 0 }
          ]
        }
        """;

    private static HvvOptions Options(int maxDepartures = 6, HvvLineFilter[]? lines = null) => new()
    {
        Endpoint = "https://test.local/departureList",
        Version = 47,
        MaxDepartures = maxDepartures,
        Stations =
        [
            new HvvStationConfig
            {
                Name = "Wedel, Feldstraße",
                MasterId = "Master:85001",
                City = "Wedel",
                Lines = lines ?? []
            }
        ]
    };

    private static HvvDepartureClient CreateClient(
        HvvOptions options, string responseJson, Action<string>? captureBody = null)
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            captureBody?.Invoke(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult());
            return StubHttpMessageHandler.Json(responseJson);
        });
        var http = new HttpClient(handler);

        return new HvvDepartureClient(http, new FakeClock { UtcNow = NowUtc }, Microsoft.Extensions.Options.Options.Create(options));
    }

    [Fact]
    public async Task GetDeparturesAsync_SendsServerTimeAndStationConfigInBody()
    {
        string? body = null;
        var client = CreateClient(Options(), OkResponse, b => body = b);

        await client.GetDeparturesAsync();

        Assert.NotNull(body);
        Assert.Contains("\"version\":47", body, StringComparison.Ordinal);
        Assert.Contains("Master:85001", body, StringComparison.Ordinal);
        Assert.Contains("\"filter\":[]", body, StringComparison.Ordinal); // kein serverseitiger Filter
        Assert.Contains("\"date\":\"10.06.2026\"", body, StringComparison.Ordinal);
        Assert.Contains("\"time\":\"14:00\"", body, StringComparison.Ordinal);
        Assert.Contains("\"useRealtime\":true", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetDeparturesAsync_FiltersByLineAndDirection()
    {
        var options = Options(lines: [new HvvLineFilter { Line = "189", Direction = "Blankenese" }]);

        var board = (await CreateClient(options, OkResponse).GetDeparturesAsync()).Stations[0];

        // Nur die 189-Abfahrt Richtung "S Blankenese" bleibt – S1/U1 fallen raus.
        Assert.Single(board.Departures);
        Assert.Equal("189", board.Departures[0].LineName);
        Assert.Contains("Blankenese", board.Departures[0].Direction, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetDeparturesAsync_OrdersByTimeOffset_AndComputesTimesRelativeToServerTime()
    {
        var board = (await CreateClient(Options(), OkResponse).GetDeparturesAsync()).Stations[0];

        Assert.True(board.Available);
        Assert.Equal("Wedel, Feldstraße", board.StationName);
        Assert.Equal(3, board.Departures.Count);

        // sortiert nach timeOffset: S1 (3), 189 (6), U1 (10)
        Assert.Equal("S1", board.Departures[0].LineName);
        Assert.Equal("189", board.Departures[1].LineName);
        Assert.Equal("U1", board.Departures[2].LineName);

        // S1: 14:00 + 3 min = 14:03 (= 12:03 UTC)
        Assert.Equal(
            new DateTimeOffset(2026, 6, 10, 12, 3, 0, TimeSpan.Zero),
            board.Departures[0].PlannedTime.ToUniversalTime());
    }

    [Fact]
    public async Task GetDeparturesAsync_PreservesDelaySemantics()
    {
        var board = (await CreateClient(Options(), OkResponse).GetDeparturesAsync()).Stations[0];

        var s1 = board.Departures[0];   // delay: null → keine Echtzeit
        Assert.False(s1.HasLiveData);
        Assert.Equal(TransportMode.SBahn, s1.Mode);

        var bus = board.Departures[1];  // delay: 120 s → +2 min
        Assert.True(bus.HasLiveData);
        Assert.Equal(TimeSpan.FromMinutes(2), bus.Delay);
        Assert.Equal(
            new DateTimeOffset(2026, 6, 10, 12, 8, 0, TimeSpan.Zero),
            bus.ExpectedTime.ToUniversalTime());
        Assert.Equal(TransportMode.Bus, bus.Mode);

        var ubahn = board.Departures[2]; // delay: 0 → Echtzeit + pünktlich
        Assert.True(ubahn.HasLiveData);
        Assert.Equal(TimeSpan.Zero, ubahn.Delay);
        Assert.Equal(TransportMode.UBahn, ubahn.Mode);
    }

    [Fact]
    public async Task GetDeparturesAsync_StampsRetrievalTime()
    {
        var snapshot = await CreateClient(Options(), OkResponse).GetDeparturesAsync();

        Assert.Equal(NowUtc, snapshot.RetrievedAtUtc);
    }

    [Fact]
    public async Task GetDeparturesAsync_RespectsMaxDepartures()
    {
        var board = (await CreateClient(Options(maxDepartures: 2), OkResponse).GetDeparturesAsync()).Stations[0];

        Assert.Equal(2, board.Departures.Count);
    }

    [Fact]
    public async Task GetDeparturesAsync_MarksStationUnavailable_OnNonOkReturnCode()
    {
        const string error = """{ "returnCode": "ERROR_TEXT" }""";

        var board = (await CreateClient(Options(), error).GetDeparturesAsync()).Stations[0];

        Assert.False(board.Available);
        Assert.Empty(board.Departures);
        Assert.Equal("Wedel, Feldstraße", board.StationName);
    }
}
