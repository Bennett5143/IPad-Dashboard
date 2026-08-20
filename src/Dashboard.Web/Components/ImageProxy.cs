namespace Dashboard.Web.Components;

/// <summary>
/// Verweis auf ein externes Bild über den lokalen Proxy. Das iPad lädt so nie direkt aus dem
/// Internet — im Anzeigekontext gibt es keins.
/// <para>
/// Der Pfad heißt weiter <c>/crests</c>, obwohl er längst nicht nur Wappen trägt: Endpoint,
/// Options-Abschnitt und Cache-Verzeichnis sind im Compose-Setup des Pi und dessen
/// <c>appsettings.Local.json</c> verdrahtet. Ein ehrlicherer Name kostete eine Config-Migration
/// auf dem Host und brächte nur einen besseren Bezeichner.
/// </para>
/// </summary>
public static class ImageProxy
{
    /// <summary><c>null</c>, wenn keine Upstream-URL vorliegt — der Aufrufer zeigt dann seinen Ersatz.</summary>
    public static string? Src(string? url) =>
        string.IsNullOrWhiteSpace(url) ? null : $"/crests?u={Uri.EscapeDataString(url)}";
}
