namespace Dashboard.Domain.Hvv;

/// <summary>
/// Eine einzelne Abfahrt. <see cref="Delay"/> ist bewusst nullable: <c>null</c> bedeutet
/// "keine Echtzeitdaten" (≠ pünktlich), ein Wert bedeutet vorhandene Echtzeitdaten – siehe
/// HVV-Recherche, Falle „delay: null ≠ delay: 0".
/// </summary>
public sealed record Departure(
    string LineName,
    string Direction,
    TransportMode Mode,
    string ShortInfo,
    DateTimeOffset PlannedTime,
    TimeSpan? Delay)
{
    /// <summary>Liegen Echtzeitdaten vor (FA-6.08)?</summary>
    public bool HasLiveData => Delay.HasValue;

    /// <summary>Tatsächlich erwartete Abfahrt inkl. Verspätung – ohne Echtzeitdaten = Planzeit.</summary>
    public DateTimeOffset ExpectedTime => Delay.HasValue ? PlannedTime + Delay.Value : PlannedTime;
}
