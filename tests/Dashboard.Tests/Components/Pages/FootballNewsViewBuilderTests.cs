using Dashboard.Domain.Research;
using Dashboard.Web.Components;
using Dashboard.Web.Components.Pages;

namespace Dashboard.Tests.Components.Pages;

public class FootballNewsViewBuilderTests
{
    private static FootballNewsItem Item(
        string competition = "Bundesliga",
        string? club = "HSV",
        string category = "transfer",
        NewsConfidence confidence = NewsConfidence.Reported,
        string? sourceName = "kicker",
        string? sourceUrl = "https://example.invalid/artikel",
        DateOnly? reportedOn = null) => new()
        {
            Competition = competition,
            Club = club,
            Category = category,
            Confidence = confidence,
            Headline = "Schlagzeile",
            Summary = "Zusammenfassung",
            SourceName = sourceName,
            SourceUrl = sourceUrl,
            ReportedOn = reportedOn ?? new DateOnly(2026, 8, 14),
        };

    [Fact]
    public void Build_HeaderCarriesLeagueClubCategoryAndDate()
    {
        var deck = FootballNewsViewBuilder.Build([Item()]);

        Assert.Equal(["BUNDESLIGA", "HSV", "Transfer", "14.08."], Assert.Single(deck).HeaderParts);
    }

    [Fact]
    public void Build_MissingFieldsLeaveNoEmptySeparator()
    {
        var deck = FootballNewsViewBuilder.Build([Item(club: null, reportedOn: null)]);

        var header = Assert.Single(deck).HeaderParts;
        Assert.Equal(["BUNDESLIGA", "Transfer", "14.08."], header);
        Assert.DoesNotContain(header, part => string.IsNullOrWhiteSpace(part));
    }

    /// <summary>
    /// Der Kern der Anforderung: im Anzeigekontext gibt es kein Internet, ein Artikel-Link wäre
    /// nicht zu öffnen. Die Ansicht kann gar keinen rendern, weil das Panel-Modell die URL nicht
    /// führt — dieser Test hält fest, dass sie auch nicht durch die Hintertür (Quellenfeld) kommt.
    /// </summary>
    [Fact]
    public void Build_DropsTheSourceUrl_AndKeepsOnlyTheName()
    {
        var deck = FootballNewsViewBuilder.Build([Item(sourceUrl: "https://example.invalid/artikel")]);

        var item = Assert.Single(deck);
        Assert.Equal("kicker", item.Source);
        Assert.DoesNotContain("http", item.Source, StringComparison.OrdinalIgnoreCase);
        Assert.Null(typeof(NewsDeckItem).GetProperty("SourceUrl"));
    }

    [Fact]
    public void Build_WithoutASourceName_LeavesTheSourceEmpty()
    {
        var deck = FootballNewsViewBuilder.Build([Item(sourceName: null)]);

        Assert.Null(Assert.Single(deck).Source);
    }

    [Theory]
    [InlineData(NewsConfidence.Confirmed, "bestätigt", "rs-badge-confirmed")]
    [InlineData(NewsConfidence.Reported, "berichtet", "rs-badge-reported")]
    [InlineData(NewsConfidence.Rumour, "Gerücht", "rs-badge-rumour")]
    public void Build_ShowsTheGradeUnchanged(NewsConfidence confidence, string label, string cssClass)
    {
        var deck = FootballNewsViewBuilder.Build([Item(confidence: confidence)]);

        var badge = Assert.Single(deck).Badge;
        Assert.NotNull(badge);
        Assert.Equal(label, badge!.Label);
        Assert.Equal(cssClass, badge.CssClass);
    }

    [Fact]
    public void Build_KeepsTheOrderItWasGiven()
    {
        var deck = FootballNewsViewBuilder.Build(
        [
            Item(club: "HSV"),
            Item(club: "Bayern"),
            Item(club: "PSG"),
        ]);

        Assert.Equal(["HSV", "Bayern", "PSG"], deck.Select(item => item.HeaderParts[1]));
    }

    [Fact]
    public void Build_WithoutItems_ReturnsNothing()
    {
        Assert.Empty(FootballNewsViewBuilder.Build([]));
    }
}
