namespace Dashboard.Domain.Status;

/// <summary>Ein gepufferter Log-Eintrag für die Status-Seite (FA-11.03).</summary>
public sealed record RecentLogEntry(DateTimeOffset TimestampUtc, string Level, string Message);

/// <summary>Liefert die zuletzt gepufferten Warnungen/Fehler (neueste zuerst).</summary>
public interface IRecentLogProvider
{
    IReadOnlyList<RecentLogEntry> Recent { get; }
}

/// <summary>
/// Beschränkter In-Memory-Ringpuffer der jüngsten Log-Einträge (FA-11.03) — kein
/// Logfile-Parsing. Thread-sicher; ältester Eintrag fällt heraus, wenn die Kapazität voll
/// ist. Reine Logik; der Serilog-Sink füttert ihn.
/// </summary>
public sealed class RecentLogBuffer : IRecentLogProvider
{
    private readonly int _capacity;
    private readonly LinkedList<RecentLogEntry> _entries = new();
    private readonly Lock _gate = new();

    public RecentLogBuffer(int capacity = 50) =>
        _capacity = capacity > 0 ? capacity : throw new ArgumentOutOfRangeException(nameof(capacity));

    public void Add(RecentLogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        lock (_gate)
        {
            _entries.AddFirst(entry); // neueste zuerst
            if (_entries.Count > _capacity)
            {
                _entries.RemoveLast();
            }
        }
    }

    public IReadOnlyList<RecentLogEntry> Recent
    {
        get { lock (_gate) { return _entries.ToList(); } }
    }
}
