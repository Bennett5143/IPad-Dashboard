using System.Globalization;
using System.Net.Http.Json;

using Dashboard.Domain.Hvv;
using Dashboard.Domain.Time;

using Microsoft.Extensions.Options;

namespace Dashboard.Infrastructure.Hvv;

/// <summary>
/// <see cref="IHvvProvider"/> auf Basis des inoffiziellen <c>geofox/departureList</c>-Endpoints
/// (siehe HVV-Recherche). Pro Haltestelle ein POST. <c>timeOffset</c> der Abfahrten wird gegen die
/// von der API gelieferte Server-Zeit gerechnet, <c>delay</c> (Sekunden, nullable) bleibt als
/// Echtzeit-Marker erhalten.
/// </summary>
public sealed class HvvDepartureClient : IHvvProvider
{
    private static readonly TimeZoneInfo BerlinTz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    private readonly HttpClient _http;
    private readonly IClock _clock;
    private readonly HvvOptions _options;

    public HvvDepartureClient(HttpClient http, IClock clock, IOptions<HvvOptions> options)
    {
        _http = http;
        _clock = clock;
        _options = options.Value;
    }

    public async Task<HvvSnapshot> GetDeparturesAsync(CancellationToken ct = default)
    {
        var nowBerlin = TimeZoneInfo.ConvertTime(_clock.UtcNow, BerlinTz);
        var requestTime = new HvvTimeDto(
            nowBerlin.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
            nowBerlin.ToString("HH:mm", CultureInfo.InvariantCulture));

        var boards = new List<StationBoard>(_options.Stations.Count);
        foreach (var station in _options.Stations)
        {
            boards.Add(await GetStationAsync(station, requestTime, ct));
        }

        return new HvvSnapshot(boards, _clock.UtcNow);
    }

    private async Task<StationBoard> GetStationAsync(
        HvvStationConfig station, HvvTimeDto requestTime, CancellationToken ct)
    {
        var request = new HvvRequest(
            _options.Version,
            [new HvvReqStation(station.Name, station.MasterId, station.City, "STATION")],
            station.Filters.Select(f => new HvvReqFilter(f.ServiceId, [f.TargetStationId])).ToList(),
            requestTime,
            station.MaxList,
            station.MaxTimeOffsetMinutes,
            UseRealtime: true,
            AllStationsInChangingNode: true);

        using var response = await _http.PostAsJsonAsync(_options.Endpoint, request, ct);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<HvvResponse>(ct)
            ?? throw new InvalidOperationException("Leere Antwort vom HVV-Endpoint.");

        // Fehlerhafter returnCode betrifft nur diese Station → freundlich "nicht verfügbar",
        // ohne die anderen Haltestellen mitzureißen.
        if (!string.Equals(dto.ReturnCode, "OK", StringComparison.Ordinal)
            || dto.Time is null || dto.Departures is null)
        {
            return new StationBoard(station.Name, Available: false, []);
        }

        var serverTime = HvvServerTimeParser.Parse(dto.Time.Date, dto.Time.Time, BerlinTz);

        var departures = dto.Departures
            .OrderBy(d => d.TimeOffset)
            .Take(_options.MaxDepartures)
            .Select(d => MapDeparture(d, serverTime))
            .ToList();

        return new StationBoard(station.Name, Available: true, departures);
    }

    private static Departure MapDeparture(HvvDepartureDto dto, DateTimeOffset serverTime)
    {
        var planned = serverTime.AddMinutes(dto.TimeOffset);
        var delay = dto.Delay.HasValue ? TimeSpan.FromSeconds(dto.Delay.Value) : (TimeSpan?)null;

        return new Departure(
            dto.Line.Name,
            dto.Line.Direction,
            HvvModeMapper.Map(dto.Line.Type.SimpleType),
            dto.Line.Type.ShortInfo ?? string.Empty,
            planned,
            delay);
    }
}
