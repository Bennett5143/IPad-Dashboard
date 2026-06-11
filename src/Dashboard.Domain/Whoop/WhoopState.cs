using Dashboard.Domain.Common;

namespace Dashboard.Domain.Whoop;

/// <summary>Beobachtbarer Zwischenspeicher des zuletzt abgerufenen WHOOP-Status.</summary>
public sealed class WhoopState : ObservableState<WhoopSnapshot>;
