using System.Text.Json.Serialization;

namespace Dashboard.Infrastructure.Football;

// Interne DTOs zum Wire-Format von football-data.org v4. Nur die genutzten Felder.

internal sealed record FdMatchesResponse(
    [property: JsonPropertyName("matches")] IReadOnlyList<FdMatch> Matches);

internal sealed record FdMatch(
    [property: JsonPropertyName("utcDate")] DateTimeOffset UtcDate,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("competition")] FdCompetition Competition,
    [property: JsonPropertyName("homeTeam")] FdTeam HomeTeam,
    [property: JsonPropertyName("awayTeam")] FdTeam AwayTeam,
    [property: JsonPropertyName("score")] FdScore Score);

internal sealed record FdCompetition(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("code")] string? Code);

internal sealed record FdTeam(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("shortName")] string? ShortName,
    [property: JsonPropertyName("tla")] string? Tla);

internal sealed record FdScore(
    [property: JsonPropertyName("fullTime")] FdScoreTime FullTime);

internal sealed record FdScoreTime(
    [property: JsonPropertyName("home")] int? Home,
    [property: JsonPropertyName("away")] int? Away);

internal sealed record FdStandingsResponse(
    [property: JsonPropertyName("standings")] IReadOnlyList<FdStanding> Standings);

internal sealed record FdStanding(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("table")] IReadOnlyList<FdTableEntry> Table);

internal sealed record FdTableEntry(
    [property: JsonPropertyName("position")] int Position,
    [property: JsonPropertyName("team")] FdTeam Team,
    [property: JsonPropertyName("playedGames")] int PlayedGames,
    [property: JsonPropertyName("won")] int Won,
    [property: JsonPropertyName("draw")] int Draw,
    [property: JsonPropertyName("lost")] int Lost,
    [property: JsonPropertyName("goalDifference")] int GoalDifference,
    [property: JsonPropertyName("points")] int Points);
