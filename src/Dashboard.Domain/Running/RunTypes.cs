namespace Dashboard.Domain.Running;

/// <summary>Welche Strava-Activity-Typen als Lauf gelten (FA-8.06): nur <c>Run</c> und <c>TrailRun</c>.</summary>
public static class RunTypes
{
    private static readonly HashSet<string> Allowed = new(StringComparer.Ordinal) { "Run", "TrailRun" };

    public static bool IsRun(string? type) => type is not null && Allowed.Contains(type);
}
