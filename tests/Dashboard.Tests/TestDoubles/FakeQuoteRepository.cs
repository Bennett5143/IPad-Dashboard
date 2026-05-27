namespace Dashboard.Tests.TestDoubles;

internal sealed class FakeQuoteRepository : IQuoteRepository
{
    private readonly List<Quote> _quotes;

    public FakeQuoteRepository(IEnumerable<Quote> quotes)
    {
        _quotes = quotes.OrderBy(q => q.Id).ToList();
    }

    public Task<int> GetCountAsync(CancellationToken ct = default)
        => Task.FromResult(_quotes.Count);

    public Task<Quote?> GetByOrdinalAsync(int ordinal, CancellationToken ct = default)
        => Task.FromResult(ordinal >= 0 && ordinal < _quotes.Count
            ? _quotes[ordinal]
            : null);
}
