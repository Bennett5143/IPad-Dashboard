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

    // Kopfleiste: Wettbewerb links, Verein rechts. Meta-Zeile: Kategorie, Datum, Bewertung.
    // Fehlt ein Feld, entfällt nur seine Zelle — die Karte hat für jedes eine eigene.
    private static NewsDeckItem ToDeckItem(FootballNewsItem item) => new(
        Eyebrow: string.IsNullOrWhiteSpace(item.Competition)
            ? "Fußball".ToUpperInvariant()
            : item.Competition.ToUpperInvariant(),
        EyebrowRight: string.IsNullOrWhiteSpace(item.Club) ? null : item.Club,
        Category: CategoryLabel(item.Category),
        Date: item.ReportedOn?.ToString("dd.MM.yyyy"),
        Badge: new NewsDeckBadge(ConfidenceLabel(item.Confidence), $"rs-badge-{BadgeClass(item.Confidence)}"),
        Headline: item.Headline,
        Summary: item.Summary,
        Source: string.IsNullOrWhiteSpace(item.SourceName) ? null : item.SourceName);

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
