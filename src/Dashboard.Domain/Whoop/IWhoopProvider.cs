namespace Dashboard.Domain.Whoop;

/// <summary>Holt den aktuellen WHOOP-Tagesstatus (Recovery, Schlaf, Tages-Strain).</summary>
public interface IWhoopProvider
{
    Task<WhoopSnapshot> GetWhoopAsync(CancellationToken ct = default);
}
