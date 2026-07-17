namespace Dashboard.Web.Components.Layout;

/// <summary>
/// Hub-and-spoke navigation model: five isolated areas reachable only from Home,
/// return always via Home (PRD section 4). Only Fitness bundles several sub-pages
/// that may navigate among themselves; every other area is a single spoke.
/// </summary>
public static class Area
{
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

    /// <summary>True when the current relative path belongs to the Fitness area.</summary>
    public static bool IsFitness(string relativePath)
    {
        var path = Normalize(relativePath);
        return FitnessPrefixes.Any(p =>
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
