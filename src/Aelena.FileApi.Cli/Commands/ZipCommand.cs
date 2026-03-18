using System.CommandLine;
using Aelena.FileApi.Cli.Helpers;
using Aelena.FileApi.Core.Services.Common;
using Spectre.Console;

namespace Aelena.FileApi.Cli.Commands;

public static class ZipCommand
{
    public static Command Create()
    {
        var fileArg = new Argument<FileInfo>("file", "ZIP archive");
        var cmd = new Command("zip", "Inspect ZIP archive contents") { fileArg };

        cmd.SetHandler(file =>
        {
            var data = Output.ReadFileWithSpinner(file.FullName);
            var z = ZipService.Inspect(data, file.Name);

            var table = new Table().Border(TableBorder.Rounded)
                .Title($"[bold]ZIP: {Markup.Escape(file.Name)}[/] ({z.TotalFiles} files, {z.TotalDirs} dirs)")
                .AddColumn("Name").AddColumn("Size").AddColumn("Compressed").AddColumn("Method");

            foreach (var e in z.Entries)
                table.AddRow(
                    Markup.Escape(e.Filename),
                    e.IsDir ? "-" : $"{e.FileSize:N0}",
                    e.IsDir ? "-" : $"{e.CompressedSize:N0}",
                    e.CompressionMethod);

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"Total uncompressed: [bold]{z.TotalUncompressedSize:N0}[/] bytes");
        }, fileArg);

        return cmd;
    }
}
