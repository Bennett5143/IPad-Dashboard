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

        var drivers = await ReadAsync(
            () => db.MarketDrivers
                .OrderByDescending(driver => driver.LastSeenAt)
                .ThenBy(driver => driver.Scope)
                .ToListAsync(ct),
            Array.Empty<MarketDriver>(),
            "market drivers");

        var events = await ReadAsync(
            () => db.MarketEvents
                .OrderByDescending(marketEvent => marketEvent.EventDate)
                .ThenByDescending(marketEvent => marketEvent.LastSeenAt)
                .ToListAsync(ct),
            Array.Empty<MarketEvent>(),
            "market events");

        var waveViews = await ReadAsync(
            () => db.ElliottWaveViews
                .OrderByDescending(view => view.PublishedOn)
                .ToListAsync(ct),
            Array.Empty<ElliottWaveView>(),
            "wave readings");

        return new MarketReport(quotes, drivers, events, waveViews);
    }

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
