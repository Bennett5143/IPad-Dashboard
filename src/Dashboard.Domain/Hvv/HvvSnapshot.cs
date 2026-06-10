namespace Dashboard.Domain.Hvv;

/// <summary>UI-fertige Sicht auf alle konfigurierten Haltestellen. Wird in <see cref="HvvState"/> gehalten.</summary>
public sealed record HvvSnapshot(
    IReadOnlyList<StationBoard> Stations,
    DateTimeOffset RetrievedAtUtc);
