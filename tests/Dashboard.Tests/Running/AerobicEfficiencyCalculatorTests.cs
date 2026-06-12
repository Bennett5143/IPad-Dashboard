using Dashboard.Domain.Running;

namespace Dashboard.Tests.Running;

public class AerobicEfficiencyCalculatorTests
{
    private static Run Run(
        int year, int month, int day, double km = 5, int minutes = 30, int? avgHr = 150) =>
        new(
            year * 10000 + month * 100 + day, "Lauf", "Run",
            new DateTimeOffset(year, month, day, 6, 0, 0, TimeSpan.Zero),
            km * 1000, TimeSpan.FromMinutes(minutes), [],
            AverageHeartRate: avgHr);

    [Fact]
    public void BeatsPerKm_FiltersShortAndUnmeasuredRuns()
    {
        Assert.Equal(900, AerobicEfficiencyCalculator.BeatsPerKm(Run(2026, 5, 1))!.Value, 1);
        Assert.Null(AerobicEfficiencyCalculator.BeatsPerKm(Run(2026, 5, 1, avgHr: null)));
        Assert.Null(AerobicEfficiencyCalculator.BeatsPerKm(Run(2026, 5, 1, km: 1.5))); // zu kurz
    }

    [Fact]
    public void Monthly_BuildsContinuousSeries_WithMinRunsPerMonth()
    {
        var months = AerobicEfficiencyCalculator.Monthly(
        [
            Run(2026, 2, 3), Run(2026, 2, 10, minutes: 25),  // Feb: 2 Läufe → Wert
            Run(2026, 3, 5),                                  // Mär: 1 Lauf → kein Wert
            Run(2026, 5, 7), Run(2026, 5, 14),                // Mai: 2 Läufe → Wert
        ]);

        Assert.Equal(4, months.Count);                        // Feb–Mai lückenlos
        Assert.Equal((2026, 2), (months[0].Year, months[0].Month));
        Assert.NotNull(months[0].AvgBeatsPerKm);
        Assert.Equal(825, months[0].AvgBeatsPerKm!.Value, 1); // (900 + 750) / 2
        Assert.Null(months[1].AvgBeatsPerKm);                 // Mär unter Min-Stichprobe
        Assert.Equal(1, months[1].SampleCount);
        Assert.Equal(0, months[2].SampleCount);               // Apr ganz leer
        Assert.NotNull(months[3].AvgBeatsPerKm);
    }

    [Fact]
    public void TrendPercent_ComparesLatestWithThreeMonthsBack()
    {
        var months = new List<MonthlyEfficiency>
        {
            new(2026, 2, 3, 900),
            new(2026, 3, 1, null),
            new(2026, 4, 2, 880),
            new(2026, 5, 3, 855),  // jüngster Monat: vs. Feb (3 zurück) → −5 %
        };

        Assert.Equal(-5.0, AerobicEfficiencyCalculator.TrendPercent(months)!.Value, 1);
    }

    [Fact]
    public void TrendPercent_NullWithoutReferenceMonth()
    {
        Assert.Null(AerobicEfficiencyCalculator.TrendPercent([new MonthlyEfficiency(2026, 5, 3, 855)]));
        Assert.Null(AerobicEfficiencyCalculator.TrendPercent(
            [new MonthlyEfficiency(2026, 4, 2, 880), new MonthlyEfficiency(2026, 5, 3, 855)]));
        Assert.Null(AerobicEfficiencyCalculator.TrendPercent([]));
    }
}
