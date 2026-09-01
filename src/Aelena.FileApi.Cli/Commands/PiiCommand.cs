using System.CommandLine;
using System.Text;
using Aelena.FileApi.Cli.Helpers;
using Aelena.FileApi.Core.Services.Common;
using Spectre.Console;

namespace Aelena.FileApi.Cli.Commands;

/// <summary><c>fileapi pii</c> — scan documents for personal data.</summary>
public static class PiiCommand
{
    public static Command Create()
    {
        var cmd = new Command("pii", "PII detection — scan for personal data");
        cmd.Add(Detect());
        return cmd;
    }

    private static Command Detect()
    {
        var fileArg = CommandExtensions.FileArgument("File to scan");
        var cmd = new Command("detect", "Detect PII in a file") { fileArg };

        return cmd.WithAction(parse =>
        {
            var file = parse.GetRequiredValue(fileArg);
            var text = Encoding.UTF8.GetString(Output.ReadFile(file));
            var report = PiiService.Detect(text, file.Name);

            if (report.TotalMatches == 0)
            {
                Output.Success("No PII detected.");
                return;
            }

            AnsiConsole.MarkupLine($"[yellow]Found {report.TotalMatches} PII match(es)[/]");

            var table = new Table().Border(TableBorder.Rounded)
                .AddColumn("Type").AddColumn("Value").AddColumn("Country").AddColumn("Position");
            foreach (var match in report.Matches)
                table.AddRow(
                    Markup.Escape(match.PiiType),
                    Markup.Escape(match.Value),
                    Markup.Escape(match.Country ?? "-"),
                    $"{match.Start}-{match.End}");
            AnsiConsole.Write(table);

            Output.Properties("Summary by type",
                [.. report.ByType.Select(kv => (kv.Key, (string?)kv.Value.Display()))]);
        });
    }
}
