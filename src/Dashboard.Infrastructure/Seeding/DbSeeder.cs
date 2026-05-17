using System.Text.Json;

using Dashboard.Domain.Entities;
using Dashboard.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dashboard.Infrastructure.Seeding;

public sealed class DbSeeder(DashboardDbContext context, IOptions<SeedSettings> options, ILogger<DbSeeder> logger)
{
    private const string QuotesResourceName =
        "Dashboard.Infrastructure.Seeding.Data.quotes.json";

    private readonly SeedSettings _settings = options.Value;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (!_settings.Enabled)
        {
            logger.LogInformation("Seeding ist deaktiviert (SeedSettings.Enabled = false).");
            return;
        }

        await EnsureSchemaIsUpToDateAsync(ct);
        await SeedQuotesAsync(ct);
    }

    private async Task EnsureSchemaIsUpToDateAsync(CancellationToken ct)
    {
        var pending = (await context.Database.GetPendingMigrationsAsync(ct)).ToList();
        if (pending.Count == 0) return;

        logger.LogError(
            "DB-Schema ist nicht aktuell. {Count} ausstehende Migration(en): {Migrations}. " +
            "Bitte 'dotnet ef database update' im Dashboard.Web-Projekt ausführen.",
            pending.Count,
            string.Join(", ", pending));

        throw new InvalidOperationException(
            $"DB-Schema ist nicht aktuell. {pending.Count} ausstehende Migration(en). " +
            "Seeding abgebrochen.");
    }

    private async Task SeedQuotesAsync(CancellationToken ct)
    {
        if (await context.Quotes.AnyAsync(ct))
        {
            logger.LogInformation(
                "Quotes-Tabelle ist bereits befüllt – Seeding übersprungen.");
            return;
        }

        var quotes = await LoadFromEmbeddedResourceAsync<List<QuoteSeedDto>>(
            QuotesResourceName, ct);

        logger.LogInformation("Seede {Count} Zitate.", quotes.Count);

        context.Quotes.AddRange(
            quotes.Select(q => new Quote { Text = q.Text, Author = q.Author }));

        await context.SaveChangesAsync(ct);
    }

    private static async Task<T> LoadFromEmbeddedResourceAsync<T>(
        string resourceName, CancellationToken ct)
    {
        var assembly = typeof(DbSeeder).Assembly;
        await using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded Resource '{resourceName}' nicht gefunden. " +
                $"Verfügbare Resources: {string.Join(", ", assembly.GetManifestResourceNames())}");

        var result = await JsonSerializer.DeserializeAsync<T>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            ct);

        return result ?? throw new InvalidOperationException(
            $"Resource '{resourceName}' konnte nicht zu {typeof(T).Name} deserialisiert werden.");
    }
}
