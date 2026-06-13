using Dashboard.Domain.Status;

namespace Dashboard.Tests.Status;

public class RecentLogBufferTests
{
    private static RecentLogEntry Entry(string message) =>
        new(new DateTimeOffset(2026, 6, 12, 12, 0, 0, TimeSpan.Zero), "Warning", message);

    [Fact]
    public void Recent_ReturnsNewestFirst()
    {
        var buffer = new RecentLogBuffer(capacity: 10);
        buffer.Add(Entry("a"));
        buffer.Add(Entry("b"));
        buffer.Add(Entry("c"));

        Assert.Equal(["c", "b", "a"], buffer.Recent.Select(e => e.Message));
    }

    [Fact]
    public void Add_DropsOldest_BeyondCapacity()
    {
        var buffer = new RecentLogBuffer(capacity: 2);
        buffer.Add(Entry("1"));
        buffer.Add(Entry("2"));
        buffer.Add(Entry("3"));

        Assert.Equal(["3", "2"], buffer.Recent.Select(e => e.Message));
    }

    [Fact]
    public void Recent_IsSnapshot_NotLiveView()
    {
        var buffer = new RecentLogBuffer();
        buffer.Add(Entry("x"));
        var snapshot = buffer.Recent;
        buffer.Add(Entry("y"));

        Assert.Single(snapshot); // alte Liste bleibt unverändert
    }

    [Fact]
    public void Constructor_RejectsNonPositiveCapacity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RecentLogBuffer(0));
    }
}
