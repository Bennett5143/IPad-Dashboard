namespace Dashboard.Domain.Weather;

/// <summary>Zu einem Tag aggregierte Vorhersage (Min/Max-Temperatur, Leitzustand, Regenwahrscheinlichkeit).</summary>
public sealed record DailyForecast(
    DateOnly Date,
    double MinTemperature,
    double MaxTemperature,
    WeatherCondition Condition,
    string Description,
    double PrecipitationProbability);
