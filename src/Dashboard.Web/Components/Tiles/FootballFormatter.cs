using System.Globalization;

namespace Dashboard.Web.Components.Tiles;

/// <summary>Reine Darstellungs-Helfer für die Fußball-Kachel.</summary>
public static class FootballFormatter
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    private static readonly TimeZoneInfo BerlinTz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    /// <summary>Kurzdatum mit Wochentag, z. B. „Sa 24.05.".</summary>
    public static string ShortDate(DateTimeOffset utc) =>
        TimeZoneInfo.ConvertTime(utc, BerlinTz).ToString("ddd dd.MM.", German);

    /// <summary>Datum + Uhrzeit (Berlin), z. B. „Sa 24.05. 18:30".</summary>
    public static string DateTimeLabel(DateTimeOffset utc) =>
        TimeZoneInfo.ConvertTime(utc, BerlinTz).ToString("ddd dd.MM. HH:mm", German);

    /// <summary>Kurzes, lesbares Wettbewerbs-Label aus dem football-data-Code.</summary>
    public static string Competition(string code) => code switch
    {
        "PD" => "La Liga",
        "BL1" => "Bundesliga",
        "BL2" => "2. Bundesliga",
        "CL" => "Champions League",
        "EL" => "Europa League",
        "PL" => "Premier League",
        "SA" => "Serie A",
        "FL1" => "Ligue 1",
        "DED" => "Eredivisie",
        "PPL" => "Primeira Liga",
        _ => code
    };

    public static string Score(Match match) =>
        match.IsFinished ? $"{match.OwnGoals}:{match.OpponentGoals}" : "–";

    /// <summary>S / U / N (Sieg, Unentschieden, Niederlage) bzw. leer, wenn nicht beendet.</summary>
    public static string OutcomeLabel(MatchOutcome? outcome) => outcome switch
    {
        MatchOutcome.Win => "S",
        MatchOutcome.Draw => "U",
        MatchOutcome.Loss => "N",
        _ => string.Empty
    };

    public static string OutcomeCss(MatchOutcome? outcome) => outcome switch
    {
        MatchOutcome.Win => "outcome-win",
        MatchOutcome.Draw => "outcome-draw",
        MatchOutcome.Loss => "outcome-loss",
        _ => string.Empty
    };

    public static string Venue(Match match) => match.IsHome ? "H" : "A";

    public static string Position(TablePosition? standing) =>
        standing is null ? "–" : $"{standing.Position}.";

    public static string UpdatedAt(DateTimeOffset retrievedAtUtc) =>
        TimeZoneInfo.ConvertTime(retrievedAtUtc, BerlinTz)
            .ToString("HH:mm", CultureInfo.InvariantCulture);
}
