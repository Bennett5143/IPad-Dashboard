using Dashboard.Domain.Research;
using Dashboard.Infrastructure.Research;

using Microsoft.EntityFrameworkCore;

namespace Dashboard.Tests.Research;

/// <summary>
/// The shape of the market report after the research tooling was rebuilt around
/// a newsletter corpus: a paragraph on how things stand, the quote table, and a
/// few entries carrying how many publications ran each of them.
/// </summary>
public class MarketReportTests
{
    private static MarketQuote Quote(string symbol = "BTC", int hour = 12) => new()
    {
        AssetClass = "crypto",
        Symbol = symbol,
        Name = symbol,
        Unit = "price",
        Value = 63050m,
        ObservedAt = new DateTimeOffset(2026, 8, 13, hour, 0, 0, TimeSpan.Zero),
        Source = "CoinGecko",
        SourceUrl = "https://example.invalid/x",
    };

    private static MarketSituation Situation(int hour = 18) => new()
    {
        Id = 1,
        RunId = 2053,
        Body = "Stocks edged higher this week.",
        CorpusFrom = new DateOnly(2026, 8, 12),
        CorpusTo = new DateOnly(2026, 8, 13),
        IssueCount = 10,
        NewsletterCount = 5,
        CreatedAt = new DateTimeOffset(2026, 8, 13, hour, 0, 0, TimeSpan.Zero),
    };

    private static MarketEvent Event(int hour = 14, params string[] newsletters) => new()
    {
        Category = "deal",
        Headline = "Apollo takes a stake in the Yankees",
        Summary = "A mix of debt and equity.",
        SourceOrigin = "corpus",
        Newsletters = newsletters,
        IssueDate = new DateOnly(2026, 8, 12),
        LastSeenAt = new DateTimeOffset(2026, 8, 13, hour, 0, 0, TimeSpan.Zero),
    };

    [Fact]
    public void AnEmptyReportIsEmpty()
    {
        Assert.True(MarketReport.Empty.IsEmpty);
        Assert.Null(MarketReport.Empty.LastUpdated);
    }

    [Fact]
    public void AReportWithOnlyASituationIsNotEmpty()
    {
        // The paragraph is half the page. A run that produced one and no
        // entries — a quiet week — must not read as "nothing was written".
        var report = new MarketReport([], Situation(), []);

        Assert.False(report.IsEmpty);
    }

    [Fact]
    public void TheSituationCountsTowardsHowCurrentThePageIs()
    {
        var report = new MarketReport([Quote(hour: 12)], Situation(hour: 18), []);

        Assert.Equal(18, report.LastUpdated!.Value.Hour);
    }

    [Fact]
    public void TheNewestTimestampWinsWhereverItSits()
    {
        var report = new MarketReport(
            [Quote(hour: 23)], Situation(hour: 18), [Event(hour: 14)]);

        Assert.Equal(23, report.LastUpdated!.Value.Hour);
    }

    [Fact]
    public void AnEntryFromNoNewsletterCarriesAnEmptyList()
    {
        // Rate decisions are computed from our own quote series, so they have
        // no newsletter behind them — and an empty list, never null, is what
        // the page can count without checking.
        var decision = new MarketEvent { Category = "rate_decision" };

        Assert.Empty(decision.Newsletters);
    }

    [Fact]
    public void MostAgreedFirstTranslatesTheNewsletterCountToSql()
    {
        // `Newsletters` is a Postgres text[]. Counting it in the database is
        // what makes agreement an ordering rather than something this app sorts
        // in memory — and if Npgsql ever stopped translating it, the failure
        // would otherwise appear at runtime on the page.
        var options = new DbContextOptionsBuilder<ResearchDbContext>()
            .UseNpgsql("Host=localhost;Database=none;Username=none")
            .Options;
        using var db = new ResearchDbContext(options);

        var sql = ResearchRepository.MostAgreedFirst(db.MarketEvents).ToQueryString();

        Assert.Contains("cardinality", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("newsletters", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OneReportIsShown_NotEveryEntryEverWritten()
    {
        // With a weekly run and a 45-day dedupe window the table keeps rows
        // long after they stopped being noteworthy — and the page that showed
        // all of them was still listing the search-driven entries of 2026-08-10
        // under the new report.
        var options = new DbContextOptionsBuilder<ResearchDbContext>()
            .UseNpgsql("Host=localhost;Database=none;Username=none")
            .Options;
        using var db = new ResearchDbContext(options);

        var scoped = ResearchRepository.OfRun(db.MarketEvents, 2053).ToQueryString();

        Assert.Contains("last_run_id", scoped, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WHERE", scoped, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WithoutASituationEveryEntryIsShownRatherThanNone()
    {
        // A database written by the older, search-driven version has entries and
        // no run to tie them to. Showing nothing would read as "the tooling
        // wrote nothing", which is the opposite of the truth.
        var options = new DbContextOptionsBuilder<ResearchDbContext>()
            .UseNpgsql("Host=localhost;Database=none;Username=none")
            .Options;
        using var db = new ResearchDbContext(options);

        var all = ResearchRepository.OfRun(db.MarketEvents, null).ToQueryString();

        Assert.DoesNotContain("WHERE", all, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheResearchContextStillMapsEveryTableTheReportNeeds()
    {
        var options = new DbContextOptionsBuilder<ResearchDbContext>()
            .UseNpgsql("Host=localhost;Database=none;Username=none")
            .Options;
        using var db = new ResearchDbContext(options);

        var tables = db.Model.GetEntityTypes()
            .Select(entity => entity.GetTableName())
            .ToList();

        Assert.Contains("market_situation", tables);
        Assert.Contains("market_events", tables);
        Assert.Contains("market_quotes", tables);
        // Retired by the research repository on 2026-08-13: the per-symbol
        // driver list produced a line per holding whether or not anything had
        // happened, and no newsletter carries an Elliott wave count.
        Assert.DoesNotContain("market_drivers", tables);
        Assert.DoesNotContain("elliott_wave_views", tables);
    }
}
