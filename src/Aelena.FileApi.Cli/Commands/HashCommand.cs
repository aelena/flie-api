using System.CommandLine;
using Aelena.FileApi.Cli.Helpers;
using Aelena.FileApi.Core.Services.Common;

namespace Aelena.FileApi.Cli.Commands;

/// <summary><c>fileapi hash</c> — content and composite file fingerprints.</summary>
public static class HashCommand
{
    public static Command Create()
    {
        var fileArg = CommandExtensions.FileArgument("File to hash");
        var cmd = new Command("hash", "Compute SHA-256, MD5, SHA-1, and composite hashes") { fileArg };

        return cmd.WithAction(parse =>
        {
            var file = parse.GetRequiredValue(fileArg);
            var result = HashService.ComputeHash(Output.ReadFile(file), file.Name);

            Output.Properties($"Hash: {file.Name}",
                ("SHA-256", result.Sha256),
                ("MD5", result.Md5),
                ("SHA-1", result.Sha1),
                ("Composite SHA-256", result.CompositeSha256),
                ("File Size", $"{result.FileSizeBytes:N0} bytes"));
        });
    }
}
