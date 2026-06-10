using System.Globalization;

namespace Dashboard.Infrastructure.Hvv;

/// <summary>
/// Parst die HVV-Server-Zeit (<c>dd.MM.yyyy</c> + <c>HH:mm</c>, immer Berlin-Zeit) in einen
/// <see cref="DateTimeOffset"/> mit korrektem CET/CEST-Offset. Wichtig im UTC-Container auf dem Pi:
/// <c>timeOffset</c> der Abfahrten ist relativ zu dieser Server-Zeit zu rechnen, nicht zu <c>DateTime.Now</c>.
/// </summary>
public static class HvvServerTimeParser
{
    public static DateTimeOffset Parse(string date, string time, TimeZoneInfo timeZone)
    {
        var local = DateTime.ParseExact(
            $"{date} {time}", "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
        var offset = timeZone.GetUtcOffset(local);

        return new DateTimeOffset(local, offset);
    }
}
