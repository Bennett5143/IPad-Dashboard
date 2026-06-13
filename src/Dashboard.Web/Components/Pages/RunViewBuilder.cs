using System.Globalization;

namespace Dashboard.Web.Components.Pages;

/// <summary>Eine Zeile der Lauf-Liste (`/runs`).</summary>
public sealed record RunListRow(
    long Id, string Date, string Name, string Distance, string Pace, string HeartRate, string Elevation);

/// <summary>Kopfdaten der Lauf-Detailseite (`/runs/{id}`).</summary>
public sealed record RunDetailHeader(
    string Name, string Date, string Distance, string Duration, string Pace, string HeartRate, string Elevation);

/// <summary>
/// Formatiert Läufe für Liste und Detailseite — reine, testbare Aufbereitung (Muster
/// <see cref="WhoopInsightsBuilder"/>), getrennt vom Profil-Rechnen (<c>RunProfileBuilder</c>).
/// </summary>
public static class RunViewBuilder
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    private static readonly TimeZoneInfo BerlinTz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    public static IReadOnlyList<RunListRow> BuildList(IReadOnlyList<Run> runs) =>
        runs.Select(run => new RunListRow(
            run.Id,
            LocalDate(run).ToString("dd.MM.yyyy", German),
            RunName(run),
            Distance(run),
            Pace(run),
            HeartRate(run),
            Elevation(run))).ToList();

    public static RunDetailHeader BuildDetailHeader(Run run) => new(
        RunName(run),
        LocalDate(run).ToString("dddd, dd.MM.yyyy · HH:mm", German),
        Distance(run),
        Duration(run.MovingTime),
        Pace(run),
        HeartRate(run),
        Elevation(run));

    private static DateTimeOffset LocalDate(Run run) => TimeZoneInfo.ConvertTime(run.StartUtc, BerlinTz);

    private static string RunName(Run run) => string.IsNullOrWhiteSpace(run.Name) ? "Lauf" : run.Name;

    private static string Distance(Run run) => $"{(run.DistanceMeters / 1000.0).ToString("0.0", German)} km";

    private static string HeartRate(Run run) => run.AverageHeartRate is { } hr ? $"Ø {hr} bpm" : "–";

    private static string Elevation(Run run) =>
        run.ElevationGainMeters is { } gain ? $"{gain.ToString("0", German)} m" : "–";

    private static string Pace(Run run)
    {
        var km = run.DistanceMeters / 1000.0;
        if (km <= 0 || run.MovingTime <= TimeSpan.Zero)
        {
            return "–";
        }

        var pace = run.MovingTime.TotalMinutes / km;
        var minutes = (int)pace;
        var seconds = (int)Math.Round((pace - minutes) * 60, MidpointRounding.AwayFromZero);
        if (seconds == 60)
        {
            minutes++;
            seconds = 0;
        }

        return $"{minutes}:{seconds:00} /km";
    }

    private static string Duration(TimeSpan time) => time >= TimeSpan.FromHours(1)
        ? $"{(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00} h"
        : $"{time.Minutes}:{time.Seconds:00} min";
}
