namespace Dashboard.Tests.Integration;

/// <summary>
/// A fact that only runs when RUN_INTEGRATION_TESTS=1 is set. The trait alone
/// would not exclude these tests from a plain <c>dotnet test</c>; this env gate
/// is what keeps the default local run Docker-free. CI opts in explicitly
/// (see the integration-tests job in ci.yml).
/// </summary>
public sealed class IntegrationFactAttribute : FactAttribute
{
    internal const string EnvVar = "RUN_INTEGRATION_TESTS";

    internal static bool Enabled => Environment.GetEnvironmentVariable(EnvVar) == "1";

    public IntegrationFactAttribute()
    {
        if (!Enabled)
        {
            Skip = $"Integration test — set {EnvVar}=1 to run (requires Docker).";
        }
    }
}
