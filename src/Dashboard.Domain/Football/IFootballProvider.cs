namespace Dashboard.Domain.Football;

/// <summary>Liefert Ergebnisse, kommende Spiele und Tabellenplätze der konfigurierten Vereine.</summary>
public interface IFootballProvider
{
    Task<FootballSnapshot> GetFootballAsync(CancellationToken ct = default);
}
