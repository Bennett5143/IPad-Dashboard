using System.Text.Json;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Dashboard.Web.Infrastructure;

public static class HealthCheckResponseWriter
{
    public static async Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        // Exception-Messages können Infrastruktur-Details leaken (z. B. nennt Npgsql
        // bei "password authentication failed" den DB-User). Der Endpoint ist LAN-weit
        // ohne Auth erreichbar, daher Details nur in Development ausgeben.
        var includeErrorDetails = context.RequestServices
            .GetRequiredService<IHostEnvironment>().IsDevelopment();

        var payload = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds,
                description = e.Value.Description,
                error = includeErrorDetails ? e.Value.Exception?.Message : null
            })
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(payload,
                new JsonSerializerOptions { WriteIndented = true }));
    }
}
