using System.CommandLine;
using System.Text;
using Aelena.FileApi.Cli.Helpers;
using Aelena.FileApi.Core.Services.Common;
using Spectre.Console;

namespace Aelena.FileApi.Cli.Commands;

public static class PiiCommand
{
    public static Command Create()
    {
        var cmd = new Command("pii", "PII detection — scan for personal data");

        var fileArg = new Argument<FileInfo>("file", "File to scan");
        var detectCmd = new Command("detect", "Detect PII in a file") { fileArg };

        detectCmd.SetHandler(file =>
        {
            var data = Output.ReadFileWithSpinner(file.FullName);
            var text = Encoding.UTF8.GetString(data);
            var r = PiiService.Detect(text, file.Name);

            if (r.TotalMatches == 0)
            {
                Output.Success("No PII detected.");
                return;
            }

            AnsiConsole.MarkupLine($"[yellow]Found {r.TotalMatches} PII match(es)[/]");

            var table = new Table().Border(TableBorder.Rounded)
                .AddColumn("Type").AddColumn("Value").AddColumn("Country").AddColumn("Position");
            foreach (var m in r.Matches)
                table.AddRow(
                    Markup.Escape(m.PiiType),
                    Markup.Escape(m.Value),
                    Markup.Escape(m.Country ?? "-"),
                    $"{m.Start}-{m.End}");
            AnsiConsole.Write(table);

            Output.Properties("Summary by type", r.ByType.Select(kv => (kv.Key, (string?)kv.Value.ToString())).ToArray());
        }, fileArg);

        cmd.AddCommand(detectCmd);
        return cmd;
    }
}
