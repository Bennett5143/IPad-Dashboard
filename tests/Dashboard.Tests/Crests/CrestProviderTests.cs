using System.Net;

using Dashboard.Infrastructure.Crests;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Dashboard.Tests.Crests;

public class CrestProviderTests
{
    private const string AllowedHost = "crests.football-data.org";
    private static readonly byte[] FakePng = [0x89, 0x50, 0x4E, 0x47, 1, 2, 3];

    private static string TempDir() => Path.Combine(Path.GetTempPath(), "cresttest-" + Guid.NewGuid().ToString("N"));

    private static CrestProvider Build(string cacheDir, Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var http = new HttpClient(new StubHttpMessageHandler(responder));
        var options = Options.Create(new CrestOptions { CacheDirectory = cacheDir, AllowedHosts = [AllowedHost] });
        return new CrestProvider(http, options, new FakeHostEnvironment(), NullLogger<CrestProvider>.Instance);
    }

    [Theory]
    [InlineData("https://crests.football-data.org/86.png", true)]
    [InlineData("http://crests.football-data.org/86.png", true)]     // http erlaubt (Host zählt)
    [InlineData("https://evil.example.com/86.png", false)]           // Host nicht in Allowlist
    [InlineData("https://sub.crests.football-data.org/86.png", false)] // exakter Host, keine Subdomain
    [InlineData("/relative/86.png", false)]                          // nicht absolut
    [InlineData("ftp://crests.football-data.org/86.png", false)]     // kein http(s)
    [InlineData(null, false)]
    public void IsAllowed_OnlyAbsoluteHttpUrlsOnAllowlistedHosts(string? url, bool expected)
    {
        var provider = Build(TempDir(), _ => new HttpResponseMessage(HttpStatusCode.OK));
        Assert.Equal(expected, provider.IsAllowed(url));
    }

    [Fact]
    public async Task GetCrestAsync_DisallowedHost_ReturnsNullWithoutFetching()
    {
        var calls = 0;
        var provider = Build(TempDir(), _ =>
        {
            calls++;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(FakePng) };
        });

        var result = await provider.GetCrestAsync("https://evil.example.com/86.png", CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task GetCrestAsync_CacheMiss_FetchesThenServesFromCache()
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
            var first = await provider.GetCrestAsync($"https://{AllowedHost}/86.png", CancellationToken.None);
            var second = await provider.GetCrestAsync($"https://{AllowedHost}/86.png", CancellationToken.None);

            Assert.NotNull(first);
            Assert.Equal(FakePng, first!.Bytes);
            Assert.Equal("image/png", first.ContentType);
            Assert.Equal(FakePng, second!.Bytes);
            Assert.Equal(1, calls); // zweiter Aufruf kommt aus dem Platten-Cache
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
    public async Task GetCrestAsync_SvgUrl_InfersSvgContentType()
    {
        var dir = TempDir();
        var provider = Build(dir, _ =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(FakePng) });

        try
        {
            var result = await provider.GetCrestAsync($"https://{AllowedHost}/770.svg", CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("image/svg+xml", result!.ContentType);
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
    public async Task GetCrestAsync_UpstreamError_ReturnsNull()
    {
        var provider = Build(TempDir(), _ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var result = await provider.GetCrestAsync($"https://{AllowedHost}/86.png", CancellationToken.None);

        Assert.Null(result);
    }
}
