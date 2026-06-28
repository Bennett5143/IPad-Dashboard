namespace Dashboard.Domain.Hvv;

/// <summary>
/// Wählt aus einer bereits chronologisch sortierten Abfahrtsliste die nächsten
/// <c>perGroup</c> Abfahrten je Gruppe und behält dabei die Eingabe-Reihenfolge
/// (früheste zuerst, gruppenübergreifend verschränkt). <c>groupKey</c> liefert
/// <c>null</c>, um eine Abfahrt zu verwerfen (z. B. eine nicht konfigurierte Linie). Rein, ohne
/// Zeit-/IO-Abhängigkeit → einfach testbar. Dient z. B. „nächster Bus 42 UND nächster 143/443".
/// </summary>
public static class DepartureSelector
{
    public static IReadOnlyList<Departure> NextPerGroup(
        IEnumerable<Departure> orderedDepartures,
        Func<Departure, string?> groupKey,
        int perGroup)
    {
        if (perGroup < 1)
        {
            return [];
        }

        var taken = new Dictionary<string, int>(StringComparer.Ordinal);
        var result = new List<Departure>();
        foreach (var departure in orderedDepartures)
        {
            if (groupKey(departure) is not { } key)
            {
                continue;
            }

            taken.TryGetValue(key, out var count);
            if (count >= perGroup)
            {
                continue;
            }

            taken[key] = count + 1;
            result.Add(departure);
        }

        return result;
    }
}
