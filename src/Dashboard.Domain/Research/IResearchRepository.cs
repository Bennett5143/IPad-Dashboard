namespace Dashboard.Domain.Research;

/// <summary>
/// Read access to the <c>research</c> schema — a schema this application does
/// not own. Another repository writes it and versions it with its own
/// migrations; here it is queried and nothing else.
///
/// The interface has no write method by design. "Read-only" that depends on
/// everybody remembering is not read-only; an interface that cannot express a
/// write is.
///
/// Every method returns an empty result when the schema or its tables are
/// absent — the research tooling may simply never have run against this
/// database. That is an empty page, not an error.
/// </summary>
public interface IResearchRepository
{
    /// <summary>Football stories, newest first.</summary>
    Task<IReadOnlyList<FootballNewsItem>> GetFootballNewsAsync(
        int limit = 50, CancellationToken ct = default);

    /// <summary>The market report: latest quote per symbol, plus the commentary of the latest run.</summary>
    Task<MarketReport> GetMarketReportAsync(CancellationToken ct = default);
}
