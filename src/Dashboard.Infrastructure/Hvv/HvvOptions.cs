namespace Dashboard.Infrastructure.Hvv;

/// <summary>
/// Konfiguration des HVV-Abfahrtsmonitors. Haltestellen ausschließlich hier (FA-6.06).
/// IDs werden einmalig über den Webseiten-Generator ermittelt – siehe HVV-Recherche.
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

    /// <summary>Maximale Anzahl angezeigter Abfahrten pro Haltestelle.</summary>
    public int MaxDepartures { get; init; } = 6;

    public IReadOnlyList<HvvStationConfig> Stations { get; init; } = [];
}

public sealed class HvvStationConfig
{
    public string Name { get; init; } = string.Empty;
    public string MasterId { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public IReadOnlyList<HvvFilterConfig> Filters { get; init; } = [];
    public int MaxList { get; init; } = 20;
    public int MaxTimeOffsetMinutes { get; init; } = 120;
}

/// <summary>Linien-Filter: eine Linie (<see cref="ServiceId"/>) Richtung einer Folgehaltestelle (<see cref="TargetStationId"/>).</summary>
public sealed class HvvFilterConfig
{
    public string ServiceId { get; init; } = string.Empty;
    public string TargetStationId { get; init; } = string.Empty;
}
