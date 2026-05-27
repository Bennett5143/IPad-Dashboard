using Dashboard.Domain.Entities;
namespace Dashboard.Domain.Quotes;

public interface IQuoteRepository
{
    Task<int> GetCountAsync(CancellationToken ct = default);
    Task<Quote?> GetByOrdinalAsync(int ordinal, CancellationToken ct = default);
}