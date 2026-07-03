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
}
