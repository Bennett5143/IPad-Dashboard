namespace Dashboard.Web.Components.Metrics;

/// <summary>
/// Erklärungstext einer Metrik fürs Info-Popup (FA-10.08). <see cref="Summary"/> = was wird
/// gezeigt, <see cref="HowToRead"/> = wie interpretieren. <see cref="XAxis"/>/<see cref="YAxis"/>
/// (Achsenbedeutung, FA-10.09) und <see cref="Method"/> (Berechnung/Quelle) sind optional und
/// nur dort gesetzt, wo sie etwas hinzufügen – etwa bei Diagrammen.
/// </summary>
public sealed record MetricExplanation(
    string Title,
    string Summary,
    string HowToRead,
    string? XAxis = null,
    string? YAxis = null,
    string? Method = null);
