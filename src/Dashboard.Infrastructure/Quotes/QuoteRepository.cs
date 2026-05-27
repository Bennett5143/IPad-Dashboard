using Dashboard.Domain.Entities;
using Dashboard.Domain.Quotes;
using Dashboard.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Dashboard.Infrastructure.Quotes;

public sealed class QuoteRepository : IQuoteRepository
{
    private readonly IDbContextFactory<DashboardDbContext> _factory;

    public QuoteRepository(IDbContextFactory<DashboardDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<int> GetCountAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Quotes.CountAsync(ct);
    }

    public async Task<Quote?> GetByOrdinalAsync(int ordinal, CancellationToken ct = default)
    {
        if (ordinal < 0) return null;

        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Quotes
            .OrderBy(q => q.Id)
            .Skip(ordinal)
            .Take(1)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);
    }
}
