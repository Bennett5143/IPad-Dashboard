namespace Dashboard.Domain.Common;

/// <summary>Von einem Provider abgerufener, UI-fertiger Datenstand mit Abrufzeitpunkt.</summary>
public interface ISnapshot
{
    DateTimeOffset RetrievedAtUtc { get; }
}
