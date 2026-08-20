using System.Text.RegularExpressions;

using Dashboard.Domain.Hvv;

namespace Dashboard.Infrastructure.Hvv;

/// <summary>Übersetzt den HVV-<c>simpleType</c> in die anbieterneutrale <see cref="TransportMode"/>.</summary>
public static partial class HvvModeMapper
{
    /// <summary>Liniennamen der Hamburger S-Bahn: „S" gefolgt von einer Ziffer (S1, S3, S5 …).</summary>
    [GeneratedRegex(@"^S\d", RegexOptions.IgnoreCase)]
    private static partial Regex SBahnLine();

    /// <param name="lineName">
    /// Der Linienname, z. B. „S3". geofox meldet die S3/S5 ab Harburg Rathaus als <c>RAIL</c> —
    /// technisch vertretbar, auf einer Hamburger Abfahrtstafel falsch. Der Linienname ist die
    /// Autorität, nach der auch der Leser geht, also entscheidet er hier mit.
    /// </param>
    public static TransportMode Map(string? simpleType, string? lineName = null)
    {
        if (SBahnLine().IsMatch(lineName ?? string.Empty))
        {
            return TransportMode.SBahn;
        }

        return simpleType switch
        {
            "BUS" => TransportMode.Bus,
            "STRAIN" => TransportMode.SBahn,
            "UTRAIN" => TransportMode.UBahn,
            "FERRY" => TransportMode.Ferry,
            "RAIL" or "TRAIN" or "REGIONALTRAIN" or "AKN" => TransportMode.RegionalTrain,
            _ => TransportMode.Other
        };
    }
}
