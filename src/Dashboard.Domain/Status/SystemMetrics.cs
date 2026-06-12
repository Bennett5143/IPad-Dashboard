namespace Dashboard.Domain.Status;

/// <summary>
/// Host-Metriken für die Status-Seite (FA-11.02). Einzelne Felder fehlen, wenn die
/// Plattform sie nicht liefert.
/// </summary>
public sealed record SystemMetrics(
    double? CpuTemperatureCelsius,
    double? MemoryUsedMegabytes,
    double? MemoryTotalMegabytes,
    double? LoadAverage1Min);

/// <summary>
/// Liefert Host-Metriken; <c>null</c>, wenn die Plattform keine bereitstellt
/// (umgesetzt nur für Linux — den Raspberry Pi; auf dem Mac bewusst leer).
/// </summary>
public interface ISystemMetricsProvider
{
    Task<SystemMetrics?> GetAsync(CancellationToken ct = default);
}
