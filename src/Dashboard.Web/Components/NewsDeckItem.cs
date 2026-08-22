namespace Dashboard.Web.Components;

/// <summary>
/// Eine Meldung in der Blätter-Ansicht, bereits anzeigefertig. Beide Recherche-Seiten
/// (Fußball-Nachrichten, Marktbericht) reichen dieselbe Form herein — sie füllen die Felder nur
/// unterschiedlich.
/// <para>
/// Die Felder sind benannt statt gelistet: die Karte verteilt den Kontext auf eine invertierte
/// Kopfleiste (links/rechts) und eine dreizellige Meta-Zeile, und eine flache Liste ließe sich
/// nur nach Position aufteilen — also raten, was ein Element bedeutet. Beide Aufrufer wissen es
/// ohnehin, sie sollen es sagen.
/// </para>
/// </summary>
/// <param name="Eyebrow">Links in der Kopfleiste: der größere Zusammenhang (Wettbewerb, Kategorie).</param>
/// <param name="EyebrowRight">Rechts in der Kopfleiste, z. B. der Verein oder die Übereinstimmung.</param>
/// <param name="Category">Erste Zelle der Meta-Zeile.</param>
/// <param name="Date">Zweite Zelle der Meta-Zeile.</param>
/// <param name="Badge">Die Bewertung des Recherche-Tools, unverändert. <c>null</c> = keine.</param>
/// <param name="Headline">Die Schlagzeile.</param>
/// <param name="Summary">Der Fließtext.</param>
/// <param name="Source">Quellenname als reiner Text — nie ein Link (LAN-Kiosk ohne Internet).</param>
public sealed record NewsDeckItem(
    string Eyebrow,
    string? EyebrowRight,
    string? Category,
    string? Date,
    NewsDeckBadge? Badge,
    string Headline,
    string Summary,
    string? Source);

/// <summary>Bewertungs-Badge: Beschriftung plus die CSS-Klasse, die seine Form trägt.</summary>
public sealed record NewsDeckBadge(string Label, string CssClass);
