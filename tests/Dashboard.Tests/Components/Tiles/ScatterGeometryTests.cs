namespace Dashboard.Tests.Components.Tiles;

public class ScatterGeometryTests
{
    [Fact]
    public void ToPoints_NormalizesIntoPaddedBox_AndInvertsY()
    {
        var points = ScatterGeometry.ToPoints(
            [(0, 0), (10, 100), (5, 50)], width: 120, height: 80, pad: 4);

        Assert.Equal(3, points.Count);
        Assert.Equal((4, 76), points[0]);     // min/min → links unten (SVG-y invertiert)
        Assert.Equal((116, 4), points[1]);    // max/max → rechts oben
        Assert.Equal(60, points[2].Cx, 1);    // Mitte
        Assert.Equal(40, points[2].Cy, 1);
    }

    [Fact]
    public void ToPoints_CentersDegenerateAxes()
    {
        var points = ScatterGeometry.ToPoints([(5, 1), (5, 2)], 120, 80);

        Assert.All(points, p => Assert.Equal(60, p.Cx)); // keine X-Varianz → mittig
    }

    [Fact]
    public void ToPoints_EmptyStaysEmpty()
    {
        Assert.Empty(ScatterGeometry.ToPoints([], 120, 80));
    }
}
