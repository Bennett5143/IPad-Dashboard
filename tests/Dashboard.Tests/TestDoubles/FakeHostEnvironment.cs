using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Dashboard.Tests.TestDoubles;

/// <summary>Minimaler <see cref="IHostEnvironment"/> für Tests (nur <see cref="ContentRootPath"/> relevant).</summary>
internal sealed class FakeHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Test";
    public string ApplicationName { get; set; } = "Dashboard.Tests";
    public string ContentRootPath { get; set; } = "";
    public IFileProvider ContentRootFileProvider { get; set; } = null!;
}
