using Dashboard.Domain.Hvv;

namespace Dashboard.Infrastructure.Hvv;

/// <summary>Übersetzt den HVV-<c>simpleType</c> in die anbieterneutrale <see cref="TransportMode"/>.</summary>
public static class HvvModeMapper
{
    public static TransportMode Map(string? simpleType) => simpleType switch
    {
        "BUS" => TransportMode.Bus,
        "STRAIN" => TransportMode.SBahn,
        "UTRAIN" => TransportMode.UBahn,
        "FERRY" => TransportMode.Ferry,
        "RAIL" or "TRAIN" or "REGIONALTRAIN" or "AKN" => TransportMode.RegionalTrain,
        _ => TransportMode.Other
    };
}
