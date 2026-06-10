using System.Text.Json.Serialization;

namespace Dashboard.Infrastructure.Hvv;

// Request- und Response-DTOs zum inoffiziellen geofox/departureList-Endpoint.
// JsonPropertyName ist hier wichtig: das Wire-Format nutzt teils Sonderschreibweisen
// ("serviceID", "stationIDs"), die nicht der Standard-camelCase-Konvention entsprechen.

internal sealed record HvvRequest(
    [property: JsonPropertyName("version")] int Version,
    [property: JsonPropertyName("stations")] IReadOnlyList<HvvReqStation> Stations,
    [property: JsonPropertyName("filter")] IReadOnlyList<HvvReqFilter> Filter,
    [property: JsonPropertyName("time")] HvvTimeDto Time,
    [property: JsonPropertyName("maxList")] int MaxList,
    [property: JsonPropertyName("maxTimeOffset")] int MaxTimeOffset,
    [property: JsonPropertyName("useRealtime")] bool UseRealtime,
    [property: JsonPropertyName("allStationsInChangingNode")] bool AllStationsInChangingNode);

internal sealed record HvvReqStation(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("city")] string City,
    [property: JsonPropertyName("type")] string Type);

internal sealed record HvvReqFilter(
    [property: JsonPropertyName("serviceID")] string ServiceId,
    [property: JsonPropertyName("stationIDs")] IReadOnlyList<string> StationIds);

internal sealed record HvvTimeDto(
    [property: JsonPropertyName("date")] string Date,
    [property: JsonPropertyName("time")] string Time);

internal sealed record HvvResponse(
    [property: JsonPropertyName("returnCode")] string? ReturnCode,
    [property: JsonPropertyName("time")] HvvTimeDto? Time,
    [property: JsonPropertyName("departures")] IReadOnlyList<HvvDepartureDto>? Departures);

internal sealed record HvvDepartureDto(
    [property: JsonPropertyName("line")] HvvLineDto Line,
    [property: JsonPropertyName("timeOffset")] int TimeOffset,
    [property: JsonPropertyName("delay")] int? Delay);

internal sealed record HvvLineDto(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("direction")] string Direction,
    [property: JsonPropertyName("type")] HvvLineTypeDto Type);

internal sealed record HvvLineTypeDto(
    [property: JsonPropertyName("simpleType")] string? SimpleType,
    [property: JsonPropertyName("shortInfo")] string? ShortInfo);
