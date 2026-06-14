namespace Dashboard.Infrastructure.Tiles;

/// <summary>
/// Konfiguration des Karten-Kachel-Proxys (Sektion <see cref="SectionName"/> in
/// <c>appsettings.json</c>). Sinn: Das (offline gehaltene) Kiosk-iPad holt Kacheln nur vom
/// LAN-Server, der sie online lädt und lokal cached.
/// </summary>
public sealed class TileOptions
{
    public const string SectionName = "Tiles";

    /// <summary>Upstream-Kachel-URL mit Platzhaltern <c>{z}/{x}/{y}</c>. Default: CARTO „dark", passend zum HUD.</summary>
    public string UrlTemplate { get; init; } = "https://a.basemaps.cartocdn.com/dark_nolabels/{z}/{x}/{y}.png";

    /// <summary>Cache-Verzeichnis (relativ zum Content-Root oder absolut).</summary>
    public string CacheDirectory { get; init; } = "tile-cache";

    /// <summary>User-Agent für den Upstream-Abruf (von OSM/CARTO erwartet).</summary>
    public string UserAgent { get; init; } = "iPad-Kiosk-Dashboard (self-hosted)";

    /// <summary>Maximal akzeptierte Zoomstufe (Schutz gegen Unsinns-Anfragen).</summary>
    public int MaxZoom { get; init; } = 20;
}
