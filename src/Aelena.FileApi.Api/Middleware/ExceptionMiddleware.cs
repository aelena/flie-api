using Aelena.FileApi.Api.Logging;

namespace Aelena.FileApi.Api.Middleware;

/// <summary>
/// Catches exceptions and converts them to RFC 9457 Problem Details JSON responses.
/// <see cref="FileApiException"/> maps to its declared status code; all other
/// exceptions produce a generic 500.
/// </summary>
public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> log)
{
    /// <summary>The media type RFC 9457 requires for a problem document.</summary>
    public const string ProblemJson = "application/problem+json";

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (FileApiException ex)
        {
            LogMessages.HandledFailure(
                log, ex, ex.StatusCode, ctx.Request.Method, ctx.Request.Path.Value, ex.Detail);
            await WriteProblem(ctx, ex.StatusCode, ex.Title, ex.Detail, ex.ErrorType);
        }
        catch (BadHttpRequestException ex)
        {
            LogMessages.HandledFailure(
                log, ex, StatusCodes.Status400BadRequest, ctx.Request.Method, ctx.Request.Path.Value, ex.Message);
            await WriteProblem(ctx, StatusCodes.Status400BadRequest, "Bad Request", ex.Message);
        }
        catch (OperationCanceledException) when (ctx.RequestAborted.IsCancellationRequested)
        {
            // The caller hung up. There is nobody left to answer, and this is not a
            // fault worth logging at error level alongside genuine failures.
        }
        catch (Exception ex)
        {
            LogMessages.UnhandledException(log, ex, ctx.Request.Method, ctx.Request.Path.Value);
            await WriteProblem(ctx, StatusCodes.Status500InternalServerError, "Internal Server Error",
                "An unexpected error occurred.");
        }
    }

    /// <summary>Write an RFC 9457 problem document.</summary>
    /// <remarks>
    /// The content type is passed to <c>WriteAsJsonAsync</c> rather than assigned
    /// beforehand: assigning it first looks right but is overwritten, so every error
    /// response went out as <c>application/json</c> and no client could distinguish a
    /// problem document from a normal payload by media type alone.
    /// </remarks>
    private static async Task WriteProblem(
        HttpContext ctx, int status, string title, string detail, string type = "about:blank")
    {
        if (ctx.Response.HasStarted) return;

        ctx.Response.StatusCode = status;

        var problem = new ProblemDetail(type, title, status, detail, ctx.Request.Path.Value);
        await ctx.Response.WriteAsJsonAsync(problem, options: null, contentType: ProblemJson, ctx.RequestAborted);
    }
}
