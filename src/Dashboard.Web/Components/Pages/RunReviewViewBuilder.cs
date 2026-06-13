using System.Globalization;

namespace Dashboard.Web.Components.Pages;

/// <summary>Ein Monatsbalken des Jahresrückblicks.</summary>
public sealed record RunReviewMonth(string Label, string Km, double BarPercent);

/// <summary>View-Modell des Jahresrückblicks auf `/runs` (FA-8.16).</summary>
public sealed record RunReviewView(
    int Year,
    IReadOnlyList<int> AvailableYears,
    string RunCount,
    string TotalKm,
    string TotalElevation,
    string TotalTime,
    string Eddington,
    string LongestRun,
    string FastestPace,
    string BiggestMonth,
    IReadOnlyList<RunReviewMonth> Months);

/// <summary>Formatiert den Jahresrückblick — reine, testbare Aufbereitung.</summary>
public static class RunReviewViewBuilder
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    private static readonly string[] MonthLabels =
        ["Jan", "Feb", "Mär", "Apr", "Mai", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dez"];

    /// <summary>Rückblick für ein Jahr; <c>null</c>, wenn gar keine Läufe vorliegen.</summary>
    public static RunReviewView? Build(IReadOnlyList<Run> allRuns, int year)
    {
        var years = RunReviewCalculator.AvailableYears(allRuns);
        if (years.Count == 0)
        {
            return null;
        }

        var review = RunReviewCalculator.Build(allRuns, year);
        var maxMonthKm = review.Months.Max(m => m.Km);

        return new RunReviewView(
            review.Year,
            years,
            review.RunCount.ToString(German),
            $"{review.TotalKm.ToString("0.0", German)} km",
            $"{review.TotalElevationMeters.ToString("0", German)} m",
            FormatDuration(review.TotalTime),
            $"E = {review.EddingtonKm}",
            review.RunCount == 0 ? "–" : $"{review.Records.LongestKm.ToString("0.0", German)} km",
            review.Records.FastestPaceMinPerKm is { } pace ? FormatPace(pace) : "–",
            review.Records.BiggestMonth is { } month
                ? $"{MonthLabels[month - 1]} ({review.Records.BiggestMonthKm.ToString("0", German)} km)"
                : "–",
            review.Months
                .Select(m => new RunReviewMonth(
                    MonthLabels[m.Month - 1],
                    m.Km.ToString("0", German),
                    maxMonthKm > 0 ? m.Km / maxMonthKm * 100 : 0))
                .ToList());
    }

    private static string FormatDuration(TimeSpan time) =>
        $"{(int)time.TotalHours} h {time.Minutes:00} min";

    private static string FormatPace(double minPerKm)
    {
        var minutes = (int)minPerKm;
        var seconds = (int)Math.Round((minPerKm - minutes) * 60, MidpointRounding.AwayFromZero);
        if (seconds == 60)
        {
            minutes++;
            seconds = 0;
        }

        return $"{minutes}:{seconds:00} /km";
    }
}
