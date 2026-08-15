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

    public TimeSpan RefreshInterval { get; init; } = TimeSpan.FromMinutes(20);

    /// <summary>
    /// Pause zwischen zwei API-Calls eines Refreshs. football-data.org Free-Tier erlaubt 10/min.
    /// Mit sechs Vereinen sind es 12 Calls je Refresh; bei 8 s liefen zwei davon nachweislich in
    /// ein 429, mit 10 s bleibt der Refresh bei 6 Calls/Minute. In Tests auf
    /// <see cref="TimeSpan.Zero"/> setzen.
    /// </summary>
    public TimeSpan InterCallDelay { get; init; } = TimeSpan.FromSeconds(10);

    public int RecentCount { get; init; } = 3;
    public int UpcomingCount { get; init; } = 2;

    /// <summary>
    /// Ligen, deren vollständige Tabelle auf <c>/football</c> gezeigt wird (Top-5-Tabellen, je 1
    /// Standings-Call). Unabhängig von den getrackten Vereinen; leer = keine Ligatabellen.
    /// <para>
    /// Hier steht bewusst <em>kein</em> Vorgabewert — die Vorgabe steht in <c>appsettings.json</c>.
    /// Der Configuration-Binder hängt an eine vorbelegte Collection an, statt sie zu ersetzen:
    /// dieselben Codes im Code und in der Konfiguration ergäben jede Liga doppelt, inklusive
    /// doppeltem Standings-Call je Refresh.
    /// </para>
    /// </summary>
    public IReadOnlyList<string> LeagueCodes { get; init; } = [];

    /// <summary>Code der Champions League (immer geholt: Ligaphase-Tabelle + K.o.-Bracket). Leer = aus.</summary>
    public string ChampionsLeagueCode { get; init; } = "CL";

    /// <summary>Turnier-Fenster (EM/WM). Nur aktive werden abgerufen (null Idle-Calls außerhalb).</summary>
    public IReadOnlyList<TournamentConfig> Tournaments { get; init; } = [];

    public IReadOnlyList<FootballTeamConfig> Teams { get; init; } = [];
}

/// <summary>Ein Turnier-Fenster (z. B. WM) mit football-data-Code und Aktiv-Zeitraum (UTC).</summary>
public sealed class TournamentConfig
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public DateTimeOffset From { get; init; }
    public DateTimeOffset To { get; init; }
}

/// <summary>Ein zu beobachtender Verein (football-data.org-IDs/-Codes).</summary>
public sealed class FootballTeamConfig
{
    public string Name { get; init; } = string.Empty;
    public int TeamId { get; init; }

    /// <summary>Liga-Code für die Tabelle (z. B. <c>PD</c>, <c>BL1</c>).</summary>
    public string CompetitionCode { get; init; } = string.Empty;

    /// <summary>
    /// Wettbewerbe, aus denen Spiele geholt werden (z. B. <c>["PD","CL"]</c>). Leer =
    /// nur <see cref="CompetitionCode"/>. Nur Free-Tier-Wettbewerbe (kein Pokal).
    /// </summary>
    public IReadOnlyList<string> Competitions { get; init; } = [];
}
