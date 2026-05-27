using Dashboard.Domain.Time;
using Dashboard.Domain.Entities;

namespace Dashboard.Domain.Quotes;

public sealed class DailyQuoteService
{
    private static readonly TimeZoneInfo BerlinTz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    private readonly IQuoteRepository _repository;
    private readonly IClock _clock;

    public DailyQuoteService(IQuoteRepository repository, IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public DateOnly GetCurrentLocalDate()
    {
        var local = TimeZoneInfo.ConvertTime(_clock.UtcNow, BerlinTz);
        return DateOnly.FromDateTime(local.DateTime);
    }

    public async Task<Quote?> GetTodaysQuoteAsync(CancellationToken ct = default)
    {
        var count = await _repository.GetCountAsync(ct);
        if (count == 0) return null;

        var ordinal = QuoteSelector.SelectOrdinal(GetCurrentLocalDate(), count);
        return await _repository.GetByOrdinalAsync(ordinal, ct);
    }
}