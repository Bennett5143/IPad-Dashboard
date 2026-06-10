namespace Dashboard.Tests.Running;

public class PolylineDecoderTests
{
    [Fact]
    public void Decode_CanonicalGoogleExample()
    {
        // Referenzbeispiel aus Googles Polyline-Spezifikation.
        var points = PolylineDecoder.Decode("_p~iF~ps|U_ulLnnqC_mqNvxq`@");

        Assert.Equal(3, points.Count);
        Assert.Equal(38.5, points[0].Latitude, 3);
        Assert.Equal(-120.2, points[0].Longitude, 3);
        Assert.Equal(40.7, points[1].Latitude, 3);
        Assert.Equal(-120.95, points[1].Longitude, 3);
        Assert.Equal(43.252, points[2].Latitude, 3);
        Assert.Equal(-126.453, points[2].Longitude, 3);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Decode_EmptyInput_ReturnsNoPoints(string? encoded)
    {
        Assert.Empty(PolylineDecoder.Decode(encoded));
    }
}
