using Dashboard.Domain.Common;

namespace Dashboard.Domain.Weather;

/// <summary>Beobachtbarer Zwischenspeicher der zuletzt abgerufenen Wetterlage (FA-2.03).</summary>
public sealed class WeatherState : ObservableState<WeatherSnapshot>;
