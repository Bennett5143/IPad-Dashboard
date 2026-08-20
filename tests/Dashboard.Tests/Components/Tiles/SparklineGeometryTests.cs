using System.Globalization;

namespace Dashboard.Tests.Components.Tiles;

public class SparklineGeometryTests
{
    [Fact]
    public void ReturnsEmpty_ForFewerThanTwoPresentValues()
    {
        Assert.Equal(string.Empty, SparklineGeometry.ToPolylinePoints([5d], 120, 32));
        Assert.Equal(string.Empty, SparklineGeometry.ToPolylinePoints([null, null], 120, 32));
        Assert.Equal(string.Empty, SparklineGeometry.ToPolylinePoints([null, 5d], 120, 32));
    }

    [Fact]
    public void MapsMinToBottom_AndMaxToTop()
    {
        // pad 2 → innerW 116, innerH 28; min(0) unten (y=30), max(10) oben (y=2)
        Assert.Equal("2,30 118,2", SparklineGeometry.ToPolylinePoints([0d, 10d], 120, 32, pad: 2));
    }

    [Fact]
    public void FlatSeries_DrawsMidline()
    {
        Assert.Equal("2,16 118,16", SparklineGeometry.ToPolylinePoints([5d, 5d], 120, 32, pad: 2));
    }

    [Fact]
    public void SkipsGaps_ButKeepsIndexSpacing()
    {
        // mittlerer Wert fehlt → Punkte bei Index 0 und 2
        Assert.Equal("2,30 118,2", SparklineGeometry.ToPolylinePoints([0d, null, 10d], 120, 32, pad: 2));
    }

    [Fact]
    public void Area_WrapsLine_WithBaseCorners()
    {
        // Linie "2,30 118,2" + untere Ecken 0,32 und 120,32 → geschlossenes Polygon
        Assert.Equal("0,32 2,30 118,2 120,32", SparklineGeometry.ToAreaPolygonPoints([0d, 10d], 120, 32, pad: 2));
    }

    [Fact]
    public void Area_ReturnsEmpty_WhenLineEmpty()
    {
        Assert.Equal(string.Empty, SparklineGeometry.ToAreaPolygonPoints([5d], 120, 32));
        Assert.Equal(string.Empty, SparklineGeometry.ToAreaPolygonPoints([null, 5d], 120, 32));
    }

    // ---- Weiche Kurve (Catmull-Rom → kubische Béziers, geklemmt) --------------------------

    [Fact]
    public void Smooth_ReturnsEmpty_ForFewerThanTwoPresentValues()
    {
        Assert.Equal(string.Empty, SparklineGeometry.ToSmoothPath([5d], 120, 32));
        Assert.Equal(string.Empty, SparklineGeometry.ToSmoothPath([null, null], 120, 32));
        Assert.Equal(string.Empty, SparklineGeometry.ToSmoothPath([null, 5d], 120, 32));
    }

    [Fact]
    public void Smooth_DrawsCurveSegments_NotStraightLines()
    {
        var path = SparklineGeometry.ToSmoothPath([0d, 10d, 4d, 8d], 120, 32);

        Assert.StartsWith("M", path, StringComparison.Ordinal);
        Assert.Equal(3, path.Count(c => c == 'C'));   // drei Segmente, je eine kubische Bézier
        Assert.DoesNotContain('L', path);
    }

    [Fact]
    public void Smooth_KeepsEndpointsOnTheData()
    {
        // Wie bei der Polyline: min unten (y=30), max oben (y=2) – die Glättung verschiebt die
        // Datenpunkte selbst nicht, sie liegen weiter auf der Kurve.
        var path = SparklineGeometry.ToSmoothPath([0d, 10d, 5d], 120, 32, pad: 2);

        Assert.StartsWith("M2,30 ", path, StringComparison.Ordinal);
        Assert.EndsWith(" 118,16", path, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(new[] { 0d, 2d, 10d, 2d, 0d })]      // Spitze in der Mitte
    [InlineData(new[] { 10d, 8d, 0d, 8d, 10d })]     // Senke in der Mitte
    [InlineData(new[] { 0d, 9d, 1d, 10d, 2d })]      // Zickzack
    public void Smooth_NeverOvershootsTheData(double[] values)
    {
        // Eine überschwingende Kurve zeichnete ein Maximum, das die Reihe nie hatte. Jede kubische
        // Bézier liegt in der konvexen Hülle ihrer vier Punkte — es genügt also zu prüfen, dass
        // kein Kontrollpunkt aus dem Wertebereich läuft.
        var path = SparklineGeometry.ToSmoothPath(values.Select(v => (double?)v).ToList(), 120, 32, pad: 2);

        var ys = path.Split(new[] { ' ', 'M', 'C' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => double.Parse(pair.Split(',')[1], CultureInfo.InvariantCulture));

        Assert.All(ys, y => Assert.InRange(y, 2 - Tolerance, 30 + Tolerance));
    }

    [Fact]
    public void Smooth_BreaksTheLineAtAGap()
    {
        // Eine Lücke wird nicht überbrückt: jede zusammenhängende Folge ist ein eigener Teilpfad.
        var path = SparklineGeometry.ToSmoothPath([0d, 5d, null, 8d, 10d], 120, 32);

        Assert.Equal(2, path.Count(c => c == 'M'));
    }

    [Fact]
    public void Smooth_DropsARunTooShortToDraw()
    {
        // Ein einzelner Wert zwischen zwei Lücken ist keine Linie – er wird übergangen, nicht
        // an die Nachbarn angeschlossen.
        var path = SparklineGeometry.ToSmoothPath([0d, 5d, null, 8d, null, 2d, 4d], 120, 32);

        Assert.Equal(2, path.Count(c => c == 'M'));
    }

    [Fact]
    public void SmoothArea_ClosesEachRunToTheBaseline()
    {
        // Die Fläche folgt der Linie, auch in ihren Lücken: zwei Folgen, zwei geschlossene
        // Teilflächen. Eine durchgehende Fläche behauptete, was die Linie darüber verschweigt.
        var area = SparklineGeometry.ToSmoothAreaPath([0d, 5d, null, 8d, 10d], 120, 32);

        Assert.Equal(2, area.Count(c => c == 'M'));
        Assert.Equal(2, area.Count(c => c == 'Z'));
    }

    [Fact]
    public void SmoothArea_ReturnsEmpty_WhenLineEmpty()
    {
        Assert.Equal(string.Empty, SparklineGeometry.ToSmoothAreaPath([5d], 120, 32));
        Assert.Equal(string.Empty, SparklineGeometry.ToSmoothAreaPath([null, 5d], 120, 32));
    }

    /// <summary>Die Pfadwerte sind auf zwei Nachkommastellen gerundet.</summary>
    private const double Tolerance = 0.01;
}
