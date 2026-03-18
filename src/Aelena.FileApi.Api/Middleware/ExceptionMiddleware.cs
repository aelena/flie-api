using System.Net.Mime;

namespace Aelena.FileApi.Api.Middleware;

/// <summary>
/// Catches exceptions and converts them to RFC 9457 Problem Details JSON responses.
/// <see cref="FileApiException"/> maps to its declared status code; all other
/// exceptions produce a generic 500.
/// </summary>
public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> log)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (FileApiException ex)
        {
            log.LogWarning(ex, "FileApiException {StatusCode}: {Detail}", ex.StatusCode, ex.Detail);
            await WriteProblem(ctx, ex.StatusCode, ex.Title, ex.Detail, ex.ErrorType);
        }
        catch (BadHttpRequestException ex)
        {
            log.LogWarning(ex, "Bad request: {Message}", ex.Message);
            await WriteProblem(ctx, 400, "Bad Request", ex.Message);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Unhandled exception on {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
            await WriteProblem(ctx, 500, "Internal Server Error", "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblem(
        HttpContext ctx, int status, string title, string detail, string type = "about:blank")
    {
        if (ctx.Response.HasStarted) return;

        var problem = new ProblemDetail(type, title, status, detail, ctx.Request.Path.Value);
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/problem+json";
        await ctx.Response.WriteAsJsonAsync(problem);
    }
}
