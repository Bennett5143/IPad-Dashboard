using Dashboard.Domain.Research;

namespace Dashboard.Web.Components.Pages;

/// <summary>
/// Macht aus den „Bemerkenswert"-Einträgen des Marktberichts die Panels der Blätter-Ansicht.
/// Rein und testbar; wie bei den Fußball-Nachrichten fällt die Quell-URL hier weg, weil
/// <see cref="NewsDeckItem"/> gar kein Feld dafür hat.
/// </summary>
public static class MarketReportViewBuilder
{
    private static readonly TimeZoneInfo BerlinTz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    /// <summary>
    /// Die Lage ist die erste Karte des Decks, nicht ein Block darüber: sie gehört in dieselbe
    /// Reihe wie das, was sie einordnet, und blätterte man daran vorbei, wäre sie weg.
    /// </summary>
    public static IReadOnlyList<NewsDeckItem> Build(MarketReport report)
    {
        var items = new List<NewsDeckItem>();
        if (report.Situation is { } situation)
        {
            items.Add(SituationItem(situation, report));
        }

        items.AddRange(report.Events.Select(marketEvent => ToDeckItem(marketEvent, report)));
        return items;
    }

    // Ohne Schlagzeile: die Lage ist ein Absatz, keine Meldung. Der Stand steht in der
    // Datumszelle — vorher trug ihn der Seitenkopf, wo er über allem stand und zu nichts gehörte.
    private static NewsDeckItem SituationItem(MarketSituation situation, MarketReport report) => new(
        Eyebrow: "Marktlage",
        EyebrowRight: CorpusLabel(situation),
        Category: null,
        Date: report.LastUpdated is { } stamp
            ? $"Stand {TimeZoneInfo.ConvertTime(stamp, BerlinTz):dd.MM. HH:mm}"
            : null,
        Badge: situation.FiguresFlagged
            ? new NewsDeckBadge("Zahl geprüft", "rs-badge-flagged")
            : null,
        Headline: string.Empty,
        Summary: situation.Body,
        Source: null);

    /// <summary>
    /// Aus welchen Ausgaben der Absatz stammt. Er trägt kein einzelnes Datum — er ist aus einem
    /// ganzen Fenster geschrieben —, also macht das Fenster sein Alter sichtbar.
    /// </summary>
    public static string? CorpusLabel(MarketSituation situation)
    {
        var parts = new List<string>();
        if (situation.IssueCount > 0)
        {
            parts.Add($"{situation.IssueCount} Ausgaben");
        }

        if (situation.NewsletterCount > 0)
        {
            parts.Add($"{situation.NewsletterCount} Newsletter");
        }

        if (situation is { CorpusFrom: { } from, CorpusTo: { } to })
        {
            parts.Add(from == to
                ? from.ToString("dd.MM.yyyy")
                : $"{from:dd.MM.} – {to:dd.MM.yyyy}");
        }

        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }

    // Kopfleiste: Kategorie links, Übereinstimmung rechts. Die Übereinstimmung ist das Maß, das
    // das Tooling selbst berechnet — wie viele Publikationen die Meldung getragen haben.
    // Die erste Meta-Zelle trägt den Zahlen-Hinweis, wenn es einen gibt; sonst bleibt sie leer.
    private static NewsDeckItem ToDeckItem(MarketEvent marketEvent, MarketReport report) => new(
        Eyebrow: CategoryLabel(marketEvent.Category),
        EyebrowRight: Agreement(marketEvent, report),
        Category: marketEvent.FiguresFlagged ? "Zahl geprüft" : null,
        Date: DateLabel(marketEvent),
        Badge: Badge(marketEvent),
        Headline: marketEvent.Headline,
        Summary: marketEvent.Summary,
        Source: Provenance(marketEvent));

    /// <summary>Wie viele der ausgewerteten Newsletter die Meldung getragen haben.</summary>
    public static string? Agreement(MarketEvent marketEvent, MarketReport report)
    {
        var carried = marketEvent.Newsletters.Count;
        if (carried == 0)
        {
            return null;
        }

        var total = report.Situation?.NewsletterCount ?? 0;
        return total > carried ? $"{carried} von {total}" : $"{carried}×";
    }

    // Mehr als eine Publikation zum selben Ereignis ist das Signal; eine ist keins.
    private static NewsDeckBadge? Badge(MarketEvent marketEvent) =>
        marketEvent.Newsletters.Count switch
        {
            0 => null,
            1 => new NewsDeckBadge("einzelne Quelle", "rs-badge-unknown"),
            var count => new NewsDeckBadge($"{count} Quellen", "rs-badge-confirmed"),
        };

    private static string? DateLabel(MarketEvent marketEvent) => marketEvent switch
    {
        { IssueDate: { } issued } => $"Ausgabe {issued:dd.MM.}",
        { EventDate: { } happened } => happened.ToString("dd.MM."),
        _ => null,
    };

    /// <summary>
    /// Woher der Eintrag stammt, in seinen eigenen Begriffen. Eine Corpus-Zeile hat keinen Link,
    /// und das ist kein Mangel: ihre Quelle ist eine Newsletter-Ausgabe, und die Publikationen zu
    /// nennen sagt mehr als eine fehlende URL. Ein Link wird nie gerendert.
    /// </summary>
    private static string? Provenance(MarketEvent marketEvent)
    {
        if (marketEvent.Newsletters.Count > 0)
        {
            return string.Join(", ", marketEvent.Newsletters.Select(NewsletterLabel));
        }

        return string.IsNullOrWhiteSpace(marketEvent.SourceName) ? null : marketEvent.SourceName;
    }

    private static string CategoryLabel(string category) => category switch
    {
        "rate_decision" => "Zinsentscheid",
        "inflation" => "Inflation",
        "macro_data" => "Konjunkturdaten",
        "policy_regulation" => "Politik / Regulierung",
        "ipo" => "Börsengang",
        "listing" => "Neu handelbar",
        "deal" => "Übernahme / Finanzierung",
        "other" => "Sonstiges",
        _ => category,
    };

    // Der Slug ist eine Adresse, kein Titel. Unbekanntes wird gezeigt, wie es ist: ein flussaufwärts
    // ergänzter Newsletter muss auftauchen, nicht hinter einem Fallback verschwinden.
    private static string NewsletterLabel(string slug) => slug switch
    {
        "axios" => "Axios",
        "theupside" => "The Daily Upside",
        "milkroad" => "Milk Road",
        "dealbook" => "NYT DealBook",
        "firstft" => "FT FirstFT",
        _ => slug,
    };
}
