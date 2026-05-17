namespace Dashboard.Infrastructure.Seeding;

public sealed class SeedSettings
{
    public const string SectionName = "Seeding";

    public bool Enabled { get; init; } = true;
}
