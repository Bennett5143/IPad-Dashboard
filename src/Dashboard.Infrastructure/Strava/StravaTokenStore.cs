using Dashboard.Domain.Running;
using Dashboard.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Dashboard.Infrastructure.Strava;

/// <summary>
/// Serverseitige Token-Persistenz (Single-Row) in der DB (FA-8.02). Die Tokens verlassen den Server nie.
/// Hinweis: für Produktion zusätzlich at-rest verschlüsseln (z. B. ASP.NET Data Protection / pgcrypto).
/// </summary>
internal sealed class StravaTokenStore : IStravaTokenStore
{
    private const int RowId = 1;

    private readonly IDbContextFactory<DashboardDbContext> _factory;

    public StravaTokenStore(IDbContextFactory<DashboardDbContext> factory) => _factory = factory;

    public async Task<StravaTokenSet?> GetAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var entity = await db.Set<StravaTokenEntity>().AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == RowId, ct);

        return entity is null
            ? null
            : new StravaTokenSet(entity.AccessToken, entity.RefreshToken, entity.ExpiresAtUtc);
    }

    public async Task SaveAsync(StravaTokenSet tokens, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var entity = await db.Set<StravaTokenEntity>().FirstOrDefaultAsync(e => e.Id == RowId, ct);

        if (entity is null)
        {
            db.Add(new StravaTokenEntity
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
        return await db.Set<StravaTokenEntity>().AnyAsync(e => e.Id == RowId, ct);
    }
}
