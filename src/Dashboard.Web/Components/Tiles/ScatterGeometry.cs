namespace Dashboard.Web.Components.Tiles;

/// <summary>
/// Normiert Wertepaare auf SVG-Koordinaten für Scatter-Plots (y-Achse invertiert, da SVG
/// von oben zählt) — Pendant zu <see cref="SparklineGeometry"/>, rein und testbar.
/// </summary>
public static class ScatterGeometry
{
    public static IReadOnlyList<(double Cx, double Cy)> ToPoints(
        IReadOnlyList<(double X, double Y)> pairs, double width, double height, double pad = 4)
    {
        if (pairs.Count == 0)
        {
            return [];
        }

        var minX = pairs.Min(p => p.X);
        var maxX = pairs.Max(p => p.X);
        var minY = pairs.Min(p => p.Y);
        var maxY = pairs.Max(p => p.Y);
        var rangeX = maxX - minX;
        var rangeY = maxY - minY;

        return pairs
            .Select(p => (
                rangeX < 1e-9 ? width / 2 : pad + (p.X - minX) / rangeX * (width - 2 * pad),
                rangeY < 1e-9 ? height / 2 : height - pad - (p.Y - minY) / rangeY * (height - 2 * pad)))
            .ToList();
    }
}
