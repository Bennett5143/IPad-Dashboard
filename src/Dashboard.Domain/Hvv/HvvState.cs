namespace Dashboard.Domain.Hvv;

/// <summary>
/// Prozessweiter, beobachtbarer Zwischenspeicher der zuletzt abgerufenen Abfahrten. Der
/// Background-Service schreibt (max. 1×/min pro Haltestelle), die Tiles lesen aus dem Cache und
/// abonnieren <see cref="Changed"/> für Push (FA-6.03). Aggressives Caching schützt den Endpoint.
/// </summary>
public sealed class HvvState
{
    private readonly Lock _gate = new();
    private HvvSnapshot? _current;
    private bool _isStale;

    public event Action? Changed;

    public HvvSnapshot? Current
    {
        get { lock (_gate) { return _current; } }
    }

    public bool IsStale
    {
        get { lock (_gate) { return _isStale; } }
    }

    public DateTimeOffset? LastUpdatedUtc
    {
        get { lock (_gate) { return _current?.RetrievedAtUtc; } }
    }

    public void Update(HvvSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        lock (_gate)
        {
            _current = snapshot;
            _isStale = false;
        }
        Changed?.Invoke();
    }

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
