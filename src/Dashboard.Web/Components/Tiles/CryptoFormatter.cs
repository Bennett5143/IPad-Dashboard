using System.Globalization;

namespace Dashboard.Web.Components.Tiles;

/// <summary>Reine Darstellungs-Helfer für die Krypto-Kacheln (de-DE, Vorzeichen, Mood-Klassen).</summary>
public static class CryptoFormatter
{
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    /// <summary>Preis in Euro; je kleiner der Kurs, desto mehr Nachkommastellen.</summary>
    public static string Price(decimal priceEur)
    {
        var format = priceEur switch
        {
            >= 1000m => "C0",
            >= 1m => "C2",
            _ => "C4"
        };
        return priceEur.ToString(format, De);
    }

    /// <summary>24-h-Änderung mit Vorzeichen, eine Nachkommastelle.</summary>
    public static string Percent(double pct)
    {
        var sign = pct > 0 ? "+" : string.Empty;
        return sign + pct.ToString("0.0", De) + " %";
    }

    /// <summary>Marktkapitalisierung kompakt (Bio./Mrd./Mio. €).</summary>
    public static string MarketCap(decimal? cap) => cap switch
    {
        null => "–",
        >= 1_000_000_000_000m => (cap.Value / 1_000_000_000_000m).ToString("0.##", De) + " Bio. €",
        >= 1_000_000_000m => (cap.Value / 1_000_000_000m).ToString("0.##", De) + " Mrd. €",
        >= 1_000_000m => (cap.Value / 1_000_000m).ToString("0.##", De) + " Mio. €",
        _ => cap.Value.ToString("C0", De)
    };

    public static string MoodLabel(MarketMood mood) => mood switch
    {
        MarketMood.Bullish => "Bullish",
        MarketMood.Bearish => "Bearish",
        _ => "Neutral"
    };

    public static string MoodClass(MarketMood mood) => mood switch
    {
        MarketMood.Bullish => "mood-bullish",
        MarketMood.Bearish => "mood-bearish",
        _ => "mood-neutral"
    };
}
