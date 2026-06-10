namespace Dashboard.Tests.Hvv;

public class DepartureTests
{
    private static readonly DateTimeOffset Planned =
        new(2026, 6, 10, 14, 6, 0, TimeSpan.FromHours(2));

    [Fact]
    public void WithoutDelay_HasNoLiveData_AndExpectedEqualsPlanned()
    {
        var departure = new Departure("189", "S Blankenese", TransportMode.Bus, "Bus", Planned, null);

        Assert.False(departure.HasLiveData);
        Assert.Equal(Planned, departure.ExpectedTime);
    }

    [Fact]
    public void WithDelay_HasLiveData_AndExpectedIsShifted()
    {
        var departure = new Departure(
            "189", "S Blankenese", TransportMode.Bus, "Bus", Planned, TimeSpan.FromMinutes(2));

        Assert.True(departure.HasLiveData);
        Assert.Equal(Planned.AddMinutes(2), departure.ExpectedTime);
    }

    [Fact]
    public void ZeroDelay_StillCountsAsLiveData()
    {
        // delay: 0 bedeutet Echtzeit + pünktlich – nicht zu verwechseln mit "keine Echtzeit".
        var departure = new Departure("U1", "Norderstedt", TransportMode.UBahn, "U-Bahn", Planned, TimeSpan.Zero);

        Assert.True(departure.HasLiveData);
        Assert.Equal(Planned, departure.ExpectedTime);
    }
}
