using System.Text.Json.Serialization;

namespace Dashboard.Infrastructure.Strava;

// Interne DTOs zum Strava-Wire-Format (snake_case → JsonPropertyName nötig).

internal sealed record StravaTokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("refresh_token")] string RefreshToken,
    [property: JsonPropertyName("expires_at")] long ExpiresAt);

internal sealed record StravaActivityDto(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("sport_type")] string? SportType,
    [property: JsonPropertyName("distance")] double Distance,
    [property: JsonPropertyName("moving_time")] int MovingTime,
    [property: JsonPropertyName("start_date")] DateTimeOffset StartDate,
    [property: JsonPropertyName("map")] StravaMapDto? Map);

internal sealed record StravaMapDto(
    [property: JsonPropertyName("summary_polyline")] string? SummaryPolyline);

// Activity-Streams (key_by_type=true): je Typ ein Objekt mit "data"-Array.
internal sealed record StravaStreamSet(
    [property: JsonPropertyName("latlng")] StravaStreamData<double[]>? LatLng,
    [property: JsonPropertyName("time")] StravaStreamData<int>? Time,
    [property: JsonPropertyName("altitude")] StravaStreamData<double>? Altitude,
    [property: JsonPropertyName("heartrate")] StravaStreamData<int>? HeartRate);

internal sealed record StravaStreamData<T>(
    [property: JsonPropertyName("data")] IReadOnlyList<T>? Data);
