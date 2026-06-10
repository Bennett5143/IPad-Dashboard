namespace Dashboard.Domain.Running;

/// <summary>Aggregierte Kennzahlen für einen Zeitraum (FA-8.11).</summary>
public sealed record RunStats(
    int RunCount,
    double TotalDistanceKm,
    TimeSpan TotalMovingTime,
    double? AveragePaceMinPerKm);
