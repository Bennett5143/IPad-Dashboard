using System.Globalization;

namespace Dashboard.Web.Components.Pages;

/// <summary>Eine Zeile der Orts-Übersicht. <see cref="Id"/> verlinkt auf die Heatmap des Ortes.</summary>
public sealed record RunPlaceRow(
    int Id, string Name, string Runs, string Distance, string Pace, string LastRun);

/// <summary>
/// Formatiert die Orts-Übersicht auf <c>/runs</c> — reine, testbare Aufbereitung (Muster
/// <see cref="WhoopInsightsBuilder"/>).
/// </summary>
public static class RunViewBuilder
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    // Ausdrücklich Berlin statt ToLocalTime(): das hinge sonst an der Zeitzone des Prozesses.
    // Compose setzt zwar TZ=Europe/Berlin, aber ein Lauf auf dem Host oder ein überschriebenes TZ
    // datierte den letzten Lauf still auf den falschen Tag.
    private static readonly TimeZoneInfo BerlinTz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    /// <summary>Orts-Zeilen: Gesamtdistanz statt Ø-Distanz — an einem Ort zählt, wie viel dort
    /// zusammenkam, nicht wie lang eine einzelne Runde war.</summary>
    public static IReadOnlyList<RunPlaceRow> BuildRunPlaces(IReadOnlyList<RunPlaceSummary> places) =>
        places.Select(place => new RunPlaceRow(
            place.Id,
            place.Name,
            $"{place.RunCount}×",
            $"{place.TotalDistanceKm.ToString("0.0", German)} km",
            place.AveragePaceMinPerKm is { } pace ? FormatPaceValue(pace) : "–",
            place.LastRunUtc is { } last
                ? TimeZoneInfo.ConvertTime(last, BerlinTz).ToString("dd.MM.yyyy", German)
                : "–")).ToList();

    private static string FormatPaceValue(double minPerKm)
    {
        var minutes = (int)minPerKm;
        var seconds = (int)Math.Round((minPerKm - minutes) * 60, MidpointRounding.AwayFromZero);
        if (seconds == 60)
        {
            minutes++;
            seconds = 0;
        }

        return $"{minutes}:{seconds:00} /km";
    }
}
