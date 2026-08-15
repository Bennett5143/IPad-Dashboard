using Dashboard.Infrastructure.Crests;

using Microsoft.Extensions.Configuration;

namespace Dashboard.Tests.Configuration;

/// <summary>
/// Der Configuration-Binder <em>hängt</em> an eine vorbelegte Collection <em>an</em>, statt sie zu
/// ersetzen. Ein Vorgabewert an der Property plus dieselben Werte in <c>appsettings.json</c> ergab
/// darum jeden Wert doppelt — genau so entstanden zehn Liga-Pills und acht Coins mit drei Dubletten.
/// Diese Tests nageln die Regel fest: Listen stehen nur in der Konfiguration, gebunden enthalten sie
/// jeden Wert genau einmal, und leer heißt leer.
/// </summary>
public class OptionsListBindingTests
{
    private static T Bind<T>(string section, Dictionary<string, string?> values)
        where T : class, new() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build()
            .GetSection(section)
            .Get<T>() ?? new T();

    private static Dictionary<string, string?> Array(string section, string key, params string[] items) =>
        items
            .Select((item, index) => (Key: $"{section}:{key}:{index}", Value: (string?)item))
            .ToDictionary(pair => pair.Key, pair => pair.Value);

    /// <summary>Eine Options-Klasse mit vorbelegter Liste — nur für den Beweis unten.</summary>
    private sealed class OptionsWithPrefilledList
    {
        public IReadOnlyList<string> Items { get; init; } = ["a", "b"];
    }

    /// <summary>
    /// Der Beweis für die Regel: bindet man dieselben Werte, die schon an der Property stehen, stehen
    /// sie danach doppelt drin. Schlägt dieser Test fehl, hat der Binder sein Verhalten geändert —
    /// dann sind die Kommentare in den Options-Klassen überholt (und die Regel überflüssig).
    /// </summary>
    [Fact]
    public void ConfigurationBinder_AppendsToAPrefilledList_InsteadOfReplacingIt()
    {
        var bound = Bind<OptionsWithPrefilledList>(
            "Prefilled",
            Array("Prefilled", "Items", "a", "b"));

        Assert.Equal(["a", "b", "a", "b"], bound.Items);
    }

    [Fact]
    public void FootballOptions_ConfiguredLeagues_AppearExactlyOnce()
    {
        var options = Bind<FootballOptions>(
            FootballOptions.SectionName,
            Array(FootballOptions.SectionName, "LeagueCodes", "PL", "PD", "BL1", "SA", "FL1"));

        Assert.Equal(["PL", "PD", "BL1", "SA", "FL1"], options.LeagueCodes);
    }

    [Fact]
    public void FootballOptions_OtherLeaguesConfigured_ContainNothingFromCode()
    {
        var options = Bind<FootballOptions>(
            FootballOptions.SectionName,
            Array(FootballOptions.SectionName, "LeagueCodes", "BL1", "DED"));

        Assert.Equal(["BL1", "DED"], options.LeagueCodes);
    }

    [Fact]
    public void FootballOptions_WithoutConfiguration_StaysEmpty()
    {
        var options = Bind<FootballOptions>(FootballOptions.SectionName, []);

        Assert.Empty(options.LeagueCodes);
    }

    [Fact]
    public void CryptoOptions_ConfiguredCoins_AppearExactlyOnce()
    {
        var options = Bind<CryptoOptions>(
            CryptoOptions.SectionName,
            Array(CryptoOptions.SectionName, "CoinIds", "bitcoin", "ethereum", "solana"));

        Assert.Equal(["bitcoin", "ethereum", "solana"], options.CoinIds);
    }

    [Fact]
    public void CryptoOptions_WithoutConfiguration_StaysEmpty()
    {
        var options = Bind<CryptoOptions>(CryptoOptions.SectionName, []);

        Assert.Empty(options.CoinIds);
    }

    [Fact]
    public void CrestOptions_ConfiguredHosts_AppearExactlyOnce()
    {
        var options = Bind<CrestOptions>(
            CrestOptions.SectionName,
            Array(CrestOptions.SectionName, "AllowedHosts", "crests.football-data.org"));

        Assert.Equal(["crests.football-data.org"], options.AllowedHosts);
    }

    /// <summary>
    /// Leer heißt leer: eine leere Allowlist lässt keinen Host durch, statt auf einen Code-Wert
    /// zurückzufallen. Bei einer SSRF-Schranke ist das die einzig vertretbare Richtung.
    /// </summary>
    [Fact]
    public void CrestOptions_WithoutConfiguration_AllowsNothing()
    {
        var options = Bind<CrestOptions>(CrestOptions.SectionName, []);

        Assert.Empty(options.AllowedHosts);
    }

    /// <summary>
    /// Die verschachtelten Listen (Vereine, deren Wettbewerbe, Haltestellen) tragen ohnehin keinen
    /// Vorgabewert — der Test hält fest, dass sie sich beim Binden nicht verdoppeln.
    /// </summary>
    [Fact]
    public void NestedLists_BindConfiguredValuesOnce()
    {
        var football = Bind<FootballOptions>(
            FootballOptions.SectionName,
            new Dictionary<string, string?>
            {
                ["Football:Teams:0:Name"] = "HSV",
                ["Football:Teams:0:TeamId"] = "7",
                ["Football:Teams:0:CompetitionCode"] = "BL1",
                ["Football:Teams:0:Competitions:0"] = "BL1",
            });

        var team = Assert.Single(football.Teams);
        Assert.Equal("HSV", team.Name);
        Assert.Equal(["BL1"], team.Competitions);

        var hvv = Bind<HvvOptions>(
            HvvOptions.SectionName,
            new Dictionary<string, string?>
            {
                ["Hvv:Stations:0:Name"] = "Lühmannstraße",
                ["Hvv:Stations:0:MasterId"] = "Master:42026",
            });

        Assert.Single(hvv.Stations);
    }
}
