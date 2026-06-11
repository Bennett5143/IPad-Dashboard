namespace Dashboard.Domain.Whoop;

/// <summary>
/// Prozessweiter, beobachtbarer Zwischenspeicher für den zuletzt abgerufenen WHOOP-Status.
/// Der Background-Service schreibt, die Tile liest und abonniert <see cref="Changed"/> für
/// Push-Aktualisierung ohne Reload. Gleiches Muster wie WeatherState/FootballState.
/// </summary>
public sealed class WhoopState
{
    private readonly Lock _gate = new();
    private WhoopSnapshot? _current;
    private bool _isStale;

    public event Action? Changed;

    public WhoopSnapshot? Current
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

    public void Update(WhoopSnapshot snapshot)
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
