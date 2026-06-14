namespace Dashboard.Web.Components.Metrics;

/// <summary>
/// Sachliche Erklärung einer Metrik fürs Popup (FA-10.08). Bewusst unpersönlich formuliert
/// (kein „du/dein"): <see cref="Summary"/> = was man sieht, <see cref="Basis"/> = Datengrundlage
/// und Berechnung, <see cref="Use"/> = wofür die Zahl taugt. <see cref="XAxis"/>/<see cref="YAxis"/>
/// nur bei Diagrammen mit Achsen.
/// </summary>
public sealed record MetricExplanation(
    string Title,
    string Summary,
    string Basis,
    string Use,
    string? XAxis = null,
    string? YAxis = null);
