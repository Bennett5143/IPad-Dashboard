namespace Dashboard.Infrastructure.Seeding;

public sealed class SeedSettings
{
    public const string SectionName = "Seeding";

    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Wendet ausstehende EF-Migrationen beim Start automatisch an (Single-Instance-
    /// Appliance, z. B. Container auf dem Pi ohne .NET-SDK). Default aus: im Dev-Setup
    /// bleibt <c>dotnet ef database update</c> der explizite, bewusste Schritt.
    /// </summary>
    public bool ApplyMigrations { get; init; }
}
