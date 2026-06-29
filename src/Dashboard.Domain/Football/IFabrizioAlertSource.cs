namespace Dashboard.Domain.Football;

/// <summary>
/// Liefert die jüngste Fabrizio-Romano-Meldung für die Fußball-Summary und die <c>/football</c>-Seite.
/// Bewusst entkoppelt von <c>Dashboard.Domain.Social</c>: Fußball kennt die Social-Quelle nicht, nur
/// diesen Port. 15.6 registriert <see cref="NullFabrizioAlertSource"/>; 15.8 tauscht die Implementierung.
/// </summary>
public interface IFabrizioAlertSource
{
    /// <summary>Die zuletzt bekannte Meldung, oder <c>null</c>, wenn keine vorliegt.</summary>
    FabrizioAlert? Latest { get; }
}
