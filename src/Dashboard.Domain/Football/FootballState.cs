namespace Dashboard.Domain.Football;

/// <summary>
/// Prozessweiter, beobachtbarer Zwischenspeicher für die zuletzt abgerufenen Fußballdaten.
/// Der Background-Service schreibt, die Tile liest und abonniert <see cref="Changed"/> für
/// Push-Aktualisierung ohne Reload (FA-4.04). Siehe Begründung in WeatherState.
/// </summary>
public sealed class FootballState
{
    private readonly Lock _gate = new();
    private FootballSnapshot? _current;
    private bool _isStale;

    public event Action? Changed;

    public FootballSnapshot? Current
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

    public void Update(FootballSnapshot snapshot)
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
