using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

using Dashboard.Domain.Crypto;

namespace Dashboard.Infrastructure.Crypto;

/// <summary>
/// <see cref="IMarketSentimentProvider"/> auf Basis des freien Fear-&amp;-Greed-Index
/// von alternative.me (<c>fng/?limit=1</c>, kein Key). <c>value</c> kommt als String.
/// </summary>
public sealed class FearGreedClient : IMarketSentimentProvider
{
    private readonly HttpClient _http;

    public FearGreedClient(HttpClient http) => _http = http;

    public async Task<MarketSentiment?> GetSentimentAsync(CancellationToken ct = default)
    {
        var response = await _http.GetFromJsonAsync<FngResponse>("fng/?limit=1", ct);
        var entry = response?.Data?.FirstOrDefault();

        if (entry is null ||
            !int.TryParse(entry.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            return null;
        }

        return new MarketSentiment(value, entry.Classification ?? string.Empty);
    }

    private sealed record FngResponse(
        [property: JsonPropertyName("data")] IReadOnlyList<FngEntry>? Data);

    private sealed record FngEntry(
        [property: JsonPropertyName("value")] string? Value,
        [property: JsonPropertyName("value_classification")] string? Classification);
}
