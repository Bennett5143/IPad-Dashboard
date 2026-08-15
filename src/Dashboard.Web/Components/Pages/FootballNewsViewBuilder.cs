using Dashboard.Domain.Research;

namespace Dashboard.Web.Components.Pages;

/// <summary>
/// Macht aus den Meldungen des Recherche-Tools die Panels der Blätter-Ansicht. Rein und testbar —
/// und die Stelle, an der die Quell-URL verlorengeht: <see cref="NewsDeckItem"/> hat kein Feld
/// dafür, die Ansicht kann also gar keinen Link rendern.
/// </summary>
public static class FootballNewsViewBuilder
{
    public static IReadOnlyList<NewsDeckItem> Build(IReadOnlyList<FootballNewsItem> items) =>
        items.Select(ToDeckItem).ToList();

    // Kopfzeile: Liga · Verein · Kategorie · Datum. Fehlt ein Feld, entfällt nur dieses —
    // sonst zeigte ein „·" ins Leere.
    private static NewsDeckItem ToDeckItem(FootballNewsItem item)
    {
        var header = new List<string>();
        if (!string.IsNullOrWhiteSpace(item.Competition))
        {
            header.Add(item.Competition.ToUpperInvariant());
        }

        if (!string.IsNullOrWhiteSpace(item.Club))
        {
            header.Add(item.Club);
        }

        header.Add(CategoryLabel(item.Category));

        if (item.ReportedOn is { } reported)
        {
            header.Add(reported.ToString("dd.MM."));
        }

        return new NewsDeckItem(
            header,
            new NewsDeckBadge(ConfidenceLabel(item.Confidence), $"rs-badge-{BadgeClass(item.Confidence)}"),
            item.Headline,
            item.Summary,
            string.IsNullOrWhiteSpace(item.SourceName) ? null : item.SourceName);
    }

    /// <summary>Die Einstufung des Recherche-Tools, unverändert wiedergegeben.</summary>
    public static string ConfidenceLabel(NewsConfidence confidence) => confidence switch
    {
        NewsConfidence.Confirmed => "bestätigt",
        NewsConfidence.Reported => "berichtet",
        NewsConfidence.Rumour => "Gerücht",
        _ => "unbekannt",
    };

    private static string BadgeClass(NewsConfidence confidence) => confidence switch
    {
        NewsConfidence.Confirmed => "confirmed",
        NewsConfidence.Reported => "reported",
        NewsConfidence.Rumour => "rumour",
        _ => "unknown",
    };

    private static string CategoryLabel(string category) => category switch
    {
        "transfer" => "Transfer",
        "contract" => "Vertrag",
        "coach" => "Trainer",
        "injury" => "Verletzung",
        "club_governance" => "Verein",
        "competition_structure" => "Wettbewerb",
        "sporting_situation" => "Sportlich",
        "other" => "Sonstiges",
        _ => category,
    };
}
