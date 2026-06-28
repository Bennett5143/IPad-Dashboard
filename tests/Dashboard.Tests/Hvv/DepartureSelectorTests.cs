namespace Dashboard.Tests.Hvv;

public class DepartureSelectorTests
{
    private static readonly DateTimeOffset Anchor =
        new(2026, 6, 10, 8, 0, 0, TimeSpan.FromHours(2));

    private static Departure Dep(string line, int offsetMinutes) =>
        new(line, "Zielrichtung", TransportMode.Bus, "Bus", Anchor.AddMinutes(offsetMinutes), null);

    // "42" → eigene Gruppe; "143"/"443" → gemeinsame Gruppe "143/443"; alles andere verwerfen.
    private static string? GroupKey(Departure d) => d.LineName switch
    {
        "42" => "42",
        "143" or "443" => "143/443",
        _ => null
    };

    [Fact]
    public void OnePerGroup_TakesNextOfEachGroup_InTimeOrder()
    {
        var ordered = new[] { Dep("42", 2), Dep("42", 5), Dep("143", 7), Dep("443", 9), Dep("42", 10) };

        var result = DepartureSelector.NextPerGroup(ordered, GroupKey, perGroup: 1);

        // Nächster 42 (2 min) + nächster der 143/443-Gruppe (143 @ 7 min); spätere verworfen.
        Assert.Equal(new[] { "42", "143" }, result.Select(d => d.LineName));
    }

    [Fact]
    public void TwoPerGroup_TakesNextTwoOfEachGroup_DroppingExtras()
    {
        var ordered = new[] { Dep("42", 2), Dep("42", 5), Dep("143", 7), Dep("443", 9), Dep("42", 10) };

        var result = DepartureSelector.NextPerGroup(ordered, GroupKey, perGroup: 2);

        // 42: 2 & 5 (10 verworfen); Gruppe 143/443: 143 @ 7 & 443 @ 9 — Chronologie bleibt.
        Assert.Equal(new[] { "42", "42", "143", "443" }, result.Select(d => d.LineName));
    }

    [Fact]
    public void UnmatchedDepartures_AreDropped()
    {
        var ordered = new[] { Dep("999", 1), Dep("42", 3) };

        var result = DepartureSelector.NextPerGroup(ordered, GroupKey, perGroup: 1);

        Assert.Single(result);
        Assert.Equal("42", result[0].LineName);
    }

    [Fact]
    public void PerGroupBelowOne_ReturnsEmpty()
    {
        var ordered = new[] { Dep("42", 1) };

        Assert.Empty(DepartureSelector.NextPerGroup(ordered, GroupKey, perGroup: 0));
    }
}
