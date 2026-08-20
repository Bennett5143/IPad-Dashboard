namespace Dashboard.Infrastructure.Crests;

/// <summary>Konfiguration des Bild-Proxys (<c>/crests</c>) — Wappen, Flaggen und Coin-Logos.</summary>
public sealed class CrestOptions
{
    public const string SectionName = "Crests";

    /// <summary>
    /// Allowlist der Upstream-Hosts, die geproxied werden dürfen. Der <c>/crests</c>-Endpoint nimmt
    /// eine beliebige URL entgegen; ohne diese Schranke wäre er ein offener Proxy (SSRF). Die
    /// vollständige Vorgabe steht in <c>appsettings.json</c> — Wappen- und Flaggen-Hosts ebenso wie
    /// die Coin-Logo-Hosts; leer schließt zu, dann ist kein Host erlaubt.
    /// <para>
    /// Kein Vorgabewert an der Property: der Configuration-Binder hängt an eine vorbelegte Collection
    /// an, statt sie zu ersetzen (siehe <c>FootballOptions.LeagueCodes</c>).
    /// </para>
    /// </summary>
    public IReadOnlyList<string> AllowedHosts { get; init; } = [];

    /// <summary>
    /// Ersatz-Wappen für Vereine, für die der Anbieter keines führt: Schlüssel ist der Vereinsname
    /// oder die football-data-Team-Id, Wert die Upstream-URL. Gehört in die gitignored
    /// <c>appsettings.Local.json</c> — privat, aber kein Geheimnis.
    /// <para>
    /// Kein Umgehen der Schranke: eine Override-URL durchläuft dieselbe <see cref="AllowedHosts"/>
    /// -Prüfung wie jede andere. Sie erspart nur das Warten auf den Anbieter, wenn ein neu
    /// aufgestiegener Verein sonst ohne Wappen dastünde.
    /// </para>
    /// </summary>
    public IReadOnlyDictionary<string, string> Overrides { get; init; } =
        new Dictionary<string, string>();

    /// <summary>Cache-Verzeichnis (relativ zum ContentRoot oder absolut).</summary>
    public string CacheDirectory { get; init; } = "crest-cache";

    public string UserAgent { get; init; } = "iPad-Kiosk-Dashboard (self-hosted)";
}
