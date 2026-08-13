using Dashboard.Domain.Research;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Dashboard.Infrastructure.Research.Configurations;

/// <summary>
/// Converters for the graded fields. The research schema stores them lowercase
/// and pins the allowed set with a check constraint, so the mapping is exact in
/// both directions — but an unrecognised value maps to <c>Unknown</c> rather
/// than throwing: the writer may add a grade before this app knows it, and a
/// page that fails to load is a worse answer than one that says "unknown".
/// </summary>
internal static class GradeConverters
{
    public static readonly ValueConverter<NewsConfidence, string> Confidence = new(
        grade => grade.ToString().ToLowerInvariant(),
        text => ParseConfidence(text));

    // Named methods rather than inline TryParse: an expression tree cannot
    // declare an `out` variable, and EF compiles these converters into one.
    internal static NewsConfidence ParseConfidence(string text) =>
        Enum.TryParse<NewsConfidence>(text, ignoreCase: true, out var grade)
            ? grade
            : NewsConfidence.Unknown;
}

public sealed class FootballNewsItemConfiguration : IEntityTypeConfiguration<FootballNewsItem>
{
    public void Configure(EntityTypeBuilder<FootballNewsItem> builder)
    {
        // ExcludeFromMigrations: the schema belongs to the research repository.
        // Nothing this application generates may create, alter or drop it.
        builder.ToTable("football_news", ResearchDbContext.SchemaName,
            table => table.ExcludeFromMigrations());

        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.Competition).HasColumnName("competition");
        builder.Property(item => item.CompetitionType).HasColumnName("competition_type");
        builder.Property(item => item.Club).HasColumnName("club");
        builder.Property(item => item.Category).HasColumnName("category");
        builder.Property(item => item.Confidence)
            .HasColumnName("confidence")
            .HasConversion(GradeConverters.Confidence);
        builder.Property(item => item.Subject).HasColumnName("subject");
        builder.Property(item => item.Headline).HasColumnName("headline");
        builder.Property(item => item.Summary).HasColumnName("summary");
        builder.Property(item => item.SourceName).HasColumnName("source_name");
        builder.Property(item => item.SourceUrl).HasColumnName("source_url");
        builder.Property(item => item.ReportedOn).HasColumnName("reported_on");
        builder.Property(item => item.FirstSeenAt).HasColumnName("first_seen_at");
        builder.Property(item => item.LastSeenAt).HasColumnName("last_seen_at");
    }
}

public sealed class MarketQuoteConfiguration : IEntityTypeConfiguration<MarketQuote>
{
    public void Configure(EntityTypeBuilder<MarketQuote> builder)
    {
        builder.ToTable("market_quotes", ResearchDbContext.SchemaName,
            table => table.ExcludeFromMigrations());

        builder.HasKey(quote => quote.Id);
        builder.Property(quote => quote.Id).HasColumnName("id");
        builder.Property(quote => quote.AssetClass).HasColumnName("asset_class");
        builder.Property(quote => quote.Symbol).HasColumnName("symbol");
        builder.Property(quote => quote.Name).HasColumnName("name");
        builder.Property(quote => quote.Currency).HasColumnName("currency");
        builder.Property(quote => quote.Unit).HasColumnName("unit");
        builder.Property(quote => quote.Value).HasColumnName("value");
        builder.Property(quote => quote.Change24hPct).HasColumnName("change_24h_pct");
        builder.Property(quote => quote.Change7dPct).HasColumnName("change_7d_pct");
        builder.Property(quote => quote.Change30dPct).HasColumnName("change_30d_pct");
        builder.Property(quote => quote.ObservedAt).HasColumnName("observed_at");
        builder.Property(quote => quote.Source).HasColumnName("source");
        builder.Property(quote => quote.SourceUrl).HasColumnName("source_url");
    }
}

public sealed class MarketSituationConfiguration : IEntityTypeConfiguration<MarketSituation>
{
    public void Configure(EntityTypeBuilder<MarketSituation> builder)
    {
        builder.ToTable("market_situation", ResearchDbContext.SchemaName,
            table => table.ExcludeFromMigrations());

        builder.HasKey(situation => situation.Id);
        builder.Property(situation => situation.Id).HasColumnName("id");
        builder.Property(situation => situation.RunId).HasColumnName("run_id");
        builder.Property(situation => situation.Body).HasColumnName("body");
        builder.Property(situation => situation.FiguresFlagged).HasColumnName("figures_flagged");
        builder.Property(situation => situation.CorpusFrom).HasColumnName("corpus_from");
        builder.Property(situation => situation.CorpusTo).HasColumnName("corpus_to");
        builder.Property(situation => situation.IssueCount).HasColumnName("issue_count");
        builder.Property(situation => situation.NewsletterCount).HasColumnName("newsletter_count");
        builder.Property(situation => situation.CreatedAt).HasColumnName("created_at");
    }
}

public sealed class MarketEventConfiguration : IEntityTypeConfiguration<MarketEvent>
{
    public void Configure(EntityTypeBuilder<MarketEvent> builder)
    {
        builder.ToTable("market_events", ResearchDbContext.SchemaName,
            table => table.ExcludeFromMigrations());

        builder.HasKey(marketEvent => marketEvent.Id);
        builder.Property(marketEvent => marketEvent.Id).HasColumnName("id");
        builder.Property(marketEvent => marketEvent.Category).HasColumnName("category");
        builder.Property(marketEvent => marketEvent.Region).HasColumnName("region");
        builder.Property(marketEvent => marketEvent.Headline).HasColumnName("headline");
        builder.Property(marketEvent => marketEvent.Summary).HasColumnName("summary");
        builder.Property(marketEvent => marketEvent.EventDate).HasColumnName("event_date");
        builder.Property(marketEvent => marketEvent.SourceName).HasColumnName("source_name");
        builder.Property(marketEvent => marketEvent.SourceUrl).HasColumnName("source_url");
        builder.Property(marketEvent => marketEvent.SourceOrigin).HasColumnName("source_origin");
        // text[] in Postgres, and Npgsql maps it to a list without help. Read
        // as IReadOnlyList so nothing on this side can append to it.
        builder.Property(marketEvent => marketEvent.Newsletters).HasColumnName("newsletters");
        builder.Property(marketEvent => marketEvent.IssueDate).HasColumnName("issue_date");
        builder.Property(marketEvent => marketEvent.IssueId).HasColumnName("issue_id");
        builder.Property(marketEvent => marketEvent.FiguresFlagged).HasColumnName("figures_flagged");
        builder.Property(marketEvent => marketEvent.LastRunId).HasColumnName("last_run_id");
        builder.Property(marketEvent => marketEvent.FirstSeenAt).HasColumnName("first_seen_at");
        builder.Property(marketEvent => marketEvent.LastSeenAt).HasColumnName("last_seen_at");
    }
}
