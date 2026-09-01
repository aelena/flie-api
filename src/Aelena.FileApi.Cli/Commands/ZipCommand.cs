using System.CommandLine;
using Aelena.FileApi.Cli.Helpers;
using Aelena.FileApi.Core.Services.Common;
using Spectre.Console;

namespace Aelena.FileApi.Cli.Commands;

/// <summary><c>fileapi zip</c> — list an archive's contents without extracting it.</summary>
public static class ZipCommand
{
    public static Command Create()
    {
        var fileArg = CommandExtensions.FileArgument("ZIP archive");
        var cmd = new Command("zip", "Inspect ZIP archive contents") { fileArg };

        return cmd.WithAction(parse =>
        {
            var file = parse.GetRequiredValue(fileArg);
            var archive = ZipService.Inspect(Output.ReadFile(file), file.Name);

            var table = new Table().Border(TableBorder.Rounded)
                .Title($"[bold]ZIP: {Markup.Escape(file.Name)}[/] ({archive.TotalFiles} files, {archive.TotalDirs} dirs)")
                .AddColumn("Name").AddColumn("Size").AddColumn("Compressed").AddColumn("Method");

            foreach (var entry in archive.Entries)
                table.AddRow(
                    Markup.Escape(entry.Filename),
                    entry.IsDir ? "-" : $"{entry.FileSize:N0}",
                    entry.IsDir ? "-" : $"{entry.CompressedSize:N0}",
                    Markup.Escape(entry.CompressionMethod));

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"Total uncompressed: [bold]{archive.TotalUncompressedSize:N0}[/] bytes");
        });
    }
}
