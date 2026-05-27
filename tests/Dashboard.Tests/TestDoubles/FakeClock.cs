namespace Dashboard.Tests.TestDoubles;

internal sealed class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; set; }
}
