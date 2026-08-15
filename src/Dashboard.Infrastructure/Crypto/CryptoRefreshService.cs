using Dashboard.Domain.Crypto;
using Dashboard.Domain.Time;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dashboard.Infrastructure.Crypto;

/// <summary>
/// Pollt Markt-Watchlist und Marktstimmung in festem Intervall und legt das Ergebnis in
/// <see cref="CryptoState"/> ab. Der Markt ist Pflicht (schlägt er fehl → <c>MarkStale</c>),
/// die Stimmung ist best-effort: ihr Ausfall behält den letzten Wert und blockiert die Kurse nie.
/// </summary>
public sealed class CryptoRefreshService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly CryptoState _state;
    private readonly IClock _clock;
    private readonly CryptoOptions _options;
    private readonly ILogger<CryptoRefreshService> _logger;

    public CryptoRefreshService(
        IServiceScopeFactory scopeFactory,
        CryptoState state,
        IClock clock,
        IOptions<CryptoOptions> options,
        ILogger<CryptoRefreshService> logger)
    {
        _scopeFactory = scopeFactory;
        _state = state;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.RefreshInterval);
        try
        {
            do
            {
                await RefreshAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Regulärer Shutdown.
        }
    }

    private async Task RefreshAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var market = scope.ServiceProvider.GetRequiredService<ICryptoMarketProvider>();
            var sentimentProvider = scope.ServiceProvider.GetRequiredService<IMarketSentimentProvider>();

            var history = scope.ServiceProvider.GetRequiredService<ICryptoHistoryProvider>();

            var coins = await market.GetMarketAsync(ct);

            // Stimmung ist best-effort: scheitert sie, behalten wir den zuletzt bekannten Wert.
            var sentiment = _state.Current?.Sentiment;
            try
            {
                sentiment = await sentimentProvider.GetSentimentAsync(ct) ?? sentiment;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Krypto: Marktstimmung nicht abrufbar – letzter Wert bleibt.");
            }

            // Die Tagesreihe trägt nur das Badge im Wochenkalender – ebenfalls best-effort. Sie darf
            // die Kurse weder aufhalten noch als veraltet erscheinen lassen.
            var dailyChanges = _state.Current?.DailyChanges;
            try
            {
                var fetched = await history.GetDailyChangesAsync(ct);
                if (fetched.Count > 0)
                {
                    dailyChanges = fetched;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Krypto: Tagesreihe nicht abrufbar – letzte Reihe bleibt.");
            }

            _state.Update(new CryptoSnapshot(
                coins, sentiment, _options.SummaryCoinId, _clock.UtcNow, dailyChanges));
            _logger.LogInformation("Krypto: Snapshot aktualisiert ({Count} Coins).", coins.Count);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Krypto: Aktualisierung fehlgeschlagen – letzte Daten bleiben erhalten.");
            _state.MarkStale();
        }
    }
}
