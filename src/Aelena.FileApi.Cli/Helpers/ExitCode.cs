namespace Aelena.FileApi.Cli.Helpers;

/// <summary>
/// Process exit codes. Distinguishing "your input was wrong" from "we broke"
/// lets scripts branch on the outcome instead of parsing stderr.
/// </summary>
public static class ExitCode
{
    /// <summary>The operation completed.</summary>
    public const int Success = 0;

    /// <summary>The operation failed for an unexpected reason.</summary>
    public const int Failure = 1;

    /// <summary>The input file or the supplied arguments were rejected.</summary>
    public const int UsageError = 2;

    /// <summary>The requested operation is not implemented for this format.</summary>
    public const int Unsupported = 3;

    /// <summary>A file could not be read or written.</summary>
    public const int IoError = 4;

    /// <summary>
    /// Map the HTTP status code carried by a <see cref="Core.Errors.FileApiException"/>
    /// onto a shell exit code. Core is shared with the HTTP host, so its errors are
    /// expressed as status codes even when nothing is being served over HTTP.
    /// </summary>
    public static int FromHttpStatus(int statusCode) => statusCode switch
    {
        501 => Unsupported,
        >= 400 and < 500 => UsageError,
        _ => Failure
    };
}
