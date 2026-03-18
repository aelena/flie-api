using System.CommandLine;
using Aelena.FileApi.Cli.Helpers;
using Aelena.FileApi.Core.Services.Docx;

namespace Aelena.FileApi.Cli.Commands;

public static class DocxCommand
{
    public static Command Create()
    {
        var cmd = new Command("docx", "DOCX operations — metrics, metadata, markdown, health");
        cmd.AddCommand(Metrics());
        cmd.AddCommand(Metadata());
        cmd.AddCommand(Markdown());
        cmd.AddCommand(Health());
        return cmd;
    }

    private static Command Metrics()
    {
        var fileArg = new Argument<FileInfo>("file", "DOCX file");
        var cmd = new Command("metrics", "Get document metrics") { fileArg };
        cmd.SetHandler(file =>
        {
            var data = Output.ReadFileWithSpinner(file.FullName);
            var m = DocxService.GetMetrics(data, file.Name);
            Output.Properties($"DOCX Metrics: {file.Name}",
                ("Paragraphs", m.ParagraphCount.ToString()),
                ("Words", m.WordCount.ToString("N0")),
                ("Tables", m.TableCount.ToString()),
                ("Images", m.ImageCount.ToString()),
                ("Pages", m.PageCount?.ToString() ?? "unknown"),
                ("Language", m.Language),
                ("Size", $"{m.FileSizeBytes:N0} bytes"));
        }, fileArg);
        return cmd;
    }

    private static Command Metadata()
    {
        var fileArg = new Argument<FileInfo>("file", "DOCX file");
        var cmd = new Command("metadata", "Extract metadata") { fileArg };
        cmd.SetHandler(file =>
        {
            var data = Output.ReadFileWithSpinner(file.FullName);
            var m = DocxService.GetMetadata(data, file.Name);
            Output.Properties($"DOCX Metadata: {file.Name}",
                ("Title", m.Title), ("Author", m.Author), ("Subject", m.Subject),
                ("Keywords", m.Keywords), ("Category", m.Category),
                ("Created", m.Created), ("Modified", m.Modified),
                ("Revision", m.Revision?.ToString()));
        }, fileArg);
        return cmd;
    }

    private static Command Markdown()
    {
        var fileArg = new Argument<FileInfo>("file", "DOCX file");
        var cmd = new Command("markdown", "Convert to Markdown") { fileArg };
        cmd.SetHandler(file =>
        {
            var data = Output.ReadFileWithSpinner(file.FullName);
            var m = DocxService.ExtractToMarkdown(data, file.Name);
            Console.WriteLine(m.Markdown);
        }, fileArg);
        return cmd;
    }

    private static Command Health()
    {
        var fileArg = new Argument<FileInfo>("file", "DOCX file");
        var cmd = new Command("health", "Run health check") { fileArg };
        cmd.SetHandler(file =>
        {
            var data = Output.ReadFileWithSpinner(file.FullName);
            var h = DocxService.HealthCheck(data, file.Name);
            var status = h.Healthy ? "[green]Healthy[/]" : "[red]Issues found[/]";
            Spectre.Console.AnsiConsole.MarkupLine($"Status: {status}");
            if (h.Issues.Count > 0)
                Output.List("Issues", h.Issues.Select(i => $"[{i.Severity}] {i.Message}"));
        }, fileArg);
        return cmd;
    }
}
