using System.Globalization;

namespace Dashboard.Web.Components.Tiles;

/// <summary>Reine Darstellungs-Helfer für die HVV-Abfahrtskacheln.</summary>
public static class HvvFormatter
{
    private static readonly TimeZoneInfo BerlinTz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    /// <summary>Erwartete Abfahrtszeit als Uhrzeit (Berlin), z. B. „08:06".</summary>
    public static string Time(Departure departure) =>
        TimeZoneInfo.ConvertTime(departure.ExpectedTime, BerlinTz)
            .ToString("HH:mm", CultureInfo.InvariantCulture);

    /// <summary>Verspätung in Minuten als Badge („+2", „-1") – null ohne Echtzeitdaten oder bei Pünktlichkeit (FA-6.09).</summary>
    public static string? DelayBadge(Departure departure)
    {
        if (!departure.HasLiveData)
        {
            return null;
        }

        var minutes = (int)Math.Round(departure.Delay!.Value.TotalMinutes, MidpointRounding.AwayFromZero);
        return minutes switch
        {
            0 => null,
            > 0 => $"+{minutes}",
            _ => minutes.ToString(CultureInfo.InvariantCulture)
        };
    }

    /// <summary>Minuten bis zur erwarteten Abfahrt, nie negativ. Wird abgerundet: „4" heißt
    /// „noch mindestens 4 Minuten" — bei Abfahrten lieber zu knapp als zu großzügig anzeigen.</summary>
    public static int MinutesUntil(Departure departure, DateTimeOffset now)
    {
        var minutes = (departure.ExpectedTime - now).TotalMinutes;
        return minutes <= 0 ? 0 : (int)Math.Floor(minutes);
    }

    /// <summary>Verkehrsmittel als Mono-Label für den Spaltenkopf.</summary>
    public static string ModeLabel(TransportMode mode) => mode switch
    {
        TransportMode.Bus => "BUS",
        TransportMode.SBahn => "S-BAHN",
        TransportMode.UBahn => "U-BAHN",
        TransportMode.Ferry => "FÄHRE",
        TransportMode.RegionalTrain => "REGIONAL",
        _ => "ABFAHRTEN"
    };

    public static string ModeEmoji(TransportMode mode) => mode switch
    {
        TransportMode.Bus => "🚌",
        TransportMode.SBahn => "🚆",
        TransportMode.UBahn => "🚇",
        TransportMode.Ferry => "⛴️",
        TransportMode.RegionalTrain => "🚆",
        _ => "🚍"
    };

    public static string ModeCss(TransportMode mode) => mode switch
    {
        TransportMode.Bus => "mode-bus",
        TransportMode.SBahn => "mode-sbahn",
        TransportMode.UBahn => "mode-ubahn",
        TransportMode.Ferry => "mode-ferry",
        TransportMode.RegionalTrain => "mode-train",
        _ => "mode-other"
    };

    public static string UpdatedAt(DateTimeOffset retrievedAtUtc) =>
        TimeZoneInfo.ConvertTime(retrievedAtUtc, BerlinTz)
            .ToString("HH:mm", CultureInfo.InvariantCulture);
}
