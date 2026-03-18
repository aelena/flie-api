using System.CommandLine;
using Aelena.FileApi.Cli.Helpers;
using Aelena.FileApi.Core.Services.Common;

namespace Aelena.FileApi.Cli.Commands;

public static class TxtCommand
{
    public static Command Create()
    {
        var cmd = new Command("txt", "Plain text operations — metrics and search");
        cmd.AddCommand(Metrics());
        cmd.AddCommand(Search());
        return cmd;
    }

    private static Command Metrics()
    {
        var fileArg = new Argument<FileInfo>("file", "Text file");
        var cmd = new Command("metrics", "Get line, word, and token counts") { fileArg };
        cmd.SetHandler(file =>
        {
            var data = Output.ReadFileWithSpinner(file.FullName);
            var m = TxtService.GetMetrics(data, file.Name);
            Output.Properties($"TXT Metrics: {file.Name}",
                ("Lines", m.LineCount.ToString()),
                ("Words", m.WordCount.ToString("N0")),
                ("Characters", m.CharCount.ToString("N0")),
                ("Tokens", m.TokenCount.ToString("N0")),
                ("Language", m.Language),
                ("Size", $"{m.FileSizeBytes:N0} bytes"));
        }, fileArg);
        return cmd;
    }

    private static Command Search()
    {
        var fileArg = new Argument<FileInfo>("file", "Text file");
        var queryOpt = new Option<string?>("--query", "Literal search text");
        var patternOpt = new Option<string?>("--pattern", "Regex pattern");
        var cmd = new Command("search", "Search for text") { fileArg, queryOpt, patternOpt };
        cmd.SetHandler((file, query, pattern) =>
        {
            var data = Output.ReadFileWithSpinner(file.FullName);
            var (_, matches) = TxtService.Search(data, file.Name, query, pattern);
            Output.Success($"{matches.Count} match(es) found");
            foreach (var m in matches)
                Console.WriteLine($"  [{m.Start}-{m.End}] {m.Match}");
        }, fileArg, queryOpt, patternOpt);
        return cmd;
    }
}
