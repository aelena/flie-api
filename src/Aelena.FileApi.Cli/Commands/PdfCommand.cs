using System.CommandLine;
using Aelena.FileApi.Cli.Helpers;
using Aelena.FileApi.Core.Services.Pdf;
using Spectre.Console;

namespace Aelena.FileApi.Cli.Commands;

/// <summary><c>fileapi pdf</c> — inspect, extract from, and transform PDF documents.</summary>
public static class PdfCommand
{
    public static Command Create()
    {
        var cmd = new Command("pdf", "PDF operations — metrics, extract text, merge, split, rotate, encrypt, etc.");
        cmd.Add(Metrics());
        cmd.Add(ExtractText());
        cmd.Add(Metadata());
        cmd.Add(Health());
        cmd.Add(Merge());
        cmd.Add(Rotate());
        cmd.Add(Encrypt());
        cmd.Add(Decrypt());
        cmd.Add(Search());
        return cmd;
    }

    private static Command Metrics()
    {
        var fileArg = CommandExtensions.FileArgument("PDF file");
        var cmd = new Command("metrics", "Get page count, word count, and analysis") { fileArg };

        return cmd.WithAction(parse =>
        {
            var file = parse.GetRequiredValue(fileArg);
            var metrics = PdfService.GetMetrics(Output.ReadFile(file), file.Name);

            Output.Properties($"PDF Metrics: {file.Name}",
                ("Pages", metrics.PageCount.Display()),
                ("Words", metrics.WordCount.Display("N0")),
                ("Characters", metrics.CharCount.Display("N0")),
                ("Tokens", metrics.TokenCount.Display("N0")),
                ("Images", metrics.ImageCount.Display()),
                ("Language", metrics.Language),
                ("OCR needed", metrics.OcrNeeded ? $"Yes ({metrics.PagesNeedingOcrCount} pages)" : "No"),
                ("Signed", YesNo(metrics.IsSigned)),
                ("Corrupt", YesNo(metrics.IsCorrupt)),
                ("Size", $"{metrics.FileSizeBytes:N0} bytes"));
        });
    }

    /// <summary>Booleans read better as Yes/No than as True/False in a report table.</summary>
    private static string YesNo(bool value) => value ? "Yes" : "No";

    private static Command ExtractText()
    {
        var fileArg = CommandExtensions.FileArgument("PDF file");
        var pagesOpt = new Option<string?>("--pages")
        {
            Description = "Pages to extract, 1-based, e.g. 1,3,5-8. Defaults to every page."
        };
        var cmd = new Command("extract-text", "Extract text from PDF") { fileArg, pagesOpt };

        return cmd.WithAction(parse =>
        {
            var file = parse.GetRequiredValue(fileArg);
            var result = PdfService.ExtractText(
                Output.ReadFile(file), file.Name, parse.GetValue(pagesOpt));

            foreach (var page in result.Pages)
            {
                AnsiConsole.MarkupLine($"[bold]--- Page {page.Page} ---[/]");
                Console.WriteLine(page.Text);
                Console.WriteLine();
            }
        });
    }

    private static Command Metadata()
    {
        var fileArg = CommandExtensions.FileArgument("PDF file");
        var cmd = new Command("metadata", "Extract embedded metadata") { fileArg };

        return cmd.WithAction(parse =>
        {
            var file = parse.GetRequiredValue(fileArg);
            var meta = PdfService.GetMetadata(Output.ReadFile(file), file.Name);

            Output.Properties($"PDF Metadata: {file.Name}",
                ("Title", meta.Title), ("Author", meta.Author), ("Subject", meta.Subject),
                ("Keywords", meta.Keywords), ("Creator", meta.Creator), ("Producer", meta.Producer),
                ("PDF Version", meta.PdfVersion), ("Page Size", meta.PageSize),
                ("Pages", meta.PageCount.Display()));
        });
    }

    private static Command Health()
    {
        var fileArg = CommandExtensions.FileArgument("PDF file");
        var cmd = new Command("health", "Run health check") { fileArg };

        return cmd.WithAction(parse =>
        {
            var file = parse.GetRequiredValue(fileArg);
            var health = PdfService.HealthCheck(Output.ReadFile(file), file.Name);

            var status = health.Healthy ? "[green]Healthy[/]" : "[red]Issues found[/]";
            AnsiConsole.MarkupLine(
                $"Status: {status} ({health.ErrorCount} errors, {health.WarningCount} warnings, {health.InfoCount} info)");

            if (health.Issues.Count > 0)
                Output.List("Issues", health.Issues.Select(i => $"[{i.Severity}] {i.Check}: {i.Message}"));
        });
    }

    private static Command Merge()
    {
        var filesArg = new Argument<FileInfo[]>("files")
        {
            Description = "PDF files to merge, in order (2 to 10 files)"
        }.AcceptExistingOnly();
        var outOpt = new Option<string>("--output", "-o")
        {
            Description = "Output file path",
            DefaultValueFactory = _ => "merged.pdf"
        };
        var cmd = new Command("merge", "Merge multiple PDFs") { filesArg, outOpt };

        return cmd.WithAction(parse =>
        {
            var inputs = parse.GetRequiredValue(filesArg)
                .Select(f => (Output.ReadFile(f), f.Name))
                .ToList();

            var (_, bytes) = PdfService.MergePdfs(inputs);
            Output.WriteFile(parse.GetRequiredValue(outOpt), bytes);
        });
    }

    private static Command Rotate()
    {
        var fileArg = CommandExtensions.FileArgument("PDF file");
        var angleOpt = new Option<int>("--angle")
        {
            Description = "Rotation angle, clockwise",
            Required = true
        }.AcceptOnlyFromAmong("90", "180", "270");
        var outOpt = CommandExtensions.OutputOption("Output file (default: <name>_rotated.pdf)");
        var cmd = new Command("rotate", "Rotate pages") { fileArg, angleOpt, outOpt };

        return cmd.WithAction(parse =>
        {
            var file = parse.GetRequiredValue(fileArg);
            var (name, bytes) = PdfService.RotatePages(
                Output.ReadFile(file), file.Name, parse.GetRequiredValue(angleOpt));

            Output.WriteFile(parse.GetValue(outOpt) ?? name, bytes);
        });
    }

    private static Command Encrypt()
    {
        var fileArg = CommandExtensions.FileArgument("PDF file");
        var passwordOpt = new Option<string>("--password")
        {
            Description = "User password required to open the encrypted file",
            Required = true
        };
        var outOpt = CommandExtensions.OutputOption("Output file (default: <name>_encrypted.pdf)");
        var cmd = new Command("encrypt", "Encrypt with password") { fileArg, passwordOpt, outOpt };

        return cmd.WithAction(parse =>
        {
            var file = parse.GetRequiredValue(fileArg);
            var (name, bytes) = PdfService.EncryptPdf(
                Output.ReadFile(file), file.Name, parse.GetRequiredValue(passwordOpt), null);

            Output.WriteFile(parse.GetValue(outOpt) ?? name, bytes);
        });
    }

    private static Command Decrypt()
    {
        var fileArg = CommandExtensions.FileArgument("PDF file");
        var passwordOpt = new Option<string>("--password")
        {
            Description = "Password the file was encrypted with",
            Required = true
        };
        var outOpt = CommandExtensions.OutputOption("Output file (default: <name>_decrypted.pdf)");
        var cmd = new Command("decrypt", "Decrypt a PDF") { fileArg, passwordOpt, outOpt };

        return cmd.WithAction(parse =>
        {
            var file = parse.GetRequiredValue(fileArg);
            var (name, bytes) = PdfService.DecryptPdf(
                Output.ReadFile(file), file.Name, parse.GetRequiredValue(passwordOpt));

            Output.WriteFile(parse.GetValue(outOpt) ?? name, bytes);
        });
    }

    private static Command Search()
    {
        var fileArg = CommandExtensions.FileArgument("PDF file");
        var queryOpt = new Option<string?>("--query") { Description = "Literal search text" };
        var patternOpt = new Option<string?>("--pattern") { Description = "Regular expression to match" };
        var cmd = new Command("search", "Search text in PDF") { fileArg, queryOpt, patternOpt };

        return cmd.WithAction(parse =>
        {
            var file = parse.GetRequiredValue(fileArg);
            var (_, matches) = PdfService.Search(
                Output.ReadFile(file), file.Name,
                parse.GetValue(queryOpt), parse.GetValue(patternOpt));

            Output.Success($"{matches.Count} match(es) found");
            foreach (var match in matches)
                AnsiConsole.MarkupLine(
                    $"  Page {match.Page}: [yellow]{Markup.Escape(match.Match)}[/] at {match.Start}");
        });
    }
}
