using Dashboard.Domain.Calendar;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dashboard.Infrastructure.Calendar;

/// <summary>
/// Polls the configured calendar source(s) on a fixed interval and stores the result in
/// <see cref="CalendarState"/>. On failure the last data is kept (graceful degradation);
/// with no ICS URL configured the service exits cleanly so the home falls back to its
/// placeholder agenda without blocking app startup.
/// </summary>
public sealed class CalendarRefreshService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly CalendarState _state;
    private readonly CalendarOptions _options;
    private readonly ILogger<CalendarRefreshService> _logger;

    public CalendarRefreshService(
        IServiceScopeFactory scopeFactory,
        CalendarState state,
        IOptions<CalendarOptions> options,
        ILogger<CalendarRefreshService> logger)
    {
        _scopeFactory = scopeFactory;
        _state = state;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.IcsUrls is null || _options.IcsUrls.Length == 0)
        {
            _logger.LogInformation(
                "Kalender: Keine ICS-Quelle konfiguriert – die Home zeigt Platzhalter-Termine. " +
                "URL(s) in appsettings.Local.json setzen (Calendar:IcsUrls).");
            return;
        }

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
            var provider = scope.ServiceProvider.GetRequiredService<ICalendarProvider>();

            var snapshot = await provider.GetCalendarAsync(ct);
            _state.Update(snapshot);
            _logger.LogInformation("Kalender: {Count} Termine aktualisiert.", snapshot.Events.Count);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kalender: Aktualisierung fehlgeschlagen – letzte Daten bleiben erhalten.");
            _state.MarkStale();
        }
    }
}
