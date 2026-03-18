using System.CommandLine;
using Aelena.FileApi.Cli.Helpers;
using Aelena.FileApi.Core.Services.Common;

namespace Aelena.FileApi.Cli.Commands;

public static class HashCommand
{
    public static Command Create()
    {
        var fileArg = new Argument<FileInfo>("file", "File to hash");
        var cmd = new Command("hash", "Compute SHA-256, MD5, SHA-1, and composite hashes") { fileArg };

        cmd.SetHandler(file =>
        {
            var data = Output.ReadFileWithSpinner(file.FullName);
            var result = HashService.ComputeHash(data, file.Name);

            Output.Properties($"Hash: {file.Name}",
                ("SHA-256", result.Sha256),
                ("MD5", result.Md5),
                ("SHA-1", result.Sha1),
                ("Composite SHA-256", result.CompositeSha256),
                ("File Size", $"{result.FileSizeBytes:N0} bytes"));
        }, fileArg);

        return cmd;
    }
}
