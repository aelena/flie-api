using System.Diagnostics;

namespace Aelena.FileApi.Api.Middleware;

/// <summary>
/// Logs every mutating request (POST/PUT/PATCH) with timing, file metadata, and status.
/// Uses Serilog structured logging so entries flow to both the JSONL audit file
/// and OpenTelemetry log exporter.
/// </summary>
public sealed class AuditLogMiddleware(RequestDelegate next, ILogger<AuditLogMiddleware> log)
{
    private static readonly HashSet<string> AuditedMethods = ["POST", "PUT", "PATCH"];

    public async Task InvokeAsync(HttpContext ctx)
    {
        var sw = Stopwatch.StartNew();

        // Initialise audit state so downstream handlers can set file info
        ctx.Items["audit_file_name"] = null;
        ctx.Items["audit_file_size"] = null;

        await next(ctx);

        sw.Stop();

        if (!AuditedMethods.Contains(ctx.Request.Method)) return;

        var fileSize = ctx.Items["audit_file_size"] as long?;
        if (fileSize is null && ctx.Request.ContentLength is { } cl)
            fileSize = cl;

        var fileName = ctx.Items["audit_file_name"] as string;
        var error = ctx.Response.StatusCode >= 400 ? $"HTTP {ctx.Response.StatusCode}" : null;

        log.LogInformation(
            "Audit: {Method} {Path} | File={FileName} Size={FileSize} | Status={StatusCode} Duration={DurationMs:F1}ms Error={Error}",
            ctx.Request.Method,
            ctx.Request.Path.Value,
            fileName,
            fileSize,
            ctx.Response.StatusCode,
            sw.Elapsed.TotalMilliseconds,
            error);
    }
}
