namespace Dashboard.Infrastructure.Hvv;

/// <summary>
/// Konfiguration des HVV-Abfahrtsmonitors. Haltestellen ausschließlich hier (FA-6.06).
/// <see cref="HvvStationConfig.MasterId"/> wird einmalig über <c>geofox/checkName</c> bzw. den
/// Webseiten-Generator ermittelt. Gefiltert wird über Linie + Richtungstext (siehe <see cref="HvvLineFilter"/>).
/// </summary>
public sealed class HvvOptions
{
    public const string SectionName = "Hvv";

    public string Endpoint { get; init; } = "https://www.hvv.de/geofox/departureList";

    /// <summary>Realistischer User-Agent als Netiquette gegenüber dem inoffiziellen Endpoint.</summary>
    public string UserAgent { get; init; } = "ipad-kiosk-dashboard/1.0";

    /// <summary>API-Version im Request-Body. Hardcoded laut Recherche, bei Bump bricht meist etwas.</summary>
    public int Version { get; init; } = 47;

    /// <summary>Poll-Intervall. Wird zur Sicherheit auf min. 60 s angehoben (FA-6.04: max. 1 Req/min pro Haltestelle).</summary>
    public int PollIntervalSeconds { get; init; } = 60;

    /// <summary>Maximale Anzahl angezeigter Abfahrten pro Haltestelle (nach Filterung).</summary>
    public int MaxDepartures { get; init; } = 6;

    public IReadOnlyList<HvvStationConfig> Stations { get; init; } = [];
}

public sealed class HvvStationConfig
{
    public string Name { get; init; } = string.Empty;

    /// <summary>HVV-interne Stations-Id, Format <c>Master:xxxxx</c>.</summary>
    public string MasterId { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    /// <summary>Welche Linien/Richtungen angezeigt werden. Leer = alle Abfahrten der Haltestelle.</summary>
    public IReadOnlyList<HvvLineFilter> Lines { get; init; } = [];

    public int MaxList { get; init; } = 40;
    public int MaxTimeOffsetMinutes { get; init; } = 120;
}

/// <summary>
/// Filtert Abfahrten nach Linie und Fahrtrichtung. <see cref="Line"/> ist der Liniennname
/// (z. B. „42", „S3"), <see cref="Direction"/> ein Teilstring des Richtungstexts der API
/// (z. B. „Hafen Harburg"); beides wird case-insensitiv geprüft. Leere <see cref="Direction"/>
/// = alle Richtungen dieser Linie.
/// </summary>
public sealed class HvvLineFilter
{
    public string Line { get; init; } = string.Empty;
    public string Direction { get; init; } = string.Empty;
}
