using System.Collections.Concurrent;
using Aelena.FileApi.Api.Auth;
using Aelena.FileApi.Api.Configuration;
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

    // _dailyCounts[date][appId] = count
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
            log.LogWarning("Rate limit exceeded for {AppId}", appId);
            await WriteProblem(ctx, 429, "Too Many Requests",
                $"Daily request limit ({cfg.MaxRequestsPerDay}) exceeded");
            return;
        }

        // ── Per-file size limit (Content-Length approximation) ───────
        if (!AppSettings.IsUnlimited(cfg.MaxFileSizeBytes) && ctx.Request.ContentLength is { } cl)
        {
            if (cl > cfg.MaxFileSizeBytes)
            {
                await WriteProblem(ctx, 400, "Bad Request",
                    $"Request body ({cl} bytes) exceeds MAX_FILE_SIZE_BYTES ({cfg.MaxFileSizeBytes})");
                return;
            }
        }

        ctx.Items["app_id"] = appId;
        await next(ctx);
    }

    private static bool IsPublicPath(string path) =>
        PublicPaths.Contains(path) ||
        path.StartsWith("/docs", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/redoc", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase);

    private static bool IncrementAndCheck(string appId, int maxPerDay)
    {
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var bucket = DailyCounts.GetOrAdd(today, _ => new ConcurrentDictionary<string, int>());
        var count = bucket.AddOrUpdate(appId, 1, (_, c) => c + 1);
        return count <= maxPerDay;
    }

    private static async Task WriteProblem(HttpContext ctx, int status, string title, string detail)
    {
        var problem = new ProblemDetail("about:blank", title, status, detail, ctx.Request.Path.Value);
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/problem+json";
        await ctx.Response.WriteAsJsonAsync(problem);
    }
}
