using System.Globalization;
using System.Text;

namespace Dashboard.Web.Components.Tiles;

/// <summary>Erzeugt aus einer Wertereihe die SVG-Polyline-Punkte einer Sparkline (reine Geometrie, testbar).</summary>
public static class SparklineGeometry
{
    /// <summary>
    /// SVG-<c>points</c>-String für eine Sparkline. Fehlende Werte (<c>null</c>) werden übersprungen,
    /// die x-Position richtet sich nach dem Index (Lücken bleiben sichtbar). Leer bei &lt; 2 Werten.
    /// </summary>
    public static string ToPolylinePoints(IReadOnlyList<double?> values, double width, double height, double pad = 2)
    {
        if (values is null || values.Count < 2)
        {
            return string.Empty;
        }

        var present = new List<(double Value, int Index)>();
        for (var i = 0; i < values.Count; i++)
        {
            if (values[i] is { } v)
            {
                present.Add((v, i));
            }
        }

        if (present.Count < 2)
        {
            return string.Empty;
        }

        double min = present.Min(p => p.Value);
        double max = present.Max(p => p.Value);
        var range = max - min;
        var innerW = Math.Max(0, width - (2 * pad));
        var innerH = Math.Max(0, height - (2 * pad));
        var lastIndex = values.Count - 1;

        var sb = new StringBuilder();
        foreach (var (value, index) in present)
        {
            var x = pad + (innerW * index / lastIndex);
            var y = range == 0 ? pad + (innerH / 2) : pad + (innerH * (1 - ((value - min) / range)));

            if (sb.Length > 0)
            {
                sb.Append(' ');
            }

            sb.Append(Fmt(x)).Append(',').Append(Fmt(y));
        }

        return sb.ToString();
    }

    private static string Fmt(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
