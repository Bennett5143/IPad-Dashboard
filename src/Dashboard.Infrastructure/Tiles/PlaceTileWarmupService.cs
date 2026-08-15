using Dashboard.Domain.Running;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dashboard.Infrastructure.Tiles;

/// <summary>
/// Lädt die Kacheln der Lauf-Orte vor — genau die Ausschnitte, die die Heatmap zeigt, und keine
/// anderen. Weil die Ansicht je Ort fest ist (kein Zoom, kein Verschieben), ist diese Menge
/// endlich und vorher bekannt; das offline iPad bekommt damit garantiert keine graue Fläche.
/// <para>
/// Der Lauf ist wiederholbar: vorhandene Kacheln werden nur geprüft, nicht erneut geholt. Er läuft
/// verzögert nach dem Start (die Orts-Zuordnung soll erst durch sein) und danach in großem
/// Abstand, weil sich Orte selten ändern.
/// </para>
/// </summary>
public sealed class PlaceTileWarmupService : BackgroundService
{
    /// <summary>Kartenfläche auf dem Kiosk (1024×768 abzüglich Rail, Kopf und Bedienzeilen).</summary>
    private const int ViewportWidthPx = 940;
    private const int ViewportHeightPx = 400;

    /// <summary>
    /// Eine Zoomstufe über der Passform als Reserve: die Karte rundet beim Einpassen, und eine
    /// Stufe mehr kostet je Ort nur ein Vielfaches weniger Kacheln, als sie an Lücken erspart.
    /// </summary>
    private const int ReserveZoomLevels = 1;

    /// <summary>Schranke je Ort — schützt vor einem versehentlich riesigen Ausschnitt.</summary>
    private const long MaxTilesPerPlace = 4_000;

    private static readonly TimeSpan StartDelay = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TileProvider _tiles;
    private readonly ILogger<PlaceTileWarmupService> _logger;

    public PlaceTileWarmupService(
        IServiceScopeFactory scopeFactory, TileProvider tiles, ILogger<PlaceTileWarmupService> logger)
    {
        _scopeFactory = scopeFactory;
        _tiles = tiles;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartDelay, stoppingToken);

            using var timer = new PeriodicTimer(Interval);
            do
            {
                await WarmPlacesAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Regulärer Shutdown.
        }
    }

    private async Task WarmPlacesAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<IRunPlaceStore>();

            var places = await store.GetPlacesAsync(ct);
            if (places.Count == 0)
            {
                return;
            }

            var fetched = 0;
            foreach (var place in places)
            {
                ct.ThrowIfCancellationRequested();

                var zoom = PlaceMapView.FitZoom(place.Bounds, ViewportWidthPx, ViewportHeightPx);
                var maxZoom = Math.Min(zoom + ReserveZoomLevels, 19);
                var count = PlaceMapView.TileCount(place.Bounds, zoom, maxZoom);

                if (count > MaxTilesPerPlace)
                {
                    _logger.LogWarning(
                        "Kachel-Warmup: Ort {Place} übersprungen – {Count} Kacheln über der Schranke.",
                        place.Name, count);
                    continue;
                }

                fetched += await _tiles.WarmAsync(
                    place.Bounds.MinLat, place.Bounds.MinLon, place.Bounds.MaxLat, place.Bounds.MaxLon,
                    zoom, maxZoom, ct);
            }

            _logger.LogInformation(
                "Kachel-Warmup: {Places} Orte geprüft, {Fetched} Kacheln neu geladen.", places.Count, fetched);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Kachel-Warmup fehlgeschlagen – nächster Durchlauf versucht es erneut.");
        }
    }
}
