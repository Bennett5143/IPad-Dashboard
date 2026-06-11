namespace Dashboard.Domain.Football;

/// <summary>UI-fertige Sicht auf alle konfigurierten Vereine. Wird in <see cref="FootballState"/> gehalten.</summary>
public sealed record FootballSnapshot(
    IReadOnlyList<FootballTeamSnapshot> Teams,
    DateTimeOffset RetrievedAtUtc) : Common.ISnapshot;
