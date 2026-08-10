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

    public static readonly ValueConverter<Causality, string> Causality = new(
        grade => grade.ToString().ToLowerInvariant(),
        text => ParseCausality(text));

    // Named methods rather than inline TryParse: an expression tree cannot
    // declare an `out` variable, and EF compiles these converters into one.
    internal static NewsConfidence ParseConfidence(string text) =>
        Enum.TryParse<NewsConfidence>(text, ignoreCase: true, out var grade)
            ? grade
            : NewsConfidence.Unknown;

    internal static Causality ParseCausality(string text) =>
        Enum.TryParse<Causality>(text, ignoreCase: true, out var grade)
            ? grade
            : Domain.Research.Causality.Unknown;
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

public sealed class MarketDriverConfiguration : IEntityTypeConfiguration<MarketDriver>
{
    public void Configure(EntityTypeBuilder<MarketDriver> builder)
    {
        builder.ToTable("market_drivers", ResearchDbContext.SchemaName,
            table => table.ExcludeFromMigrations());

        builder.HasKey(driver => driver.Id);
        builder.Property(driver => driver.Id).HasColumnName("id");
        builder.Property(driver => driver.Scope).HasColumnName("scope");
        builder.Property(driver => driver.Symbol).HasColumnName("symbol");
        builder.Property(driver => driver.Statement).HasColumnName("statement");
        builder.Property(driver => driver.Causality)
            .HasColumnName("causality")
            .HasConversion(GradeConverters.Causality);
        builder.Property(driver => driver.SourceName).HasColumnName("source_name");
        builder.Property(driver => driver.SourceUrl).HasColumnName("source_url");
        builder.Property(driver => driver.ReportedOn).HasColumnName("reported_on");
        builder.Property(driver => driver.FirstSeenAt).HasColumnName("first_seen_at");
        builder.Property(driver => driver.LastSeenAt).HasColumnName("last_seen_at");
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
        builder.Property(marketEvent => marketEvent.FirstSeenAt).HasColumnName("first_seen_at");
        builder.Property(marketEvent => marketEvent.LastSeenAt).HasColumnName("last_seen_at");
    }
}

public sealed class ElliottWaveViewConfiguration : IEntityTypeConfiguration<ElliottWaveView>
{
    public void Configure(EntityTypeBuilder<ElliottWaveView> builder)
    {
        builder.ToTable("elliott_wave_views", ResearchDbContext.SchemaName,
            table => table.ExcludeFromMigrations());

        builder.HasKey(view => view.Id);
        builder.Property(view => view.Id).HasColumnName("id");
        builder.Property(view => view.Symbol).HasColumnName("symbol");
        builder.Property(view => view.Analyst).HasColumnName("analyst");
        builder.Property(view => view.Reading).HasColumnName("reading");
        builder.Property(view => view.PublishedOn).HasColumnName("published_on");
        builder.Property(view => view.SourceName).HasColumnName("source_name");
        builder.Property(view => view.SourceUrl).HasColumnName("source_url");
        builder.Property(view => view.FirstSeenAt).HasColumnName("first_seen_at");
        builder.Property(view => view.LastSeenAt).HasColumnName("last_seen_at");
    }
}
