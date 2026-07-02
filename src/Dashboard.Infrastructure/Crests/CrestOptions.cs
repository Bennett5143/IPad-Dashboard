namespace Dashboard.Infrastructure.Crests;

/// <summary>Konfiguration des Wappen-/Flaggen-Proxys (<c>/crests</c>).</summary>
public sealed class CrestOptions
{
    public const string SectionName = "Crests";

    /// <summary>
    /// Allowlist der Upstream-Hosts, die geproxied werden dürfen. Der <c>/crests</c>-Endpoint nimmt
    /// eine beliebige URL entgegen; ohne diese Schranke wäre er ein offener Proxy (SSRF). Default =
    /// football-data.org-Wappen (Nationalteams liefern hierüber ihre Flaggen).
    /// </summary>
    public IReadOnlyList<string> AllowedHosts { get; init; } = ["crests.football-data.org"];

    /// <summary>Cache-Verzeichnis (relativ zum ContentRoot oder absolut).</summary>
    public string CacheDirectory { get; init; } = "crest-cache";

    public string UserAgent { get; init; } = "iPad-Kiosk-Dashboard (self-hosted)";
}
