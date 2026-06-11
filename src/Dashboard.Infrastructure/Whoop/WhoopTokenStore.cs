using Dashboard.Domain.Whoop;
using Dashboard.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Dashboard.Infrastructure.Whoop;

/// <summary>
/// Serverseitige Token-Persistenz (Single-Row) in der DB. Die Tokens verlassen den Server nie.
/// Hinweis: für Produktion zusätzlich at-rest verschlüsseln (z. B. ASP.NET Data Protection / pgcrypto).
/// </summary>
public sealed class WhoopTokenStore : IWhoopTokenStore
{
    private const int RowId = 1;

    private readonly IDbContextFactory<DashboardDbContext> _factory;

    public WhoopTokenStore(IDbContextFactory<DashboardDbContext> factory) => _factory = factory;

    public async Task<WhoopTokenSet?> GetAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var entity = await db.Set<WhoopTokenEntity>().AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == RowId, ct);

        return entity is null
            ? null
            : new WhoopTokenSet(entity.AccessToken, entity.RefreshToken, entity.ExpiresAtUtc);
    }

    public async Task SaveAsync(WhoopTokenSet tokens, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var entity = await db.Set<WhoopTokenEntity>().FirstOrDefaultAsync(e => e.Id == RowId, ct);

        if (entity is null)
        {
            db.Add(new WhoopTokenEntity
            {
                Id = RowId,
                AccessToken = tokens.AccessToken,
                RefreshToken = tokens.RefreshToken,
                ExpiresAtUtc = tokens.ExpiresAtUtc
            });
        }
        else
        {
            entity.AccessToken = tokens.AccessToken;
            entity.RefreshToken = tokens.RefreshToken;
            entity.ExpiresAtUtc = tokens.ExpiresAtUtc;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> HasTokensAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Set<WhoopTokenEntity>().AnyAsync(e => e.Id == RowId, ct);
    }
}
