using Dashboard.Domain.Common;

namespace Dashboard.Domain.Hvv;

/// <summary>
/// Beobachtbarer Zwischenspeicher der zuletzt abgerufenen Abfahrten (FA-6.03). Der Background-
/// Service schreibt max. 1×/min pro Haltestelle – aggressives Caching schützt den Endpoint.
/// </summary>
public sealed class HvvState : ObservableState<HvvSnapshot>;
