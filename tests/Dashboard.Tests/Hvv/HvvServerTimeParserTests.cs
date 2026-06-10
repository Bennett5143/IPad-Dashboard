namespace Dashboard.Tests.Hvv;

public class HvvServerTimeParserTests
{
    private static readonly TimeZoneInfo BerlinTz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    [Fact]
    public void Parse_Summer_UsesCestOffset()
    {
        var result = HvvServerTimeParser.Parse("10.06.2026", "14:00", BerlinTz);

        Assert.Equal(TimeSpan.FromHours(2), result.Offset);                     // CEST
        Assert.Equal(new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero), result.ToUniversalTime());
    }

    [Fact]
    public void Parse_Winter_UsesCetOffset()
    {
        var result = HvvServerTimeParser.Parse("10.01.2026", "14:00", BerlinTz);

        Assert.Equal(TimeSpan.FromHours(1), result.Offset);                     // CET
        Assert.Equal(new DateTimeOffset(2026, 1, 10, 13, 0, 0, TimeSpan.Zero), result.ToUniversalTime());
    }
}
