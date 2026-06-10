namespace Dashboard.Domain.Hvv;

/// <summary>Verkehrsmittel-Art, anbieterneutral. Übersetzung aus dem HVV-<c>simpleType</c> in der Infrastructure.</summary>
public enum TransportMode
{
    Other = 0,
    Bus,
    SBahn,
    UBahn,
    Ferry,
    RegionalTrain
}
