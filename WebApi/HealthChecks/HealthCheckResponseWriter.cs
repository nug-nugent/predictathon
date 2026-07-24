using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace Predictathon.WebApi.HealthChecks;

/// <summary>
/// Renders a <see cref="HealthReport"/> for the "/health" and "/health/detailed" endpoints.
/// </summary>
public static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Writes the overall result as a bare "true"/"false" plain-text body - no check names,
    /// timings, or other detail - so an external uptime monitor can keyword-match on an exact,
    /// unchanging result without the response revealing anything about the app's internals.
    /// </summary>
    /// <param name="context">The HTTP context to write the response to.</param>
    /// <param name="report">The health report produced by the health check middleware.</param>
    public static Task WriteBooleanAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "text/plain";

        return context.Response.WriteAsync(report.Status == HealthStatus.Healthy ? "true" : "false");
    }

    /// <summary>
    /// Writes the overall status and a per-check breakdown as the JSON response body, for
    /// diagnosing which specific dependency is failing.
    /// </summary>
    /// <param name="context">The HTTP context to write the response to.</param>
    /// <param name="report">The health report produced by the health check middleware.</param>
    public static Task WriteDetailedAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                durationMs = entry.Value.Duration.TotalMilliseconds,
                description = entry.Value.Description
            })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload, SerializerOptions));
    }
}
