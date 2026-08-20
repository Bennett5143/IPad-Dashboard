using System.Globalization;
using System.Text;

namespace Dashboard.Web.Components.Tiles;

/// <summary>Erzeugt aus einer Wertereihe die SVG-Geometrie einer Sparkline (rein, testbar).</summary>
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

    /// <summary>
    /// SVG-<c>points</c>-String für die gefüllte Fläche unter der Sparkline: die Linie plus die
    /// beiden unteren Ecken (<c>0,height</c> … <c>width,height</c>), sodass sich ein geschlossenes
    /// Polygon zum Verlaufs-Fill ergibt. Leer, wenn die Linie leer ist (&lt; 2 vorhandene Werte).
    /// </summary>
    public static string ToAreaPolygonPoints(IReadOnlyList<double?> values, double width, double height, double pad = 2)
    {
        var line = ToPolylinePoints(values, width, height, pad);
        if (line.Length == 0)
        {
            return string.Empty;
        }

        return $"0,{Fmt(height)} {line} {Fmt(width)},{Fmt(height)}";
    }

    /// <summary>
    /// SVG-<c>d</c>-String für dieselbe Reihe als weiche Kurve: Catmull-Rom, umgerechnet in kubische
    /// Béziers. Die Kontrollpunkte werden auf den Wertebereich der beiden Endpunkte des Segments
    /// geklemmt, damit die Kurve nicht über die Daten hinausschießt — eine überschwingende
    /// Temperaturkurve zeichnete ein Maximum, das der Tag nie hatte.
    /// <para>
    /// Fehlende Werte (<c>null</c>) unterbrechen die Linie: jede zusammenhängende Folge wird ein
    /// eigener Teilpfad, statt über die Lücke hinweg gezogen zu werden.
    /// </para>
    /// Leer bei &lt; 2 zusammenhängenden Werten.
    /// </summary>
    public static string ToSmoothPath(IReadOnlyList<double?> values, double width, double height, double pad = 2)
    {
        if (values is null || values.Count < 2)
        {
            return string.Empty;
        }

        var present = values.Index().Where(x => x.Item.HasValue).ToList();
        if (present.Count < 2)
        {
            return string.Empty;
        }

        double min = present.Min(p => p.Item!.Value);
        double max = present.Max(p => p.Item!.Value);
        var range = max - min;
        var innerW = Math.Max(0, width - (2 * pad));
        var innerH = Math.Max(0, height - (2 * pad));
        var lastIndex = values.Count - 1;

        Point At(int index, double value) => new(
            pad + (innerW * index / lastIndex),
            range == 0 ? pad + (innerH / 2) : pad + (innerH * (1 - ((value - min) / range))));

        var sb = new StringBuilder();
        foreach (var run in Runs(present))
        {
            AppendRun(sb, run.Select(p => At(p.Index, p.Item!.Value)).ToList());
        }

        return sb.ToString();
    }

    /// <summary>Zusammenhängende Folgen vorhandener Werte — an einer Lücke beginnt eine neue.</summary>
    private static IEnumerable<List<(int Index, double? Item)>> Runs(
        IReadOnlyList<(int Index, double? Item)> present)
    {
        var run = new List<(int Index, double? Item)> { present[0] };
        for (var i = 1; i < present.Count; i++)
        {
            if (present[i].Index != present[i - 1].Index + 1)
            {
                yield return run;
                run = [];
            }

            run.Add(present[i]);
        }

        yield return run;
    }

    /// <summary>
    /// Ein Teilpfad: <c>M</c> auf den ersten Punkt, dann je Segment ein <c>C</c>. Die Tangente in
    /// einem Punkt zeigt entlang der Verbindung seiner Nachbarn (Catmull-Rom, α=0); an den Enden
    /// wiederholt sich der Randpunkt. Ein Lauf mit nur einem Punkt zeichnet nichts — eine Linie
    /// braucht zwei.
    /// </summary>
    private static void AppendRun(StringBuilder sb, IReadOnlyList<Point> points)
    {
        if (points.Count < 2)
        {
            return;
        }

        if (sb.Length > 0)
        {
            sb.Append(' ');
        }

        sb.Append('M').Append(Fmt(points[0].X)).Append(',').Append(Fmt(points[0].Y));

        for (var i = 0; i < points.Count - 1; i++)
        {
            var p0 = points[Math.Max(i - 1, 0)];
            var p1 = points[i];
            var p2 = points[i + 1];
            var p3 = points[Math.Min(i + 2, points.Count - 1)];

            var c1 = Clamp(new Point(p1.X + ((p2.X - p0.X) / 6), p1.Y + ((p2.Y - p0.Y) / 6)), p1, p2);
            var c2 = Clamp(new Point(p2.X - ((p3.X - p1.X) / 6), p2.Y - ((p3.Y - p1.Y) / 6)), p1, p2);

            sb.Append(" C").Append(Fmt(c1.X)).Append(',').Append(Fmt(c1.Y))
              .Append(' ').Append(Fmt(c2.X)).Append(',').Append(Fmt(c2.Y))
              .Append(' ').Append(Fmt(p2.X)).Append(',').Append(Fmt(p2.Y));
        }
    }

    /// <summary>
    /// Hält einen Kontrollpunkt im Rechteck der beiden Segment-Endpunkte. Eine kubische Bézier
    /// liegt in der konvexen Hülle ihrer vier Punkte — damit kann die Kurve keinen Wert zeigen,
    /// der zwischen den beiden Datenpunkten nicht vorkommt.
    /// </summary>
    private static Point Clamp(Point control, Point a, Point b) => new(
        Math.Clamp(control.X, Math.Min(a.X, b.X), Math.Max(a.X, b.X)),
        Math.Clamp(control.Y, Math.Min(a.Y, b.Y), Math.Max(a.Y, b.Y)));

    private readonly record struct Point(double X, double Y);

    private static string Fmt(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
