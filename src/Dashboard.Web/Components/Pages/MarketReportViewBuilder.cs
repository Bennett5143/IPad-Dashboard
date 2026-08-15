using Dashboard.Domain.Research;

namespace Dashboard.Web.Components.Pages;

/// <summary>
/// Macht aus den „Bemerkenswert"-Einträgen des Marktberichts die Panels der Blätter-Ansicht.
/// Rein und testbar; wie bei den Fußball-Nachrichten fällt die Quell-URL hier weg, weil
/// <see cref="NewsDeckItem"/> gar kein Feld dafür hat.
/// </summary>
public static class MarketReportViewBuilder
{
    public static IReadOnlyList<NewsDeckItem> Build(MarketReport report) =>
        report.Events.Select(marketEvent => ToDeckItem(marketEvent, report)).ToList();

    // Kopfzeile: Kategorie · Übereinstimmung · Datum. Die Übereinstimmung ist das Maß, das das
    // Tooling selbst berechnet — wie viele Publikationen die Meldung getragen haben.
    private static NewsDeckItem ToDeckItem(MarketEvent marketEvent, MarketReport report)
    {
        var header = new List<string> { CategoryLabel(marketEvent.Category) };

        if (Agreement(marketEvent, report) is { } agreement)
        {
            header.Add(agreement);
        }

        if (DateLabel(marketEvent) is { } date)
        {
            header.Add(date);
        }

        if (marketEvent.FiguresFlagged)
        {
            header.Add("Zahl geprüft");
        }

        return new NewsDeckItem(
            header,
            Badge(marketEvent),
            marketEvent.Headline,
            marketEvent.Summary,
            Provenance(marketEvent));
    }

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
