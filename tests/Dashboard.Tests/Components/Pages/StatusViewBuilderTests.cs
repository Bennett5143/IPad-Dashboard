using Dashboard.Web.Components.Pages;

namespace Dashboard.Tests.Components.Pages;

public class StatusViewBuilderTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 6, 12, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void BuildSliceRows_MapsStatesAndGermanLabels_SortedByLabel()
    {
        var rows = StatusViewBuilder.BuildSliceRows(
        [
            ("Whoop", true, false, NowUtc.AddMinutes(-5)),
            ("Weather", true, true, NowUtc.AddHours(-3)),
            ("Hvv", false, false, null),
        ], NowUtc);

        Assert.Equal(3, rows.Count);
        Assert.Equal(["HVV", "WHOOP", "Wetter"], rows.Select(r => r.Label));

        var hvv = rows[0];
        Assert.Equal("keine Daten", hvv.StateLabel);
        Assert.Equal("status-idle", hvv.CssClass);
        Assert.Equal("–", hvv.UpdatedLabel);

        var whoop = rows[1];
        Assert.Equal("aktuell", whoop.StateLabel);
        Assert.Equal("status-ok", whoop.CssClass);
        Assert.Equal("vor 5 min", whoop.UpdatedLabel);

        var weather = rows[2];
        Assert.Equal("veraltet", weather.StateLabel);
        Assert.Equal("status-warn", weather.CssClass);
        Assert.Equal("vor 3 h", weather.UpdatedLabel);
    }

    [Fact]
    public void RelativeLabel_PicksSensibleUnits()
    {
        Assert.Equal("gerade eben", StatusViewBuilder.RelativeLabel(NowUtc.AddSeconds(-30), NowUtc));
        Assert.Equal("vor 59 min", StatusViewBuilder.RelativeLabel(NowUtc.AddMinutes(-59), NowUtc));
        Assert.Equal("vor 47 h", StatusViewBuilder.RelativeLabel(NowUtc.AddHours(-47), NowUtc));
        Assert.Equal("vor 4 Tagen", StatusViewBuilder.RelativeLabel(NowUtc.AddDays(-4), NowUtc));
        Assert.Equal("gerade eben", StatusViewBuilder.RelativeLabel(NowUtc.AddMinutes(5), NowUtc)); // Zukunft klemmt
    }

    [Fact]
    public void HistoryDepthLabel_ShowsDepthOrWaiting()
    {
        var today = new DateOnly(2026, 6, 12);

        Assert.Equal("wartet auf erste Daten", StatusViewBuilder.HistoryDepthLabel(null, today));
        Assert.Equal(
            "zurück bis 14.03.2026 (90 Tage)",
            StatusViewBuilder.HistoryDepthLabel(new DateOnly(2026, 3, 14), today));
    }
}
