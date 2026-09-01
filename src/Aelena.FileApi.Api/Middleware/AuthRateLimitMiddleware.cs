using System.Collections.Concurrent;
using System.Globalization;
using Aelena.FileApi.Api.Auth;
using Aelena.FileApi.Api.Configuration;
using Aelena.FileApi.Api.Logging;
using Microsoft.Extensions.Options;

namespace Aelena.FileApi.Api.Middleware;

/// <summary>
/// Extracts the JWT user from the <c>auth_token</c> cookie, enforces
/// daily request limits and per-file size limits.
/// Public paths (<c>/health</c>, <c>/docs</c>, etc.) are exempt.
/// </summary>
public sealed class AuthRateLimitMiddleware(
    RequestDelegate next,
    IOptions<AppSettings> settings,
    ILogger<AuthRateLimitMiddleware> log)
{
    private static readonly HashSet<string> PublicPaths =
        ["/health", "/docs", "/openapi.json", "/redoc", "/api/auth/set-cookie", "/swagger"];

    // DailyCounts[date][appId] = count
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, int>> DailyCounts = new();

    public async Task InvokeAsync(HttpContext ctx)
    {
        var path = ctx.Request.Path.Value ?? "";

        if (IsPublicPath(path))
        {
            await next(ctx);
            return;
        }

        var cfg = settings.Value;

        // ── Extract user from cookie ─────────────────────────────────
        var user = JwtCookieAuth.GetUserFromCookie(ctx.Request, cfg);
        ctx.Items["user_email"] = user?.Email;
        ctx.Items["user_id"] = user?.UserId;

        var appId = user?.Email ?? "anonymous";

        // ── Daily request rate limit ─────────────────────────────────
        if (!AppSettings.IsUnlimited(cfg.MaxRequestsPerDay) && !IncrementAndCheck(appId, cfg.MaxRequestsPerDay))
        {
            LogMessages.RateLimitExceeded(log, appId, cfg.MaxRequestsPerDay);
            await WriteProblem(ctx, StatusCodes.Status429TooManyRequests, "Too Many Requests",
                $"You have used your daily quota of {cfg.MaxRequestsPerDay} requests. "
                + "The quota resets at 00:00 UTC.");
            return;
        }

        // ── Per-file size limit (Content-Length approximation) ───────
        if (!AppSettings.IsUnlimited(cfg.MaxFileSizeBytes)
            && ctx.Request.ContentLength is { } contentLength
            && contentLength > cfg.MaxFileSizeBytes)
        {
            LogMessages.RequestTooLarge(log, path, contentLength, cfg.MaxFileSizeBytes);

            // 413, not 400: the request is well-formed, it is simply too big. The old
            // 400 also quoted the MAX_FILE_SIZE_BYTES environment variable, which means
            // nothing to an API caller who cannot see the server's configuration.
            await WriteProblem(ctx, StatusCodes.Status413PayloadTooLarge, "Payload Too Large",
                $"The upload is {Megabytes(contentLength)} and the limit is {Megabytes(cfg.MaxFileSizeBytes)}.");
            return;
        }

        ctx.Items["app_id"] = appId;
        await next(ctx);
    }

    private static string Megabytes(long bytes) =>
        (bytes / (1024d * 1024d)).ToString("F1", CultureInfo.InvariantCulture) + " MB";

    private static bool IsPublicPath(string path) =>
        PublicPaths.Contains(path) ||
        path.StartsWith("/docs", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/redoc", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase);

    /// <summary>Count this request against the caller's daily quota.</summary>
    /// <remarks>
    /// Yesterday's buckets are dropped when the date rolls over. Without this the
    /// dictionary gained a permanent entry per day per caller and never released one:
    /// a long-running process leaked every counter it had ever created.
    /// </remarks>
    private static bool IncrementAndCheck(string appId, int maxPerDay)
    {
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var bucket = DailyCounts.GetOrAdd(today, _ => new ConcurrentDictionary<string, int>());

        if (DailyCounts.Count > 1)
        {
            foreach (var stale in DailyCounts.Keys.Where(k => !string.Equals(k, today, StringComparison.Ordinal)))
                DailyCounts.TryRemove(stale, out _);
        }

        return bucket.AddOrUpdate(appId, 1, (_, count) => count + 1) <= maxPerDay;
    }

    private static async Task WriteProblem(HttpContext ctx, int status, string title, string detail)
    {
        if (ctx.Response.HasStarted) return;

        ctx.Response.StatusCode = status;

        var problem = new ProblemDetail("about:blank", title, status, detail, ctx.Request.Path.Value);
        await ctx.Response.WriteAsJsonAsync(problem, options: null, contentType: ExceptionMiddleware.ProblemJson, ctx.RequestAborted);
    }
}
