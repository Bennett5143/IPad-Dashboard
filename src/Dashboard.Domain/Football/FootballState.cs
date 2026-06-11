using Dashboard.Domain.Common;

namespace Dashboard.Domain.Football;

/// <summary>Beobachtbarer Zwischenspeicher der zuletzt abgerufenen Fußballdaten (FA-4.04).</summary>
public sealed class FootballState : ObservableState<FootballSnapshot>;
