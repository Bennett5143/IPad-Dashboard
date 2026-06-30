namespace Dashboard.Domain.Crypto;

/// <summary>
/// Grobe Richtung/Stimmung – dient sowohl der abgeleiteten Marktstimmung
/// (Fear-&amp;-Greed) als auch der 24-h-Kursrichtung einer Münze (Vorzeichen/Farbe).
/// </summary>
public enum MarketMood
{
    Bearish,
    Neutral,
    Bullish
}
