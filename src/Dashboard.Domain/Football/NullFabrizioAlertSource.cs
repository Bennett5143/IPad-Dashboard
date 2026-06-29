namespace Dashboard.Domain.Football;

/// <summary>
/// Inerte Standard-Implementierung von <see cref="IFabrizioAlertSource"/> (Null-Object): liefert nie
/// eine Meldung. Bis 15.8 (echter Social-Feed) registriert das die Fußball-Verkabelung, damit die
/// Summary-Kachel den Badge-Slot bereits enthält, ohne dass etwas erscheint.
/// </summary>
public sealed class NullFabrizioAlertSource : IFabrizioAlertSource
{
    public FabrizioAlert? Latest => null;
}
