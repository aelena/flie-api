using System.CommandLine;
using Aelena.FileApi.Cli.Helpers;
using Aelena.FileApi.Core.Services.Common;

namespace Aelena.FileApi.Cli.Commands;

/// <summary><c>fileapi txt</c> — metrics and search over plain text.</summary>
public static class TxtCommand
{
    public static Command Create()
    {
        var cmd = new Command("txt", "Plain text operations — metrics and search");
        cmd.Add(Metrics());
        cmd.Add(Search());
        return cmd;
    }

    private static Command Metrics()
    {
        var fileArg = CommandExtensions.FileArgument("Text file");
        var cmd = new Command("metrics", "Get line, word, and token counts") { fileArg };

        return cmd.WithAction(parse =>
        {
            var file = parse.GetRequiredValue(fileArg);
            var metrics = TxtService.GetMetrics(Output.ReadFile(file), file.Name);

            Output.Properties($"TXT Metrics: {file.Name}",
                ("Lines", metrics.LineCount.Display()),
                ("Words", metrics.WordCount.Display("N0")),
                ("Characters", metrics.CharCount.Display("N0")),
                ("Tokens", metrics.TokenCount.Display("N0")),
                ("Language", metrics.Language),
                ("Size", $"{metrics.FileSizeBytes:N0} bytes"));
        });
    }

    private static Command Search()
    {
        var fileArg = CommandExtensions.FileArgument("Text file");
        var queryOpt = new Option<string?>("--query") { Description = "Literal search text" };
        var patternOpt = new Option<string?>("--pattern") { Description = "Regular expression to match" };
        var cmd = new Command("search", "Search for text") { fileArg, queryOpt, patternOpt };

        return cmd.WithAction(parse =>
        {
            var file = parse.GetRequiredValue(fileArg);
            var (_, matches) = TxtService.Search(
                Output.ReadFile(file), file.Name,
                parse.GetValue(queryOpt), parse.GetValue(patternOpt));

            Output.Success($"{matches.Count} match(es) found");
            foreach (var match in matches)
                Console.WriteLine($"  [{match.Start}-{match.End}] {match.Match}");
        });
    }
}
