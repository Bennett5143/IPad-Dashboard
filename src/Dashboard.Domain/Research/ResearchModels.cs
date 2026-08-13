namespace Dashboard.Domain.Research;

/// <summary>
/// How well a football story is established, as the research tool recorded it.
/// The values mirror a check constraint in the <c>research</c> schema, so this
/// set is fixed by the writer, not by us.
/// </summary>
public enum NewsConfidence
{
    /// <summary>Value the writer produced that this app does not know yet.</summary>
    Unknown = 0,
    Confirmed,
    Reported,
    Rumour,
}

/// <summary>One football story. Written by the research tool, read-only here.</summary>
public sealed class FootballNewsItem
{
    public long Id { get; init; }
    public string Competition { get; init; } = "";
    public string CompetitionType { get; init; } = "";
    public string? Club { get; init; }
    public string Category { get; init; } = "";
    public NewsConfidence Confidence { get; init; }
    public string Subject { get; init; } = "";
    public string Headline { get; init; } = "";
    public string Summary { get; init; } = "";
    public string? SourceName { get; init; }
    public string? SourceUrl { get; init; }
    public DateOnly? ReportedOn { get; init; }
    public DateTimeOffset FirstSeenAt { get; init; }
    public DateTimeOffset LastSeenAt { get; init; }
}

/// <summary>
/// One observation of a price, index level or policy rate — with the source that
/// supplied it and the timestamp THAT source reported, never our fetch time.
///
/// Since the market report was rebuilt around a newsletter corpus, this table is
/// the ONLY place figures appear. The report itself no longer prints any: a
/// price quoted in prose is stale the moment it is written and would contradict
/// the table beside it.
/// </summary>
public sealed class MarketQuote
{
    public long Id { get; init; }
    public string AssetClass { get; init; } = "";
    public string Symbol { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Currency { get; init; }
    public string Unit { get; init; } = "";
    public decimal Value { get; init; }
    public decimal? Change24hPct { get; init; }
    public decimal? Change7dPct { get; init; }
    public decimal? Change30dPct { get; init; }
    public DateTimeOffset ObservedAt { get; init; }
    public string Source { get; init; } = "";
    public string SourceUrl { get; init; } = "";
}

/// <summary>
/// How markets stand, in one paragraph — the opening half of the report.
///
/// One row per run of the research tool. It carries no date of its own because
/// it is drawn from every issue in a window rather than from one of them; the
/// window is here instead, and it is what makes ageing visible. A paragraph
/// whose newest issue is four days old says so.
/// </summary>
public sealed class MarketSituation
{
    public long Id { get; init; }
    public long RunId { get; init; }
    public string Body { get; init; } = "";

    /// <summary>
    /// The writer found a figure in this text that restates a quantity it
    /// fetches itself. Marked, never edited: cutting the number out would leave
    /// broken prose and hide the evidence that the rule was broken.
    /// </summary>
    public bool FiguresFlagged { get; init; }

    public DateOnly? CorpusFrom { get; init; }
    public DateOnly? CorpusTo { get; init; }
    public int IssueCount { get; init; }
    public int NewsletterCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// One noteworthy market event: a rate decision, a macro release, a regulatory
/// move, a listing, a deal.
///
/// <see cref="Newsletters"/> is the relevance measure and the reason the rebuilt
/// report exists: how many independent publications carried a story is a
/// statement about its importance that no wording can move. <see cref="IssueDate"/>
/// is the date of the issue the wording came from — every statement carries one,
/// so that ageing is visible rather than invisible.
/// </summary>
public sealed class MarketEvent
{
    public long Id { get; init; }
    public string Category { get; init; } = "";
    public string? Region { get; init; }
    public string Headline { get; init; } = "";
    public string Summary { get; init; } = "";
    public DateOnly? EventDate { get; init; }
    public string? SourceName { get; init; }
    public string? SourceUrl { get; init; }

    /// <summary>
    /// Where the row came from: <c>corpus</c> (written from a newsletter issue,
    /// no link by nature), <c>measured</c> (computed from our own quote series,
    /// links to the API), <c>model</c>, <c>matched</c> or <c>none</c>. An empty
    /// link is only a defect for the last of those.
    /// </summary>
    public string SourceOrigin { get; init; } = "";

    /// <summary>Slugs of every newsletter that carried this. Empty for a row that came from no newsletter.</summary>
    public IReadOnlyList<string> Newsletters { get; init; } = [];

    public DateOnly? IssueDate { get; init; }
    public long? IssueId { get; init; }
    public bool FiguresFlagged { get; init; }

    /// <summary>
    /// The run that last wrote this row. The page shows one report — the
    /// entries of the run that also wrote the situation paragraph — because
    /// "noteworthy" is a handful of things about now, not everything the
    /// tooling has ever recorded.
    /// </summary>
    public long LastRunId { get; init; }

    public DateTimeOffset FirstSeenAt { get; init; }
    public DateTimeOffset LastSeenAt { get; init; }
}

/// <summary>The whole market report of one point in time, as displayed on one page.</summary>
public sealed record MarketReport(
    IReadOnlyList<MarketQuote> Quotes,
    MarketSituation? Situation,
    IReadOnlyList<MarketEvent> Events)
{
    public static readonly MarketReport Empty = new([], null, []);

    public bool IsEmpty => Quotes.Count == 0 && Situation is null && Events.Count == 0;

    /// <summary>Newest timestamp anywhere in the report — how current the page is.</summary>
    public DateTimeOffset? LastUpdated
    {
        get
        {
            var stamps = Quotes.Select(quote => quote.ObservedAt)
                .Concat(Events.Select(marketEvent => marketEvent.LastSeenAt))
                .ToList();
            if (Situation is { } situation)
            {
                stamps.Add(situation.CreatedAt);
            }

            return stamps.Count == 0 ? null : stamps.Max();
        }
    }
}
