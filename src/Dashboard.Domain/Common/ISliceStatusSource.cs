namespace Dashboard.Domain.Common;

/// <summary>
/// Einheitliche Status-Sicht auf einen Daten-Slice (FA-11.01): Name, ob Daten vorliegen,
/// ob sie veraltet sind und wann der letzte Stand kam. Wird von
/// <see cref="ObservableState{TSnapshot}"/> direkt implementiert — die Status-Seite und
/// der Header-Indikator aggregieren alle registrierten Quellen.
/// </summary>
public interface ISliceStatusSource
{
    /// <summary>Slice-Name, abgeleitet vom State-Typ (z. B. „Weather", „Hvv").</summary>
    string SliceName { get; }

    bool HasData { get; }

    bool IsStale { get; }

    DateTimeOffset? LastUpdatedUtc { get; }

    /// <summary>Wird ausgelöst, sobald sich Daten oder Stale-Status ändern.</summary>
    event Action? Changed;
}
