namespace Dashboard.Domain.Football;

/// <summary>
/// Eine Transfer-/Verletzungs-Meldung im Stil von Fabrizio Romano (FA-15.8). In 15.6 nur als Daten-
/// und Port-Vertrag vorhanden; befüllt wird sie erst vom Social-Slice (15.8). Medien werden – wegen
/// des Offline-iPads – nie als Browser-Bild geladen, sondern höchstens als <see cref="Url"/>-Textlink.
/// </summary>
public sealed record FabrizioAlert(
    string Headline,
    DateTimeOffset PublishedUtc,
    string? Url = null);
