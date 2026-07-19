using Dashboard.Domain.Common;

namespace Dashboard.Domain.Calendar;

/// <summary>Observable cache of the last fetched calendar agenda (home month grid + day timeline).</summary>
public sealed class CalendarState : ObservableState<CalendarSnapshot>;
