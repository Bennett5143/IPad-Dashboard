namespace Dashboard.Domain.ValueObjects;

public sealed record RunningDetails(
    int DurationMinutes,
    decimal PaceMinPerKm
);
