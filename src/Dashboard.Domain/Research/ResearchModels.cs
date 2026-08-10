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

/// <summary>
/// How well a market explanation is backed by a source. <see cref="Unclear"/> is
/// a deliberate, valid answer: the research tool says "unclear" instead of
/// inventing a reason, and this app must not present it as anything else.
/// </summary>
public enum Causality
{
    Unknown = 0,
    Evidenced,
    Plausible,
    Unclear,
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

/// <summary>An account of what moved a market, graded by how well it is evidenced.</summary>
public sealed class MarketDriver
{
    public long Id { get; init; }
    public string Scope { get; init; } = "";
    public string? Symbol { get; init; }
    public string Statement { get; init; } = "";
    public Causality Causality { get; init; }
    public string? SourceName { get; init; }
    public string? SourceUrl { get; init; }
    public DateOnly? ReportedOn { get; init; }
    public DateTimeOffset FirstSeenAt { get; init; }
    public DateTimeOffset LastSeenAt { get; init; }
}

/// <summary>A market event: a rate decision, an inflation print, a regulatory move, an IPO.</summary>
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
    public DateTimeOffset FirstSeenAt { get; init; }
    public DateTimeOffset LastSeenAt { get; init; }
}

/// <summary>
/// Someone else's Elliott wave reading. Analyst and source are non-nullable in
/// the source schema on purpose: a reading without an author would be one the
/// model produced, and neither the writer nor this app may present such a thing.
/// </summary>
public sealed class ElliottWaveView
{
    public long Id { get; init; }
    public string Symbol { get; init; } = "";
    public string Analyst { get; init; } = "";
    public string Reading { get; init; } = "";
    public DateOnly? PublishedOn { get; init; }
    public string SourceName { get; init; } = "";
    public string SourceUrl { get; init; } = "";
    public DateTimeOffset FirstSeenAt { get; init; }
    public DateTimeOffset LastSeenAt { get; init; }
}

/// <summary>The whole market report of one point in time, as displayed on one page.</summary>
public sealed record MarketReport(
    IReadOnlyList<MarketQuote> Quotes,
    IReadOnlyList<MarketDriver> Drivers,
    IReadOnlyList<MarketEvent> Events,
    IReadOnlyList<ElliottWaveView> WaveViews)
{
    public static readonly MarketReport Empty = new([], [], [], []);

    public bool IsEmpty =>
        Quotes.Count == 0 && Drivers.Count == 0 && Events.Count == 0 && WaveViews.Count == 0;

    /// <summary>Newest timestamp anywhere in the report — how current the page is.</summary>
    public DateTimeOffset? LastUpdated
    {
        get
        {
            var stamps = Quotes.Select(q => q.ObservedAt)
                .Concat(Drivers.Select(d => d.LastSeenAt))
                .Concat(Events.Select(e => e.LastSeenAt))
                .Concat(WaveViews.Select(w => w.LastSeenAt))
                .ToList();
            return stamps.Count == 0 ? null : stamps.Max();
        }
    }
}
