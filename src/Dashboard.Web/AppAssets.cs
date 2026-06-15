namespace Dashboard.Web;

/// <summary>
/// Versions-Token für clientseitig importierte Assets (JS-Module). Wird pro Prozessstart einmal
/// erzeugt, sodass nach jedem App-Neustart (<c>dotnet run</c>) der Browser garantiert die aktuelle
/// Datei lädt – ohne manuelles Cache-Bust-Hochzählen und ohne erzwungenes Hard-Reload.
/// </summary>
public static class AppAssets
{
    public static readonly string Version = Guid.NewGuid().ToString("N")[..8];
}
