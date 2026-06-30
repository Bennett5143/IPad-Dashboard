using System.Globalization;

namespace Dashboard.Tests.Crypto;

public class CryptoFormatterTests
{
    // Erzwingt de-DE-Ausgabe unabhängig von der Kultur des Test-Hosts (CI ist invariant).
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    [Fact]
    public void Price_PicksPrecisionByMagnitude()
    {
        Assert.Equal(51248m.ToString("C0", De), CryptoFormatter.Price(51248m));   // >= 1000 → keine Nachkommastellen
        Assert.Equal(3.45m.ToString("C2", De), CryptoFormatter.Price(3.45m));     // >= 1 → 2 Stellen
        Assert.Equal(0.1234m.ToString("C4", De), CryptoFormatter.Price(0.1234m)); // < 1 → 4 Stellen
    }

    [Theory]
    [InlineData(2.34, "+2,3 %")]
    [InlineData(-1.52, "-1,5 %")]
    [InlineData(0.0, "0,0 %")]
    public void Percent_AddsSignAndGermanDecimal(double pct, string expected)
    {
        Assert.Equal(expected, CryptoFormatter.Percent(pct));
    }

    [Fact]
    public void MarketCap_FormatsCompactly()
    {
        Assert.Equal("1,03 Bio. €", CryptoFormatter.MarketCap(1_027_528_382_139m));
        Assert.Equal("300 Mrd. €", CryptoFormatter.MarketCap(300_000_000_000m));
        Assert.Equal("5,5 Mio. €", CryptoFormatter.MarketCap(5_500_000m));
        Assert.Equal("–", CryptoFormatter.MarketCap(null));
    }

    [Theory]
    [InlineData(MarketMood.Bullish, "Bullish", "mood-bullish")]
    [InlineData(MarketMood.Bearish, "Bearish", "mood-bearish")]
    [InlineData(MarketMood.Neutral, "Neutral", "mood-neutral")]
    public void Mood_LabelAndClass(MarketMood mood, string label, string cssClass)
    {
        Assert.Equal(label, CryptoFormatter.MoodLabel(mood));
        Assert.Equal(cssClass, CryptoFormatter.MoodClass(mood));
    }
}
