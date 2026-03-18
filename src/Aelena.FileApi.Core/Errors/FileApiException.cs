namespace Aelena.FileApi.Core.Errors;

/// <summary>
/// Base exception that maps to an RFC 9457 Problem Details response.
/// Throw this from any service to produce a structured error with the
/// correct HTTP status code, title, and detail message.
/// </summary>
public sealed class FileApiException(
    int statusCode,
    string detail,
    string? title = null,
    string errorType = "about:blank") : Exception(detail)
{
    /// <summary>HTTP status code to return (e.g. 400, 404, 422).</summary>
    public int StatusCode { get; } = statusCode;

    /// <summary>Short human-readable title (defaults from status code).</summary>
    public string Title { get; } = title ?? DefaultTitle(statusCode);

    /// <summary>URI reference identifying the problem type (RFC 9457).</summary>
    public string ErrorType { get; } = errorType;

    /// <summary>Detailed human-readable explanation.</summary>
    public string Detail { get; } = detail;

    private static string DefaultTitle(int code) => code switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        410 => "Gone",
        413 => "Payload Too Large",
        415 => "Unsupported Media Type",
        422 => "Unprocessable Content",
        429 => "Too Many Requests",
        500 => "Internal Server Error",
        501 => "Not Implemented",
        _ => "Error"
    };
}
