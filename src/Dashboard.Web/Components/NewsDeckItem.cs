namespace Dashboard.Web.Components;

/// <summary>
/// Eine Meldung in der Blätter-Ansicht, bereits anzeigefertig. Beide Recherche-Seiten
/// (Fußball-Nachrichten, Marktbericht) reichen dieselbe Form herein — die Kopfzeile trägt nur
/// unterschiedliche Felder.
/// </summary>
/// <param name="HeaderParts">
/// Kontext der Meldung, in fester Reihenfolge; leere Angaben lässt der Aufrufer weg, damit kein
/// „·" ins Leere zeigt.
/// </param>
/// <param name="Badge">Die Bewertung des Recherche-Tools, unverändert. <c>null</c> = keine.</param>
/// <param name="Source">Quellenname als reiner Text — nie ein Link (LAN-Kiosk ohne Internet).</param>
public sealed record NewsDeckItem(
    IReadOnlyList<string> HeaderParts,
    NewsDeckBadge? Badge,
    string Headline,
    string Summary,
    string? Source);

/// <summary>Bewertungs-Badge: Beschriftung plus die CSS-Klasse, die seine Farbe trägt.</summary>
public sealed record NewsDeckBadge(string Label, string CssClass);
