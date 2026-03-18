using System.CommandLine;
using Aelena.FileApi.Cli.Helpers;
using Aelena.FileApi.Core.Services.Pdf;

namespace Aelena.FileApi.Cli.Commands;

public static class PdfCommand
{
    public static Command Create()
    {
        var cmd = new Command("pdf", "PDF operations — metrics, extract text, merge, split, rotate, encrypt, etc.");
        cmd.AddCommand(Metrics());
        cmd.AddCommand(ExtractText());
        cmd.AddCommand(Metadata());
        cmd.AddCommand(Health());
        cmd.AddCommand(Merge());
        cmd.AddCommand(Rotate());
        cmd.AddCommand(Encrypt());
        cmd.AddCommand(Decrypt());
        cmd.AddCommand(Search());
        return cmd;
    }

    private static Command Metrics()
    {
        var fileArg = new Argument<FileInfo>("file", "PDF file");
        var cmd = new Command("metrics", "Get page count, word count, and analysis") { fileArg };
        cmd.SetHandler(file =>
        {
            var data = Output.ReadFileWithSpinner(file.FullName);
            var m = PdfService.GetMetrics(data, file.Name);
            Output.Properties($"PDF Metrics: {file.Name}",
                ("Pages", m.PageCount.ToString()),
                ("Words", m.WordCount.ToString("N0")),
                ("Characters", m.CharCount.ToString("N0")),
                ("Tokens", m.TokenCount.ToString("N0")),
                ("Images", m.ImageCount.ToString()),
                ("Language", m.Language),
                ("OCR needed", m.OcrNeeded ? $"Yes ({m.PagesNeedingOcrCount} pages)" : "No"),
                ("Signed", m.IsSigned.ToString()),
                ("Corrupt", m.IsCorrupt.ToString()),
                ("Size", $"{m.FileSizeBytes:N0} bytes"));
        }, fileArg);
        return cmd;
    }

    private static Command ExtractText()
    {
        var fileArg = new Argument<FileInfo>("file", "PDF file");
        var pagesOpt = new Option<string?>("--pages", "Page range (e.g. 1,3,5-8)");
        var cmd = new Command("extract-text", "Extract text from PDF") { fileArg, pagesOpt };
        cmd.SetHandler((file, pages) =>
        {
            var data = Output.ReadFileWithSpinner(file.FullName);
            var result = PdfService.ExtractText(data, file.Name, pages);
            foreach (var p in result.Pages)
            {
                Spectre.Console.AnsiConsole.MarkupLine($"[bold]--- Page {p.Page} ---[/]");
                Console.WriteLine(p.Text);
                Console.WriteLine();
            }
        }, fileArg, pagesOpt);
        return cmd;
    }

    private static Command Metadata()
    {
        var fileArg = new Argument<FileInfo>("file", "PDF file");
        var cmd = new Command("metadata", "Extract embedded metadata") { fileArg };
        cmd.SetHandler(file =>
        {
            var data = Output.ReadFileWithSpinner(file.FullName);
            var m = PdfService.GetMetadata(data, file.Name);
            Output.Properties($"PDF Metadata: {file.Name}",
                ("Title", m.Title), ("Author", m.Author), ("Subject", m.Subject),
                ("Keywords", m.Keywords), ("Creator", m.Creator), ("Producer", m.Producer),
                ("PDF Version", m.PdfVersion), ("Page Size", m.PageSize),
                ("Pages", m.PageCount.ToString()));
        }, fileArg);
        return cmd;
    }

    private static Command Health()
    {
        var fileArg = new Argument<FileInfo>("file", "PDF file");
        var cmd = new Command("health", "Run health check") { fileArg };
        cmd.SetHandler(file =>
        {
            var data = Output.ReadFileWithSpinner(file.FullName);
            var h = PdfService.HealthCheck(data, file.Name);
            var status = h.Healthy ? "[green]Healthy[/]" : "[red]Issues found[/]";
            Spectre.Console.AnsiConsole.MarkupLine($"Status: {status} ({h.ErrorCount} errors, {h.WarningCount} warnings, {h.InfoCount} info)");
            if (h.Issues.Count > 0)
                Output.List("Issues", h.Issues.Select(i => $"[{i.Severity}] {i.Check}: {i.Message}"));
        }, fileArg);
        return cmd;
    }

    private static Command Merge()
    {
        var filesArg = new Argument<FileInfo[]>("files", "PDF files to merge (2-10)");
        var outputOpt = new Option<string>("-o", () => "merged.pdf", "Output file");
        var cmd = new Command("merge", "Merge multiple PDFs") { filesArg, outputOpt };
        cmd.SetHandler((files, output) =>
        {
            var inputs = files.Select(f => (Output.ReadFileWithSpinner(f.FullName), f.Name)).ToList();
            var (_, bytes) = PdfService.MergePdfs(inputs);
            Output.WriteFile(output, bytes);
        }, filesArg, outputOpt);
        return cmd;
    }

    private static Command Rotate()
    {
        var fileArg = new Argument<FileInfo>("file", "PDF file");
        var angleOpt = new Option<int>("--angle", "Rotation angle: 90, 180, or 270") { IsRequired = true };
        var outputOpt = new Option<string?>("-o", "Output file (default: <name>_rotated.pdf)");
        var cmd = new Command("rotate", "Rotate pages") { fileArg, angleOpt, outputOpt };
        cmd.SetHandler((file, angle, output) =>
        {
            var data = Output.ReadFileWithSpinner(file.FullName);
            var (name, bytes) = PdfService.RotatePages(data, file.Name, angle);
            Output.WriteFile(output ?? name, bytes);
        }, fileArg, angleOpt, outputOpt);
        return cmd;
    }

    private static Command Encrypt()
    {
        var fileArg = new Argument<FileInfo>("file", "PDF file");
        var pwOpt = new Option<string>("--password", "User password") { IsRequired = true };
        var outputOpt = new Option<string?>("-o", "Output file");
        var cmd = new Command("encrypt", "Encrypt with password") { fileArg, pwOpt, outputOpt };
        cmd.SetHandler((file, pw, output) =>
        {
            var data = Output.ReadFileWithSpinner(file.FullName);
            var (name, bytes) = PdfService.EncryptPdf(data, file.Name, pw, null);
            Output.WriteFile(output ?? name, bytes);
        }, fileArg, pwOpt, outputOpt);
        return cmd;
    }

    private static Command Decrypt()
    {
        var fileArg = new Argument<FileInfo>("file", "PDF file");
        var pwOpt = new Option<string>("--password", "Password") { IsRequired = true };
        var outputOpt = new Option<string?>("-o", "Output file");
        var cmd = new Command("decrypt", "Decrypt a PDF") { fileArg, pwOpt, outputOpt };
        cmd.SetHandler((file, pw, output) =>
        {
            var data = Output.ReadFileWithSpinner(file.FullName);
            var (name, bytes) = PdfService.DecryptPdf(data, file.Name, pw);
            Output.WriteFile(output ?? name, bytes);
        }, fileArg, pwOpt, outputOpt);
        return cmd;
    }

    private static Command Search()
    {
        var fileArg = new Argument<FileInfo>("file", "PDF file");
        var queryOpt = new Option<string?>("--query", "Literal search text");
        var patternOpt = new Option<string?>("--pattern", "Regex pattern");
        var cmd = new Command("search", "Search text in PDF") { fileArg, queryOpt, patternOpt };
        cmd.SetHandler((file, query, pattern) =>
        {
            var data = Output.ReadFileWithSpinner(file.FullName);
            var (_, matches) = PdfService.Search(data, file.Name, query, pattern);
            Output.Success($"{matches.Count} match(es) found");
            foreach (var m in matches)
                Spectre.Console.AnsiConsole.MarkupLine($"  Page {m.Page}: [yellow]{Spectre.Console.Markup.Escape(m.Match)}[/] at {m.Start}");
        }, fileArg, queryOpt, patternOpt);
        return cmd;
    }
}
