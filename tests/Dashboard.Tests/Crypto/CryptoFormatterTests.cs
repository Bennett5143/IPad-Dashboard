using System.Globalization;

namespace Dashboard.Tests.Crypto;

public class CryptoFormatterTests
{
    // Erzwingt de-DE-Ausgabe unabhängig von der Kultur des Test-Hosts (CI ist invariant).
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    [Fact]
    public void Price_PicksPrecisionByMagnitude()
    {
        Assert.Equal("$" + 51248m.ToString("N0", De), CryptoFormatter.Price(51248m));   // >= 1000 → keine Nachkommastellen
        Assert.Equal("$" + 3.45m.ToString("N2", De), CryptoFormatter.Price(3.45m));     // >= 1 → 2 Stellen
        Assert.Equal("$" + 0.1234m.ToString("N4", De), CryptoFormatter.Price(0.1234m)); // < 1 → 4 Stellen
    }

    /// <summary>
    /// Das Dollarzeichen steht ausdrücklich davor: ein de-DE-Währungsformat setzte ein € hinter
    /// die Zahl, ganz gleich, in welcher Währung der Anbieter geliefert hat. Die Gruppierung
    /// bleibt deutsch — nur das Zeichen wechselt.
    /// </summary>
    [Fact]
    public void Price_UsesADollarSignWithGermanGrouping()
    {
        Assert.Equal("$51.248", CryptoFormatter.Price(51248m));
        Assert.Equal("$3,45", CryptoFormatter.Price(3.45m));
        Assert.DoesNotContain("€", CryptoFormatter.Price(51248m), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(2.34, "+2,3 %")]
    [InlineData(-1.52, "-1,5 %")]
    [InlineData(0.0, "0,0 %")]
    public void Percent_AddsSignAndGermanDecimal(double pct, string expected)
    {
        Assert.Equal(expected, CryptoFormatter.Percent(pct));
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
