namespace Dashboard.Domain.Hvv;

/// <summary>Liefert die nächsten Abfahrten für alle konfigurierten Haltestellen.</summary>
public interface IHvvProvider
{
    Task<HvvSnapshot> GetDeparturesAsync(CancellationToken ct = default);
}
