using System.Net;

using Dashboard.Infrastructure.Calendar;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Dashboard.Tests.Calendar;

public class IcsCalendarClientTests
{
    // Clock fixed so the [today-1, today+WindowDays] window covers mid-July 2026.
    private static readonly DateTimeOffset Now = new(2026, 7, 15, 0, 0, 0, TimeSpan.Zero);

    private const string SampleIcs =
        """
        BEGIN:VCALENDAR
        VERSION:2.0
        PRODID:-//test//test//EN
        BEGIN:VEVENT
        UID:1@test
        DTSTART:20260716T080000Z
        DTEND:20260716T090000Z
        SUMMARY:Einzeltermin
        END:VEVENT
        BEGIN:VEVENT
        UID:2@test
        DTSTART:20260715T060000Z
        DTEND:20260715T063000Z
        RRULE:FREQ=WEEKLY;COUNT=3
        SUMMARY:Woechentlich
        END:VEVENT
        END:VCALENDAR
        """;

    private static IcsCalendarClient Create(HttpClient http, CalendarOptions options) =>
        new(http, new FakeClock { UtcNow = Now }, Options.Create(options), NullLogger<IcsCalendarClient>.Instance);

    private static HttpClient StubReturning(string body)
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body),
        });
        return new HttpClient(handler);
    }

    [Fact]
    public async Task GetCalendarAsync_ExpandsRecurrence_AndParsesSingleEvent()
    {
        var client = Create(StubReturning(SampleIcs), new CalendarOptions
        {
            IcsUrls = ["https://cal.test/a.ics"],
        });

        var snapshot = await client.GetCalendarAsync();

        // 1 single event + 3 weekly occurrences (COUNT=3) = 4, all inside the window.
        Assert.Equal(4, snapshot.Events.Count);
        Assert.Equal(3, snapshot.Events.Count(e => e.Title == "Woechentlich"));

        var single = Assert.Single(snapshot.Events, e => e.Title == "Einzeltermin");
        Assert.Equal(new DateTime(2026, 7, 16, 8, 0, 0, DateTimeKind.Utc), single.Start.UtcDateTime);
        Assert.Equal(new DateTime(2026, 7, 16, 9, 0, 0, DateTimeKind.Utc), single.End.UtcDateTime);

        // Weekly occurrences land on 15/22/29 July (UTC 06:00).
        var weekly = snapshot.Events.Where(e => e.Title == "Woechentlich").OrderBy(e => e.Start).ToList();
        Assert.Equal(new DateTime(2026, 7, 15, 6, 0, 0, DateTimeKind.Utc), weekly[0].Start.UtcDateTime);
        Assert.Equal(new DateTime(2026, 7, 22, 6, 0, 0, DateTimeKind.Utc), weekly[1].Start.UtcDateTime);
        Assert.Equal(new DateTime(2026, 7, 29, 6, 0, 0, DateTimeKind.Utc), weekly[2].Start.UtcDateTime);

        // Sorted ascending by start.
        Assert.True(snapshot.Events.SequenceEqual(snapshot.Events.OrderBy(e => e.Start)));
    }

    [Fact]
    public async Task GetCalendarAsync_ParsesAllDayEvent_AsMidnightLocalSpanningOneDay()
    {
        const string allDayIcs =
            """
            BEGIN:VCALENDAR
            VERSION:2.0
            PRODID:-//test//test//EN
            BEGIN:VEVENT
            UID:3@test
            DTSTART;VALUE=DATE:20260717
            DTEND;VALUE=DATE:20260718
            SUMMARY:Ganztag
            END:VEVENT
            END:VCALENDAR
            """;

        var client = Create(StubReturning(allDayIcs), new CalendarOptions
        {
            IcsUrls = ["https://cal.test/a.ics"],
        });

        var snapshot = await client.GetCalendarAsync();

        var e = Assert.Single(snapshot.Events);
        Assert.True(e.AllDay);
        // Midnight in Europe/Berlin (CEST, +02:00 in July), spanning exactly one day.
        Assert.Equal(new DateTimeOffset(2026, 7, 17, 0, 0, 0, TimeSpan.FromHours(2)), e.Start);
        Assert.Equal(new DateTimeOffset(2026, 7, 18, 0, 0, 0, TimeSpan.FromHours(2)), e.End);
    }

    [Fact]
    public async Task GetCalendarAsync_ToleratesOneFailingSource_WhenAnotherSucceeds()
    {
        var handler = new StubHttpMessageHandler(req =>
            req.RequestUri!.Host == "bad.test"
                ? new HttpResponseMessage(HttpStatusCode.InternalServerError)
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(SampleIcs) });

        var client = Create(new HttpClient(handler), new CalendarOptions
        {
            IcsUrls = ["https://bad.test/a.ics", "https://good.test/b.ics"],
        });

        var snapshot = await client.GetCalendarAsync();

        Assert.Equal(4, snapshot.Events.Count);
    }

    [Fact]
    public async Task GetCalendarAsync_Throws_WhenAllSourcesFail()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var client = Create(new HttpClient(handler), new CalendarOptions
        {
            IcsUrls = ["https://bad.test/a.ics", "https://also-bad.test/b.ics"],
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetCalendarAsync());
    }

    [Fact]
    public async Task GetCalendarAsync_NormalizesWebcalScheme()
    {
        string? requestedScheme = null;
        var handler = new StubHttpMessageHandler(req =>
        {
            requestedScheme = req.RequestUri!.Scheme;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(SampleIcs) };
        });

        var client = Create(new HttpClient(handler), new CalendarOptions
        {
            IcsUrls = ["webcal://cal.test/a.ics"],
        });

        await client.GetCalendarAsync();

        Assert.Equal("https", requestedScheme);
    }
}
