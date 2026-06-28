using System.Net;

using Dashboard.Infrastructure.Tiles;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Dashboard.Tests.Tiles;

public class TileProviderTests
{
    private static readonly byte[] FakePng = [0x89, 0x50, 0x4E, 0x47, 1, 2, 3];

    private static string TempDir() => Path.Combine(Path.GetTempPath(), "tiletest-" + Guid.NewGuid().ToString("N"));

    private static TileProvider Build(string cacheDir, Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var http = new HttpClient(new StubHttpMessageHandler(responder));
        var options = Options.Create(new TileOptions { CacheDirectory = cacheDir, MaxZoom = 20 });
        return new TileProvider(http, options, new FakeHostEnvironment(), NullLogger<TileProvider>.Instance);
    }

    [Theory]
    [InlineData(-1, 0, 0, false)]  // negative Zoomstufe
    [InlineData(0, 1, 0, false)]   // z=0 → nur Kachel (0,0)
    [InlineData(2, 4, 0, false)]   // z=2 → gültig sind 0..3
    [InlineData(2, 3, 3, true)]
    [InlineData(21, 0, 0, false)]  // über MaxZoom
    public void IsValid_ChecksSlippyMapBounds(int z, int x, int y, bool expected)
    {
        var provider = Build(TempDir(), _ => new HttpResponseMessage(HttpStatusCode.OK));
        Assert.Equal(expected, provider.IsValid(z, x, y));
    }

    [Fact]
    public async Task GetTileAsync_InvalidCoords_ReturnsNullWithoutFetching()
    {
        var calls = 0;
        var provider = Build(TempDir(), _ =>
        {
            calls++;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var result = await provider.GetTileAsync(2, 99, 0, CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task GetTileAsync_CacheMiss_FetchesThenServesFromCache()
    {
        var dir = TempDir();
        var calls = 0;
        var provider = Build(dir, _ =>
        {
            calls++;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(FakePng) };
        });

        try
        {
            var first = await provider.GetTileAsync(5, 8, 8, CancellationToken.None);
            var second = await provider.GetTileAsync(5, 8, 8, CancellationToken.None);

            Assert.Equal(FakePng, first);
            Assert.Equal(FakePng, second);
            Assert.Equal(1, calls); // der zweite Aufruf kommt aus dem Platten-Cache
            Assert.NotEmpty(Directory.GetFiles(dir, "8.png", SearchOption.AllDirectories));
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task GetTileAsync_UpstreamError_ReturnsNull()
    {
        var provider = Build(TempDir(), _ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var result = await provider.GetTileAsync(5, 8, 8, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public void CountTiles_WholeWorldAtZoom0_IsOne()
    {
        Assert.Equal(1, TileProvider.CountTiles(-85, -180, 85, 180, 0, 0));
    }

    [Fact]
    public void CountTiles_GrowsWithZoom_AndIsAtLeastOne()
    {
        var coarse = TileProvider.CountTiles(53.4, 9.9, 53.6, 10.1, 2, 2);
        var fine = TileProvider.CountTiles(53.4, 9.9, 53.6, 10.1, 10, 10);

        Assert.True(coarse >= 1);
        Assert.True(fine > coarse);
    }
}
