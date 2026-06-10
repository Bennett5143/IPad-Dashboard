namespace Dashboard.Infrastructure.Football;

/// <summary>
/// Konfiguration der Fußball-Anbindung (football-data.org v4). Vereine und Intervall stehen in
/// <c>appsettings.json</c> (Sektion <see cref="SectionName"/>); der <see cref="ApiKey"/> gehört
/// als Geheimnis in User-Secrets/Umgebungsvariablen (Header <c>X-Auth-Token</c>).
/// </summary>
public sealed class FootballOptions
{
    public const string SectionName = "Football";

    public string ApiKey { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = "https://api.football-data.org/";

    public TimeSpan RefreshInterval { get; init; } = TimeSpan.FromMinutes(30);

    public int RecentCount { get; init; } = 3;
    public int UpcomingCount { get; init; } = 2;

    public IReadOnlyList<FootballTeamConfig> Teams { get; init; } = [];
}

/// <summary>Ein zu beobachtender Verein. <see cref="TeamId"/>/<see cref="CompetitionCode"/> sind football-data.org-IDs.</summary>
public sealed class FootballTeamConfig
{
    public string Name { get; init; } = string.Empty;
    public int TeamId { get; init; }
    public string CompetitionCode { get; init; } = string.Empty;
}
