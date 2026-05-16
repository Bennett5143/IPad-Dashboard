using System.Reflection;
using System.Text.Json;
using Dashboard.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dashboard.Infrastructure.Seeding;

public sealed class QuoteSeeder(
    DashboardDbContext context,
    ILogger<QuoteSeeder> logger)
{
    private const string ResourceName =
        "Dashboard.Infrastructure.Seeding.Data.quotes.json";

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await context.Quotes.AnyAsync(ct))
        {
            logger.LogInformation(
                "Quotes-Tabelle ist bereits befüllt – Seeding übersprungen.");
            return;
        }

        var quotes = await LoadFromEmbeddedResourceAsync(ct);
        
        logger.LogInformation(
            "Seede {Count} Zitate in die Datenbank.", quotes.Count);

        context.Quotes.AddRange(
            quotes.Select(q => new Quote
            {
                Text = q.Text,
                Author = q.Author
            }));

        await context.SaveChangesAsync(ct);
    }

    private static async Task<IReadOnlyList<QuoteSeedDto>> 
        LoadFromEmbeddedResourceAsync(CancellationToken ct)
    {
        var assembly = typeof(QuoteSeeder).Assembly;
        await using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded Resource '{ResourceName}' nicht gefunden. " +
                $"Verfügbare Resources: {string.Join(", ", assembly.GetManifestResourceNames())}");

        var quotes = await JsonSerializer.DeserializeAsync<List<QuoteSeedDto>>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            ct);

        return quotes ?? throw new InvalidOperationException(
            "Zitate-JSON konnte nicht deserialisiert werden.");
    }
}