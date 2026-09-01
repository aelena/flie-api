using System.Diagnostics;
using Aelena.FileApi.Api.Logging;

namespace Aelena.FileApi.Api.Middleware;

/// <summary>
/// Logs every mutating request (POST/PUT/PATCH) with timing, file metadata, and status.
/// Uses Serilog structured logging so entries flow to both the JSONL audit file
/// and OpenTelemetry log exporter.
/// </summary>
public sealed class AuditLogMiddleware(RequestDelegate next, ILogger<AuditLogMiddleware> log)
{
    /// <summary>Key under which a handler may record the name of the file it processed.</summary>
    public const string FileNameItem = "audit_file_name";

    /// <summary>Key under which a handler may record the size of the file it processed.</summary>
    public const string FileSizeItem = "audit_file_size";

    private static readonly HashSet<string> AuditedMethods = ["POST", "PUT", "PATCH"];

    public async Task InvokeAsync(HttpContext ctx)
    {
        if (!AuditedMethods.Contains(ctx.Request.Method))
        {
            await next(ctx);
            return;
        }

        var started = Stopwatch.GetTimestamp();
        string? failure = null;

        try
        {
            await next(ctx);
        }
        catch (Exception ex)
        {
            // The audit entry has to be written for failures above all — those are the
            // requests anyone reviewing the log is looking for. Without this the record
            // was simply lost: the exception unwound straight past the logging call
            // below into ExceptionMiddleware, so no failed request was ever audited.
            failure = $"{ex.GetType().Name}: {ex.Message}";
            throw;
        }
        finally
        {
            failure ??= ctx.Response.StatusCode >= 400
                ? $"HTTP {ctx.Response.StatusCode}"
                : null;

            LogMessages.Audit(
                log,
                ctx.Request.Method,
                ctx.Request.Path.Value,
                ctx.Items[FileNameItem] as string,
                ctx.Items[FileSizeItem] as long? ?? ctx.Request.ContentLength,
                ctx.Response.StatusCode,
                Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                failure);
        }
    }
}
