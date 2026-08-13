using Dashboard.Domain.Research;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Npgsql;

namespace Dashboard.Infrastructure.Research;

/// <summary>
/// Queries the research schema with LINQ, and treats "the schema is not there"
/// as an empty result rather than as a failure.
/// </summary>
public sealed class ResearchRepository : IResearchRepository
{
    // Postgres SQLSTATEs for "that schema does not exist" and "that table does
    // not exist". Catching exactly these two — rather than every exception —
    // keeps a real outage loud: a connection failure must not look like an
    // empty page.
    private const string UndefinedSchema = "3F000";
    private const string UndefinedTable = "42P01";

    private readonly IDbContextFactory<ResearchDbContext> _factory;
    private readonly ILogger<ResearchRepository> _log;

    public ResearchRepository(
        IDbContextFactory<ResearchDbContext> factory,
        ILogger<ResearchRepository> log)
    {
        _factory = factory;
        _log = log;
    }

    public async Task<IReadOnlyList<FootballNewsItem>> GetFootballNewsAsync(
        int limit = 50, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await ReadAsync(
            () => db.FootballNews
                .OrderByDescending(item => item.LastSeenAt)
                .ThenByDescending(item => item.Id)
                .Take(limit)
                .ToListAsync(ct),
            Array.Empty<FootballNewsItem>(),
            "football news");
    }

    public async Task<MarketReport> GetMarketReportAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        // One row per symbol: the newest observation. Older rows stay in the
        // table — they are a time series, not duplicates — but a report shows
        // the current state.
        var quotes = await ReadAsync(
            () => db.MarketQuotes
                .GroupBy(quote => quote.Symbol)
                .Select(group => group
                    .OrderByDescending(quote => quote.ObservedAt)
                    .First())
                .ToListAsync(ct),
            Array.Empty<MarketQuote>(),
            "market quotes");

        // One row per run of the research tool, so the page wants the newest.
        // Ordered by id rather than by created_at: two runs in the same second
        // would tie on the timestamp, and the identity column cannot.
        var situation = await ReadAsync(
            () => db.MarketSituations
                .OrderByDescending(item => item.Id)
                .Take(1)
                .ToListAsync(ct),
            Array.Empty<MarketSituation>(),
            "market situation");

        // One report, not an archive. The entries shown are those the run that
        // wrote the paragraph also wrote: "noteworthy" is a handful of things
        // about now, and with a weekly run and a 45-day dedupe window the table
        // would otherwise grow without bound on the page.
        //
        // Without a situation there is nothing to tie entries to — a database
        // written by the older, search-driven version — so everything is shown
        // rather than nothing.
        var current = situation.FirstOrDefault();
        var events = await ReadAsync(
            () => MostAgreedFirst(OfRun(db.MarketEvents, current?.RunId)).ToListAsync(ct),
            Array.Empty<MarketEvent>(),
            "market events");

        return new MarketReport(quotes, current, events);
    }

    /// <summary>
    /// How many newsletters carried a story, first.
    ///
    /// The importance measure the research tooling computes from its corpus,
    /// and the only ordering this page applies that is not a timestamp — a
    /// count of independent publications is a statement no wording can move.
    /// Named and public so the SQL translation of <c>newsletters.Count</c> can
    /// be asserted without a database: it becomes <c>cardinality()</c>, and a
    /// version of Npgsql that stopped doing so would otherwise fail at runtime.
    /// </summary>
    public static IQueryable<MarketEvent> MostAgreedFirst(IQueryable<MarketEvent> events) =>
        events
            .OrderByDescending(marketEvent => marketEvent.Newsletters.Count)
            .ThenByDescending(marketEvent => marketEvent.IssueDate)
            .ThenByDescending(marketEvent => marketEvent.LastSeenAt);

    /// <summary>The entries of one run, or all of them when there is no run to tie them to.</summary>
    public static IQueryable<MarketEvent> OfRun(IQueryable<MarketEvent> events, long? runId) =>
        runId is { } id ? events.Where(marketEvent => marketEvent.LastRunId == id) : events;

    private async Task<IReadOnlyList<T>> ReadAsync<T>(
        Func<Task<List<T>>> query, IReadOnlyList<T> fallback, string what)
    {
        try
        {
            return await query();
        }
        catch (PostgresException ex) when (
            ex.SqlState is UndefinedSchema or UndefinedTable)
        {
            // The research tooling has not run against this database yet.
            _log.LogInformation(
                "Research schema not present ({SqlState}); {What} shown as empty.",
                ex.SqlState, what);
            return fallback;
        }
    }
}
