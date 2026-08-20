using Dashboard.Domain.Research;
using Dashboard.Web.Components;
using Dashboard.Web.Components.Pages;

namespace Dashboard.Tests.Components.Pages;

public class MarketReportViewBuilderTests
{
    private static MarketEvent Event(
        string category = "rate_decision",
        string[]? newsletters = null,
        DateOnly? issueDate = null,
        DateOnly? eventDate = null,
        bool figuresFlagged = false,
        string? sourceName = "Reuters",
        string? sourceUrl = "https://example.invalid/artikel") => new()
        {
            Category = category,
            Headline = "Schlagzeile",
            Summary = "Zusammenfassung",
            Newsletters = newsletters ?? ["axios", "milkroad"],
            IssueDate = issueDate,
            EventDate = eventDate,
            FiguresFlagged = figuresFlagged,
            SourceName = sourceName,
            SourceUrl = sourceUrl,
        };

    private static MarketSituation Situation(int newsletterCount = 4) => new()
    {
        Body = "Die Lage",
        NewsletterCount = newsletterCount,
    };

    private static MarketReport Report(params MarketEvent[] events) =>
        new([], Situation(), events);

    /// <summary>
    /// Die Ereignis-Karten ohne die Lage, die als erste Karte des Decks davorsteht.
    /// </summary>
    private static IReadOnlyList<NewsDeckItem> Events(MarketReport report) =>
        MarketReportViewBuilder.Build(report).Skip(1).ToList();

    [Fact]
    public void Build_PutsTheSituationFirst()
    {
        // Die Lage steht in derselben Reihe wie das, was sie einordnet — als Block darüber wäre
        // sie beim Blättern nicht mehr zu sehen gewesen.
        var deck = MarketReportViewBuilder.Build(Report(Event()));

        Assert.Equal(2, deck.Count);
        var situation = deck[0];
        Assert.Equal("Marktlage", situation.Eyebrow);
        Assert.Equal("4 Newsletter", situation.EyebrowRight);
        Assert.Equal("Die Lage", situation.Summary);
        Assert.Empty(situation.Headline);      // ein Absatz, keine Meldung
        Assert.Null(situation.Badge);          // FiguresFlagged nicht gesetzt
    }

    [Fact]
    public void Build_WithoutASituation_StartsWithTheEvents()
    {
        var deck = MarketReportViewBuilder.Build(new MarketReport([], null, [Event()]));

        Assert.Equal("Zinsentscheid", Assert.Single(deck).Eyebrow);
    }

    [Fact]
    public void Build_SituationCarriesTheFlaggedFigureAsItsBadge()
    {
        var report = new MarketReport(
            [], new MarketSituation { Body = "Die Lage", FiguresFlagged = true }, []);

        var badge = Assert.Single(MarketReportViewBuilder.Build(report)).Badge;

        Assert.NotNull(badge);
        Assert.Equal("Zahl geprüft", badge!.Label);
        Assert.Equal("rs-badge-flagged", badge.CssClass);
    }

    [Fact]
    public void Build_FillsTheBarAndTheMetaRow()
    {
        var item = Assert.Single(Events(
            Report(Event(issueDate: new DateOnly(2026, 8, 12)))));

        Assert.Equal("Zinsentscheid", item.Eyebrow);        // Kopfleiste links
        Assert.Equal("2 von 4", item.EyebrowRight);         // Kopfleiste rechts
        Assert.Equal("Ausgabe 12.08.", item.Date);
    }

    [Fact]
    public void Build_WithoutAnIssueDate_FallsBackToTheEventDate()
    {
        var deck = Events(
            Report(Event(eventDate: new DateOnly(2026, 8, 9))));

        Assert.Equal("09.08.", Assert.Single(deck).Date);
    }

    [Fact]
    public void Build_MarksACheckedFigure()
    {
        Assert.Equal("Zahl geprüft",
            Assert.Single(Events(Report(Event(figuresFlagged: true)))).Category);
        Assert.Null(Assert.Single(Events(Report(Event()))).Category);
    }

    /// <summary>Mehr als eine Publikation zum selben Ereignis ist das Signal; eine ist keins.</summary>
    [Fact]
    public void Build_BadgeSeparatesOneSourceFromSeveral()
    {
        var single = Events(Report(Event(newsletters: ["axios"])));
        Assert.Equal("einzelne Quelle", Assert.Single(single).Badge!.Label);

        var several = Events(Report(Event(newsletters: ["axios", "firstft", "milkroad"])));
        Assert.Equal("3 Quellen", Assert.Single(several).Badge!.Label);
        Assert.Equal("rs-badge-confirmed", Assert.Single(several).Badge!.CssClass);
    }

    [Fact]
    public void Build_WithoutNewsletters_ShowsNoAgreementAndNoBadge()
    {
        var deck = Events(Report(Event(newsletters: [])));

        var item = Assert.Single(deck);
        Assert.Null(item.Badge);
        Assert.Null(item.EyebrowRight);
    }

    /// <summary>
    /// Eine Corpus-Zeile hat keinen Link, und das ist kein Mangel — ihre Quelle sind die
    /// Newsletter, die sie getragen haben. Ein Link entsteht hier ohnehin nie.
    /// </summary>
    [Fact]
    public void Build_NamesTheNewslettersInsteadOfALink()
    {
        var deck = Events(Report(Event(newsletters: ["axios", "theupside"])));

        var item = Assert.Single(deck);
        Assert.Equal("Axios, The Daily Upside", item.Source);
        Assert.DoesNotContain("http", item.Source!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_WithoutNewsletters_KeepsTheSourceNameButNeverTheUrl()
    {
        var deck = Events(
            Report(Event(newsletters: [], sourceName: "Reuters", sourceUrl: "https://example.invalid/x")));

        Assert.Equal("Reuters", Assert.Single(deck).Source);
    }

    [Fact]
    public void Build_UnknownNewsletterSlug_IsShownAsItIs()
    {
        var deck = Events(Report(Event(newsletters: ["neuerdienst"])));

        Assert.Equal("neuerdienst", Assert.Single(deck).Source);
    }

    [Fact]
    public void Build_WithoutEvents_ReturnsNothing()
    {
        Assert.Empty(Events(MarketReport.Empty));
    }
}
