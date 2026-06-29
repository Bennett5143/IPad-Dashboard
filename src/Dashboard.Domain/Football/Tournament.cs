namespace Dashboard.Domain.Football;

/// <summary>
/// Ein zeitlich begrenztes Turnier (EM/WM). Wird nur abgerufen/angezeigt, solange das Fenster aktiv ist
/// (<see cref="IsActive"/>) – außerhalb fallen null Idle-Calls an. Fenster werden manuell konfiguriert.
/// </summary>
public sealed record Tournament(
    string Code,
    string Name,
    DateTimeOffset From,
    DateTimeOffset To)
{
    public bool IsActive(DateTimeOffset now) => now >= From && now <= To;
}
