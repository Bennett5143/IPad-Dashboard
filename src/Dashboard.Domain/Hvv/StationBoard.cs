namespace Dashboard.Domain.Hvv;

/// <summary>
/// Abfahrtstafel einer Haltestelle. <see cref="Available"/> ist <c>false</c>, wenn der Endpoint
/// für diese Station keine verwertbaren Daten lieferte (freundlicher „nicht verfügbar"-Zustand, FA-6.05).
/// </summary>
public sealed record StationBoard(
    string StationName,
    bool Available,
    IReadOnlyList<Departure> Departures);
