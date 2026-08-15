namespace Dashboard.Infrastructure.Crests;

/// <summary>Konfiguration des Wappen-/Flaggen-Proxys (<c>/crests</c>).</summary>
public sealed class CrestOptions
{
    public const string SectionName = "Crests";

    /// <summary>
    /// Allowlist der Upstream-Hosts, die geproxied werden dürfen. Der <c>/crests</c>-Endpoint nimmt
    /// eine beliebige URL entgegen; ohne diese Schranke wäre er ein offener Proxy (SSRF). Die Vorgabe
    /// (football-data.org-Wappen, über die Nationalteams auch ihre Flaggen liefern) steht in
    /// <c>appsettings.json</c>; leer schließt zu — dann ist kein Host erlaubt.
    /// <para>
    /// Kein Vorgabewert an der Property: der Configuration-Binder hängt an eine vorbelegte Collection
    /// an, statt sie zu ersetzen (siehe <c>FootballOptions.LeagueCodes</c>).
    /// </para>
    /// </summary>
    public IReadOnlyList<string> AllowedHosts { get; init; } = [];

    /// <summary>Cache-Verzeichnis (relativ zum ContentRoot oder absolut).</summary>
    public string CacheDirectory { get; init; } = "crest-cache";

    public string UserAgent { get; init; } = "iPad-Kiosk-Dashboard (self-hosted)";
}
