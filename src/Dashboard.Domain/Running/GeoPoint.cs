namespace Dashboard.Domain.Running;

/// <summary>Ein WGS84-Koordinatenpunkt (Breitengrad/Längengrad). Anbieterneutral, DB-frei.</summary>
public readonly record struct GeoPoint(double Latitude, double Longitude);
