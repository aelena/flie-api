using System.CommandLine;
using Aelena.FileApi.Core.Errors;

namespace Aelena.FileApi.Cli.Helpers;

/// <summary>Wiring shared by every <c>fileapi</c> subcommand.</summary>
public static class CommandExtensions
{
    /// <summary>
    /// Attach a handler that reports failures as one readable line on stderr and a
    /// meaningful exit code.
    /// </summary>
    /// <remarks>
    /// Core signals every expected failure — unsupported format, out-of-range page,
    /// wrong PDF password — by throwing <see cref="FileApiException"/>. Without this
    /// wrapper those surfaced to the user as a .NET stack trace, which is both
    /// unreadable and indistinguishable from a genuine crash.
    /// </remarks>
    public static Command WithAction(this Command command, Action<ParseResult> body)
    {
        command.SetAction(parseResult =>
        {
            try
            {
                body(parseResult);
                return ExitCode.Success;
            }
            catch (FileApiException ex)
            {
                Output.Error(ex.Detail);
                return ExitCode.FromHttpStatus(ex.StatusCode);
            }
            catch (ArgumentException ex)
            {
                // Page ranges, mutually exclusive flags, and similar argument-shape
                // problems that Core validates rather than the parser.
                Output.Error(ex.Message);
                return ExitCode.UsageError;
            }
            catch (UnauthorizedAccessException ex)
            {
                Output.Error(ex.Message);
                Output.Hint("Check the file's permissions, or run from a directory you can write to.");
                return ExitCode.IoError;
            }
            catch (IOException ex)
            {
                Output.Error(ex.Message);
                return ExitCode.IoError;
            }
        });

        return command;
    }

    /// <summary>A required positional file argument that must already exist on disk.</summary>
    public static Argument<FileInfo> FileArgument(string description) =>
        new Argument<FileInfo>("file") { Description = description }.AcceptExistingOnly();

    /// <summary>The conventional <c>-o, --output</c> option.</summary>
    public static Option<string?> OutputOption(string description = "Output file path") =>
        new("--output", "-o") { Description = description };
}
