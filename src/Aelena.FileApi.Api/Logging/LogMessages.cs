namespace Aelena.FileApi.Api.Logging;

/// <summary>
/// Every log statement the Api layer emits, as compile-time generated
/// <c>LoggerMessage</c> delegates.
/// </summary>
/// <remarks>
/// Source-generated logging avoids the boxing and format-string parsing that the
/// <c>ILogger.LogXxx(string, params object?[])</c> extensions pay on every call —
/// including calls that are filtered out by level and produce no output at all.
/// Collecting the messages here also keeps event IDs unique and greppable.
/// </remarks>
internal static partial class LogMessages
{
    // ── 1000–1099: request audit ─────────────────────────────────────────

    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "Audit: {Method} {Path} | File={FileName} Size={FileSize} | "
                + "Status={StatusCode} Duration={DurationMs:F1}ms Error={Error}")]
    public static partial void Audit(
        ILogger logger,
        string method,
        string? path,
        string? fileName,
        long? fileSize,
        int statusCode,
        double durationMs,
        string? error);

    // ── 1100–1199: request rejection ─────────────────────────────────────

    [LoggerMessage(
        EventId = 1100,
        Level = LogLevel.Warning,
        Message = "Daily request limit of {Limit} exceeded by {AppId}")]
    public static partial void RateLimitExceeded(ILogger logger, string appId, int limit);

    [LoggerMessage(
        EventId = 1101,
        Level = LogLevel.Warning,
        Message = "Rejected {ContentLength}-byte body on {Path}; limit is {Limit} bytes")]
    public static partial void RequestTooLarge(
        ILogger logger, string? path, long contentLength, long limit);

    // ── 1200–1299: error handling ────────────────────────────────────────

    [LoggerMessage(
        EventId = 1200,
        Level = LogLevel.Warning,
        Message = "Handled {StatusCode} on {Method} {Path}: {Detail}")]
    public static partial void HandledFailure(
        ILogger logger, Exception exception, int statusCode, string method, string? path, string detail);

    [LoggerMessage(
        EventId = 1201,
        Level = LogLevel.Error,
        Message = "Unhandled exception on {Method} {Path}")]
    public static partial void UnhandledException(
        ILogger logger, Exception exception, string method, string? path);

    // ── 1300–1399: webhooks ──────────────────────────────────────────────

    [LoggerMessage(
        EventId = 1300,
        Level = LogLevel.Information,
        Message = "Webhook {Url} responded {StatusCode}")]
    public static partial void WebhookDelivered(ILogger logger, string url, int statusCode);

    [LoggerMessage(
        EventId = 1301,
        Level = LogLevel.Warning,
        Message = "Webhook delivery failed for {Url}")]
    public static partial void WebhookFailed(ILogger logger, Exception exception, string url);
}
