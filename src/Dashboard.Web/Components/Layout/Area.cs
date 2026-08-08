namespace Dashboard.Web.Components.Layout;

/// <summary>
/// The content areas of the dashboard. Every area is reachable from Home and — via the
/// subpage rail — directly from any other subpage; the earlier hub-and-spoke isolation
/// is gone. Fitness is the one area that bundles several sub-pages, reachable through a
/// tab row in the page body rather than a group in the rail.
/// </summary>
public static class Area
{
    /// <summary>One entry per rail item: where it goes, what it is called, which glyph it wears.</summary>
    /// <param name="Href">Landing page of the area.</param>
    /// <param name="Label">Accessible name — the rail itself stays textless.</param>
    /// <param name="Icon">Glyph shared with the home rail.</param>
    /// <param name="Prefixes">Path prefixes that count as "inside this area".</param>
    public sealed record Entry(string Href, string Label, AreaIconName Icon, string[] Prefixes);

    /// <summary>The Fitness sub-pages (the one multi-page area).</summary>
    public static readonly (string Href, string Label)[] Fitness =
    {
        ("/whoop", "WHOOP"),
        ("/runs", "Läufe"),
        ("/heatmap", "Heatmap"),
        ("/habits", "Habits"),
    };

    private static readonly string[] FitnessPrefixes =
        { "/whoop", "/runs", "/heatmap", "/habits" };

    /// <summary>The rail areas in display order, top to bottom. Status hangs off the health pulse
    /// and is pinned to the bottom by the rail, so it is kept out of this list.</summary>
    public static readonly Entry[] All =
    {
        new("/hvv", "Abfahrten", AreaIconName.Hvv, new[] { "/hvv" }),
        new("/whoop", "Fitness", AreaIconName.Fitness, FitnessPrefixes),
        new("/football", "Fußball", AreaIconName.Football, new[] { "/football" }),
        new("/crypto", "Krypto", AreaIconName.Crypto, new[] { "/crypto" }),
        new("/weather", "Wetter", AreaIconName.Weather, new[] { "/weather" }),
    };

    /// <summary>The Status area behind the health pulse.</summary>
    public static readonly Entry Status =
        new("/status", "Status", AreaIconName.Health, new[] { "/status" });

    /// <summary>True when <paramref name="relativePath"/> sits inside <paramref name="entry"/>.
    /// Used for the rail's active marking, which NavLink cannot express: Fitness spans four
    /// disjoint prefixes.</summary>
    public static bool IsActive(Entry entry, string relativePath) => Contains(entry.Prefixes, relativePath);

    private static bool Contains(string[] prefixes, string relativePath)
    {
        var path = Normalize(relativePath);
        return prefixes.Any(p =>
            path.Equals(p, StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(p + "/", StringComparison.OrdinalIgnoreCase));
    }

    private static string Normalize(string relativePath)
    {
        var path = relativePath;
        var cut = path.IndexOfAny(new[] { '?', '#' });
        if (cut >= 0) path = path[..cut];
        if (!path.StartsWith('/')) path = "/" + path;
        return path;
    }
}
