using Dashboard.Web.Components.Metrics;

namespace Dashboard.Tests.Components.Metrics;

public class MetricCatalogTests
{
    public static IEnumerable<object[]> AllMetricIds =>
        Enum.GetValues<MetricId>().Select(id => new object[] { id });

    [Theory]
    [MemberData(nameof(AllMetricIds))]
    public void Get_ReturnsCompleteExplanation_ForEveryMetricId(MetricId id)
    {
        var explanation = MetricCatalog.Get(id);

        Assert.False(string.IsNullOrWhiteSpace(explanation.Title), $"Titel fehlt für {id}");
        Assert.False(string.IsNullOrWhiteSpace(explanation.Summary), $"Beschreibung fehlt für {id}");
        Assert.False(string.IsNullOrWhiteSpace(explanation.Basis), $"Grundlage fehlt für {id}");
        Assert.False(string.IsNullOrWhiteSpace(explanation.Use), $"Wofür-Text fehlt für {id}");

        // Optionale Achsen dürfen, wenn gesetzt, nicht nur aus Leerraum bestehen.
        Assert.True(explanation.XAxis is null || explanation.XAxis.Trim().Length > 0, $"Leere X-Achse für {id}");
        Assert.True(explanation.YAxis is null || explanation.YAxis.Trim().Length > 0, $"Leere Y-Achse für {id}");
    }

    [Fact]
    public void Ids_CoverExactlyTheEnum()
    {
        var expected = Enum.GetValues<MetricId>().ToHashSet();
        Assert.Equal(expected, MetricCatalog.Ids.ToHashSet());
    }

    [Fact]
    public void Titles_AreUnique()
    {
        var titles = Enum.GetValues<MetricId>()
            .Select(id => MetricCatalog.Get(id).Title)
            .ToList();

        Assert.Equal(titles.Count, titles.Distinct().Count());
    }
}
