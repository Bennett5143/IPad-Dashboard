namespace Dashboard.Domain.Running;

/// <summary>
/// Dekodiert Googles "Encoded Polyline Algorithm Format" (Präzision 5), wie es Strava in
/// <c>map.summary_polyline</c> liefert, in eine Liste von <see cref="GeoPoint"/>.
/// </summary>
public static class PolylineDecoder
{
    public static IReadOnlyList<GeoPoint> Decode(string? encoded)
    {
        var points = new List<GeoPoint>();
        if (string.IsNullOrEmpty(encoded))
        {
            return points;
        }

        int index = 0, lat = 0, lng = 0;
        while (index < encoded.Length)
        {
            lat += DecodeValue(encoded, ref index);
            lng += DecodeValue(encoded, ref index);
            points.Add(new GeoPoint(lat / 1e5, lng / 1e5));
        }

        return points;
    }

    private static int DecodeValue(string encoded, ref int index)
    {
        int result = 0, shift = 0, current;
        do
        {
            current = encoded[index++] - 63;
            result |= (current & 0x1f) << shift;
            shift += 5;
        }
        while (current >= 0x20);

        return (result & 1) != 0 ? ~(result >> 1) : result >> 1;
    }
}
