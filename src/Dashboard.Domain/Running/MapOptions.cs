namespace Dashboard.Domain.Running;

/// <summary>
/// Standard-Kartenausschnitt der Heatmap (Sektion <c>Map</c> in appsettings). Die Gesamt-Ansicht
/// zentriert bewusst auf den Heimat-Standort statt über ALLE Läufe zu passen – sonst zerrt ein
/// einzelner weit entfernter Lauf (z. B. Urlaub) die Karte auf halb Europa. Default: Hamburg.
/// In der Domäne, weil die Web-Seite sie injiziert (Seiten hängen nicht an Infrastructure).
/// </summary>
public sealed class MapOptions
{
    public const string SectionName = "Map";

    public double Latitude { get; init; } = 53.5511;
    public double Longitude { get; init; } = 9.9937;
    public int Zoom { get; init; } = 12;
}
