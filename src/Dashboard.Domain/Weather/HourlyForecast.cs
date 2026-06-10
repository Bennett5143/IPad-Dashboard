namespace Dashboard.Domain.Weather;

/// <summary>Ein Eintrag der stündlichen Vorschau. <see cref="Time"/> ist bereits lokalisiert.</summary>
public sealed record HourlyForecast(
    DateTimeOffset Time,
    double Temperature,
    double PrecipitationProbability,
    WeatherCondition Condition);
