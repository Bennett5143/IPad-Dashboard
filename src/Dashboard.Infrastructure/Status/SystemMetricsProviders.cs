using Dashboard.Domain.Status;

namespace Dashboard.Infrastructure.Status;

/// <summary>
/// Host-Metriken von den Linux-Pseudodateien (Raspberry Pi). Fehlende/ungelesbare Dateien
/// führen zu fehlenden Feldern, nie zu Fehlern — die Status-Seite degradiert sauber.
/// </summary>
public sealed class LinuxSystemMetricsProvider : ISystemMetricsProvider
{
    public async Task<SystemMetrics?> GetAsync(CancellationToken ct = default)
    {
        var temperature = SystemMetricsParser.ParseCpuTemperature(
            await ReadOrNullAsync("/sys/class/thermal/thermal_zone0/temp", ct));
        var (usedMb, totalMb) = SystemMetricsParser.ParseMemInfo(
            await ReadOrNullAsync("/proc/meminfo", ct));
        var load = SystemMetricsParser.ParseLoadAverage(
            await ReadOrNullAsync("/proc/loadavg", ct));

        return temperature is null && usedMb is null && load is null
            ? null
            : new SystemMetrics(temperature, usedMb, totalMb, load);
    }

    private static async Task<string?> ReadOrNullAsync(string path, CancellationToken ct)
    {
        try
        {
            return await File.ReadAllTextAsync(path, ct);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}

/// <summary>Plattformen ohne Host-Metriken (Entwicklung auf dem Mac): bewusst leer.</summary>
public sealed class NullSystemMetricsProvider : ISystemMetricsProvider
{
    public Task<SystemMetrics?> GetAsync(CancellationToken ct = default) =>
        Task.FromResult<SystemMetrics?>(null);
}
