namespace Dashboard.Domain.Weather;

/// <summary>
/// Prozessweiter, beobachtbarer Zwischenspeicher für die zuletzt abgerufene Wetterlage.
/// Der Background-Service schreibt hier rein, die Tiles lesen und abonnieren
/// <see cref="Changed"/> für Push-Aktualisierung ohne Reload (FA-2.03).
/// </summary>
/// <remarks>
/// Bewusst ein beobachtbarer Singleton statt <c>IMemoryCache</c>: In Blazor Server braucht
/// es neben dem In-Memory-Halten auch ein Change-Signal, das die Kacheln zum Neu-Rendern
/// bewegt – das liefert ein reines Cache-Abstract nicht.
/// </remarks>
public sealed class WeatherState
{
    private readonly Lock _gate = new();
    private WeatherSnapshot? _current;
    private bool _isStale;

    /// <summary>Wird ausgelöst, sobald sich Daten oder Stale-Status ändern.</summary>
    public event Action? Changed;

    public WeatherSnapshot? Current
    {
        get { lock (_gate) { return _current; } }
    }

    /// <summary>Sind die gehaltenen Daten veraltet, weil der letzte Abruf scheiterte?</summary>
    public bool IsStale
    {
        get { lock (_gate) { return _isStale; } }
    }

    public DateTimeOffset? LastUpdatedUtc
    {
        get { lock (_gate) { return _current?.RetrievedAtUtc; } }
    }

    public void Update(WeatherSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        lock (_gate)
        {
            _current = snapshot;
            _isStale = false;
        }
        Changed?.Invoke();
    }

    /// <summary>
    /// Markiert vorhandene Daten als veraltet (letzter Abruf fehlgeschlagen), behält sie aber –
    /// Graceful Degradation statt leerer Kachel. Ohne vorhandene Daten ein No-op.
    /// </summary>
    public void MarkStale()
    {
        lock (_gate)
        {
            if (_current is null)
            {
                return;
            }

            _isStale = true;
        }
        Changed?.Invoke();
    }
}
