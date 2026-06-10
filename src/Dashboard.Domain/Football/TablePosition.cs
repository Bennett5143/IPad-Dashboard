namespace Dashboard.Domain.Football;

/// <summary>Tabellenplatzierung eines Vereins in seiner Liga.</summary>
public sealed record TablePosition(
    int Position,
    int PlayedGames,
    int Points);
