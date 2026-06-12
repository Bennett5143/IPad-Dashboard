using System.Globalization;

namespace Dashboard.Infrastructure.Status;

/// <summary>
/// Reines Parsen der Linux-Pseudodateien für Host-Metriken (FA-11.02) — getrennt vom
/// Datei-Zugriff, damit die Formate offline testbar sind.
/// </summary>
public static class SystemMetricsParser
{
    /// <summary><c>/sys/class/thermal/thermal_zone0/temp</c>: Milligrad, z. B. „48312" → 48,3 °C.</summary>
    public static double? ParseCpuTemperature(string? raw) =>
        double.TryParse(raw?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var milli)
            ? milli / 1000.0
            : null;

    /// <summary>
    /// <c>/proc/meminfo</c>: belegter Speicher = <c>MemTotal − MemAvailable</c> (beide in kB),
    /// als MB. <c>MemAvailable</c> statt <c>MemFree</c>, weil Page-Cache rückforderbar ist.
    /// </summary>
    public static (double? UsedMb, double? TotalMb) ParseMemInfo(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return (null, null);
        }

        double? totalKb = null, availableKb = null;
        foreach (var line in content.Split('\n'))
        {
            if (line.StartsWith("MemTotal:", StringComparison.Ordinal))
            {
                totalKb = ParseKbLine(line);
            }
            else if (line.StartsWith("MemAvailable:", StringComparison.Ordinal))
            {
                availableKb = ParseKbLine(line);
            }
        }

        if (totalKb is not { } total || availableKb is not { } available)
        {
            return (null, totalKb / 1024.0);
        }

        return ((total - available) / 1024.0, total / 1024.0);
    }

    /// <summary><c>/proc/loadavg</c>: erster Wert ist der 1-Minuten-Load, z. B. „0.52 0.58 …".</summary>
    public static double? ParseLoadAverage(string? content)
    {
        var first = content?.TrimStart().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return double.TryParse(first, NumberStyles.Float, CultureInfo.InvariantCulture, out var load)
            ? load
            : null;
    }

    private static double? ParseKbLine(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var kb)
            ? kb
            : null;
    }
}
