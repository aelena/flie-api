using System.CommandLine;
using Aelena.FileApi.Cli.Helpers;
using Aelena.FileApi.Core.Services.Docx;
using Spectre.Console;

namespace Aelena.FileApi.Cli.Commands;

/// <summary><c>fileapi docx</c> — metrics, metadata, Markdown conversion, health.</summary>
public static class DocxCommand
{
    public static Command Create()
    {
        var cmd = new Command("docx", "DOCX operations — metrics, metadata, markdown, health");
        cmd.Add(Metrics());
        cmd.Add(Metadata());
        cmd.Add(Markdown());
        cmd.Add(Health());
        return cmd;
    }

    private static Command Metrics()
    {
        var fileArg = CommandExtensions.FileArgument("DOCX file");
        var cmd = new Command("metrics", "Get document metrics") { fileArg };

        return cmd.WithAction(parse =>
        {
            var file = parse.GetRequiredValue(fileArg);
            var metrics = DocxService.GetMetrics(Output.ReadFile(file), file.Name);

            Output.Properties($"DOCX Metrics: {file.Name}",
                ("Paragraphs", metrics.ParagraphCount.Display()),
                ("Words", metrics.WordCount.Display("N0")),
                ("Tables", metrics.TableCount.Display()),
                ("Images", metrics.ImageCount.Display()),
                ("Pages", metrics.PageCount?.Display() ?? "unknown"),
                ("Language", metrics.Language),
                ("Size", $"{metrics.FileSizeBytes:N0} bytes"));
        });
    }

    private static Command Metadata()
    {
        var fileArg = CommandExtensions.FileArgument("DOCX file");
        var cmd = new Command("metadata", "Extract metadata") { fileArg };

        return cmd.WithAction(parse =>
        {
            var file = parse.GetRequiredValue(fileArg);
            var meta = DocxService.GetMetadata(Output.ReadFile(file), file.Name);

            Output.Properties($"DOCX Metadata: {file.Name}",
                ("Title", meta.Title), ("Author", meta.Author), ("Subject", meta.Subject),
                ("Keywords", meta.Keywords), ("Category", meta.Category),
                ("Created", meta.Created), ("Modified", meta.Modified),
                ("Revision", meta.Revision?.Display()));
        });
    }

    private static Command Markdown()
    {
        var fileArg = CommandExtensions.FileArgument("DOCX file");
        var cmd = new Command("markdown", "Convert to Markdown") { fileArg };

        return cmd.WithAction(parse =>
        {
            var file = parse.GetRequiredValue(fileArg);
            Console.WriteLine(DocxService.ExtractToMarkdown(Output.ReadFile(file), file.Name).Markdown);
        });
    }

    private static Command Health()
    {
        var fileArg = CommandExtensions.FileArgument("DOCX file");
        var cmd = new Command("health", "Run health check") { fileArg };

        return cmd.WithAction(parse =>
        {
            var file = parse.GetRequiredValue(fileArg);
            var health = DocxService.HealthCheck(Output.ReadFile(file), file.Name);

            var status = health.Healthy ? "[green]Healthy[/]" : "[red]Issues found[/]";
            AnsiConsole.MarkupLine($"Status: {status}");
            if (health.Issues.Count > 0)
                Output.List("Issues", health.Issues.Select(i => $"[{i.Severity}] {i.Message}"));
        });
    }
}
