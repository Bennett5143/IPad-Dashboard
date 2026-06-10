namespace Dashboard.Domain.Weather;

/// <summary>
/// Reine Aggregations-Logik: verdichtet die rohen <see cref="ForecastStep"/>s der API
/// zur UI-fertigen <see cref="WeatherSnapshot"/>. Bewusst frei von HTTP/JSON, damit die
/// fachlich interessante Logik (Tages-Aggregation, Zeitzonen, Fallbacks) isoliert testbar bleibt.
/// </summary>
public static class WeatherSnapshotFactory
{
    public static WeatherSnapshot Create(
        CurrentWeather current,
        IReadOnlyList<ForecastStep> steps,
        DateTimeOffset nowUtc,
        TimeZoneInfo timeZone,
        int hourlyCount)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(steps);
        ArgumentNullException.ThrowIfNull(timeZone);

        var localNow = TimeZoneInfo.ConvertTime(nowUtc, timeZone);
        var todayDate = DateOnly.FromDateTime(localNow.Date);
        var tomorrowDate = todayDate.AddDays(1);

        DateOnly LocalDate(ForecastStep step) =>
            DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(step.TimestampUtc, timeZone).Date);

        var todaySteps = steps.Where(s => LocalDate(s) == todayDate).ToList();
        var tomorrowSteps = steps.Where(s => LocalDate(s) == tomorrowDate).ToList();

        // Spät am Abend liefert die Vorhersage ggf. keinen (oder nur einen) Tagesschritt mehr
        // für "heute". Die aktuell gemessene Temperatur wird in Min/Max einbezogen, damit die
        // Spanne nie im Widerspruch zur angezeigten Ist-Temperatur steht (z. B. 13/13 bei aktuell 14).
        var todayAggregate = Aggregate(todayDate, todaySteps);
        var today = todayAggregate is null
            ? new DailyForecast(
                todayDate, current.Temperature, current.Temperature,
                current.Condition, current.Description, 0d)
            : todayAggregate with
            {
                MinTemperature = Math.Min(todayAggregate.MinTemperature, current.Temperature),
                MaxTemperature = Math.Max(todayAggregate.MaxTemperature, current.Temperature)
            };

        var tomorrow = Aggregate(tomorrowDate, tomorrowSteps);

        var hourly = steps
            .Where(s => s.TimestampUtc > nowUtc)
            .OrderBy(s => s.TimestampUtc)
            .Take(hourlyCount)
            .Select(s => new HourlyForecast(
                TimeZoneInfo.ConvertTime(s.TimestampUtc, timeZone),
                s.Temperature,
                s.PrecipitationProbability,
                s.Condition))
            .ToList();

        return new WeatherSnapshot(current, today, tomorrow, hourly, nowUtc);
    }

    private static DailyForecast? Aggregate(DateOnly date, IReadOnlyList<ForecastStep> steps)
    {
        if (steps.Count == 0)
        {
            return null;
        }

        var min = steps.Min(s => s.Temperature);
        var max = steps.Max(s => s.Temperature);
        var pop = steps.Max(s => s.PrecipitationProbability);
        var dominant = DominantStep(steps);

        return new DailyForecast(date, min, max, dominant.Condition, dominant.Description, pop);
    }

    // Leitzustand des Tages: häufigster Zustand, bei Gleichstand der "schwerere"
    // (Gewitter vor Schnee vor Regen …), damit die Kachel nicht "heiter" zeigt,
    // obwohl es die halbe Zeit regnet.
    private static ForecastStep DominantStep(IReadOnlyList<ForecastStep> steps)
    {
        var condition = steps
            .GroupBy(s => s.Condition)
            .OrderByDescending(g => g.Count())
            .ThenByDescending(g => Severity(g.Key))
            .First()
            .Key;

        return steps.First(s => s.Condition == condition);
    }

    private static int Severity(WeatherCondition condition) => condition switch
    {
        WeatherCondition.Thunderstorm => 6,
        WeatherCondition.Snow => 5,
        WeatherCondition.Rain => 4,
        WeatherCondition.Drizzle => 3,
        WeatherCondition.Clouds => 2,
        WeatherCondition.Mist => 1,
        WeatherCondition.Clear => 0,
        _ => -1
    };
}
