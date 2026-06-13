using Serilog.Core;
using Serilog.Events;

namespace Dashboard.Web.Infrastructure;

/// <summary>
/// Serilog-Sink, der Warnungen und Fehler in den <see cref="RecentLogBuffer"/> spiegelt (FA-11.03) —
/// damit die Status-Seite die jüngsten Probleme ohne Logfile-Parsing zeigen kann.
/// </summary>
public sealed class RingBufferLogSink : ILogEventSink
{
    private readonly RecentLogBuffer _buffer;

    public RingBufferLogSink(RecentLogBuffer buffer) => _buffer = buffer;

    public void Emit(LogEvent logEvent)
    {
        if (logEvent.Level < LogEventLevel.Warning)
        {
            return;
        }

        _buffer.Add(new RecentLogEntry(
            logEvent.Timestamp.UtcDateTime,
            logEvent.Level.ToString(),
            logEvent.RenderMessage()));
    }
}
