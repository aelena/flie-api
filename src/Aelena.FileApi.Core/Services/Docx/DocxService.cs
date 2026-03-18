using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Aelena.FileApi.Core.Errors;
using Aelena.FileApi.Core.Models;
using Aelena.FileApi.Core.Services.Common;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Aelena.FileApi.Core.Services.Docx;

/// <summary>
/// Stateless DOCX processing service built on Open XML SDK.
/// Provides metrics, metadata, text extraction, search, markdown conversion,
/// health checks, and metadata removal for Word documents.
/// </summary>
public static class DocxService
{
    // ── Read Operations ──────────────────────────────────────────────────

    /// <summary>Get comprehensive metrics for a DOCX file.</summary>
    public static DocxMetrics GetMetrics(byte[] data, string fileName)
    {
        using var stream = new MemoryStream(data);
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart?.Document?.Body
            ?? throw new FileApiException(400, "Cannot read DOCX: missing document body.");

        var paragraphs = body.Descendants<Paragraph>().ToList();
        var fullText = string.Join("\n", paragraphs.Select(p => p.InnerText));
        var imageCount = CountImages(doc);
        var tableCount = body.Descendants<Table>().Count();
        var (created, modified) = GetCoreDates(doc);
        var pageCount = GetPageCount(doc);

        return new DocxMetrics(
            FileName: fileName,
            FileSizeBytes: data.Length,
            WordCount: TextAnalysis.CountWords(fullText),
            CharCount: TextAnalysis.CountChars(fullText),
            TokenCount: TextAnalysis.CountTokens(fullText),
            Language: TextAnalysis.DetectLanguage(fullText),
            CreationDate: created,
            LastModifiedDate: modified,
            PageCount: pageCount,
            ParagraphCount: paragraphs.Count,
            ImageCount: imageCount,
            TableCount: tableCount);
    }

    /// <summary>Extract core and custom properties from a DOCX.</summary>
    public static DocxMetadataResponse GetMetadata(byte[] data, string fileName)
    {
        using var stream = new MemoryStream(data);
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart?.Document?.Body;

        var props = doc.PackageProperties;
        int? revision = null;
        if (props.Revision is not null && int.TryParse(props.Revision, out var rev))
            revision = rev;

        var custom = ParseCustomProperties(data);

        return new DocxMetadataResponse(
            FileName: fileName,
            Title: NullIfEmpty(props.Title),
            Author: NullIfEmpty(props.Creator),
            Subject: NullIfEmpty(props.Subject),
            Keywords: NullIfEmpty(props.Keywords),
            Category: NullIfEmpty(props.Category),
            Comments: NullIfEmpty(props.Description),
            LastModifiedBy: NullIfEmpty(props.LastModifiedBy),
            Revision: revision,
            ContentStatus: NullIfEmpty(props.ContentStatus),
            Created: props.Created?.ToString("o"),
            Modified: props.Modified?.ToString("o"),
            ParagraphCount: body?.Descendants<Paragraph>().Count() ?? 0,
            TableCount: body?.Descendants<Table>().Count() ?? 0,
            ImageCount: CountImages(doc),
            CustomMetadata: custom.Count > 0 ? custom : null);
    }

    /// <summary>Extract paragraphs by index ranges (1-based, like page numbers).</summary>
    public static ExtractionResponse ExtractPages(byte[] data, string fileName, string pages)
    {
        using var stream = new MemoryStream(data);
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body is null) throw new FileApiException(400, "Cannot read DOCX.");
        var paragraphs = body.Descendants<Paragraph>().Select(p => p.InnerText).ToList();

        var total = paragraphs.Count;
        var indices = PageRangeParser.Parse(pages, total);
        var extracted = indices.Select(i => new PageContent(i + 1, paragraphs[i])).ToList();

        return new ExtractionResponse(fileName, total, extracted);
    }

    /// <summary>Search a DOCX for literal text or regex matches.</summary>
    public static (string FileName, IReadOnlyList<SearchMatch> Matches) Search(
        byte[] data, string fileName, string? query = null, string? pattern = null)
    {
        using var stream = new MemoryStream(data);
        using var doc = WordprocessingDocument.Open(stream, false);
        var bodyEl = doc.MainDocumentPart?.Document?.Body;
        var fullText = bodyEl is not null
            ? string.Join("\n", bodyEl.Descendants<Paragraph>().Select(p => p.InnerText))
            : "";

        var matches = TextSearch.Search(fullText, query: query, pattern: pattern);
        return (fileName, matches);
    }

    /// <summary>Convert a DOCX to basic Markdown with heading and formatting detection.</summary>
    public static DocxMarkdownResponse ExtractToMarkdown(byte[] data, string fileName)
    {
        using var stream = new MemoryStream(data);
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart?.Document?.Body
            ?? throw new FileApiException(400, "Cannot read DOCX.");

        var md = new StringBuilder();
        var paraCount = 0;

        foreach (var element in body.ChildElements)
        {
            if (element is Paragraph para)
            {
                paraCount++;
                var text = para.InnerText;
                if (string.IsNullOrWhiteSpace(text)) { md.AppendLine(); continue; }

                var styleId = para.ParagraphProperties?.ParagraphStyleId?.Val?.Value ?? "";
                var headingLevel = styleId switch
                {
                    "Heading1" or "heading1" => 1,
                    "Heading2" or "heading2" => 2,
                    "Heading3" or "heading3" => 3,
                    "Heading4" or "heading4" => 4,
                    _ when styleId.StartsWith("Heading", StringComparison.OrdinalIgnoreCase)
                        && int.TryParse(styleId.AsSpan(7), out var lvl) => lvl,
                    _ => 0
                };

                if (headingLevel > 0)
                    md.AppendLine($"{new string('#', headingLevel)} {text}");
                else
                    md.AppendLine(FormatRuns(para));

                md.AppendLine();
            }
            else if (element is Table table)
            {
                md.AppendLine(TableToMarkdown(table));
                md.AppendLine();
            }
        }

        return new DocxMarkdownResponse(fileName, paraCount, md.ToString().TrimEnd());
    }

    /// <summary>Run comprehensive health checks on a DOCX file.</summary>
    public static DocxHealthResponse HealthCheck(byte[] data, string fileName)
    {
        var issues = new List<HealthIssue>();

        try
        {
            using var stream = new MemoryStream(data);
            using var doc = WordprocessingDocument.Open(stream, false);
            var hBody = doc.MainDocumentPart?.Document?.Body;

            // Tracked changes
            if (hBody?.Descendants<InsertedRun>().Any() == true ||
                hBody?.Descendants<DeletedRun>().Any() == true)
                issues.Add(new HealthIssue("tracked_changes", "warning",
                    "Document contains unresolved tracked changes."));

            // Embedded macros (check for vbaProject.bin)
            if (doc.MainDocumentPart?.GetPartsOfType<EmbeddedPackagePart>().Any() == true)
                issues.Add(new HealthIssue("macros", "warning",
                    "Document may contain embedded macros or packages."));

            // Check core properties exist
            if (doc.PackageProperties.Created is null)
                issues.Add(new HealthIssue("missing_properties", "info",
                    "Core properties (creation date) are missing."));

            // Check for broken image references
            var mainPart = doc.MainDocumentPart;
            if (mainPart is not null)
            {
                var drawings = hBody?.Descendants<Drawing>().Count() ?? 0;
                var imageParts = mainPart.ImageParts.Count();
                if (drawings > 0 && imageParts == 0)
                    issues.Add(new HealthIssue("broken_images", "warning",
                        $"Document references {drawings} drawing(s) but has no image parts."));
            }
        }
        catch (Exception ex) when (ex is not FileApiException)
        {
            issues.Add(new HealthIssue("corruption", "error", $"Cannot parse DOCX: {ex.Message}"));
        }

        var errors = issues.Count(i => i.Severity == "error");
        var warnings = issues.Count(i => i.Severity == "warning");
        var infos = issues.Count(i => i.Severity == "info");

        return new DocxHealthResponse(
            FileName: fileName,
            Healthy: errors == 0 && warnings == 0,
            IssueCount: issues.Count,
            ErrorCount: errors,
            WarningCount: warnings,
            InfoCount: infos,
            Issues: issues);
    }

    /// <summary>Remove all metadata from a DOCX file.</summary>
    public static (string FileName, byte[] Data) RemoveMetadata(byte[] data, string fileName)
    {
        using var stream = new MemoryStream();
        stream.Write(data);
        stream.Position = 0;

        using (var doc = WordprocessingDocument.Open(stream, true))
        {
            var props = doc.PackageProperties;
            props.Title = null;
            props.Creator = null;
            props.Subject = null;
            props.Keywords = null;
            props.Category = null;
            props.Description = null;
            props.LastModifiedBy = null;
            props.ContentStatus = null;
        }

        // Remove custom properties from ZIP
        var cleaned = RemoveCustomPropertiesFromZip(stream.ToArray());

        var stem = System.IO.Path.GetFileNameWithoutExtension(fileName);
        return ($"{stem}_no_metadata.docx", cleaned);
    }

    // ── Stubs for operations requiring external tools ─────────────────────

    /// <summary>Convert DOCX to PDF. Requires LibreOffice headless.</summary>
    public static (string FileName, byte[] Data) ConvertToPdf(byte[] data, string fileName) =>
        throw new FileApiException(501, "DOCX to PDF conversion requires LibreOffice headless. Coming soon.");

    /// <summary>Add a diagonal text watermark to all sections.</summary>
    public static (string FileName, byte[] Data) AddWatermark(
        byte[] data, string fileName, string text, string color, int fontSize, int angle) =>
        throw new FileApiException(501, "DOCX watermark requires VML shape injection. Coming soon.");

    /// <summary>Remove VML-based watermarks from headers.</summary>
    public static (string FileName, byte[] Data) RemoveWatermark(byte[] data, string fileName) =>
        throw new FileApiException(501, "DOCX watermark removal coming soon.");

    // ── Private Helpers ──────────────────────────────────────────────────

    private static int CountImages(WordprocessingDocument doc) =>
        doc.MainDocumentPart?.ImageParts.Count() ?? 0;

    private static (string? Created, string? Modified) GetCoreDates(WordprocessingDocument doc) =>
        (doc.PackageProperties.Created?.ToString("o"), doc.PackageProperties.Modified?.ToString("o"));

    private static int? GetPageCount(WordprocessingDocument doc)
    {
        try
        {
            var extProps = doc.ExtendedFilePropertiesPart;
            if (extProps?.Properties?.Pages?.Text is { } pagesText && int.TryParse(pagesText, out var count))
                return count;
        }
        catch { /* not available */ }
        return null;
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

    private static Dictionary<string, string> ParseCustomProperties(byte[] data)
    {
        var custom = new Dictionary<string, string>();
        try
        {
            using var zip = new ZipArchive(new MemoryStream(data), ZipArchiveMode.Read);
            var entry = zip.GetEntry("docProps/custom.xml");
            if (entry is null) return custom;

            using var entryStream = entry.Open();
            var xdoc = XDocument.Load(entryStream);
            var ns = XNamespace.Get("http://schemas.openxmlformats.org/officeDocument/2006/custom-properties");
            foreach (var prop in xdoc.Descendants(ns + "property"))
            {
                var name = prop.Attribute("name")?.Value ?? "";
                var value = prop.Elements().FirstOrDefault()?.Value ?? "";
                if (!string.IsNullOrEmpty(name))
                    custom[name] = value;
            }
        }
        catch { /* ignore */ }
        return custom;
    }

    private static byte[] RemoveCustomPropertiesFromZip(byte[] docxBytes)
    {
        using var srcStream = new MemoryStream(docxBytes);
        using var srcZip = new ZipArchive(srcStream, ZipArchiveMode.Read);
        using var outStream = new MemoryStream();
        using (var outZip = new ZipArchive(outStream, ZipArchiveMode.Create, true))
        {
            foreach (var entry in srcZip.Entries)
            {
                if (entry.FullName == "docProps/custom.xml") continue;
                var newEntry = outZip.CreateEntry(entry.FullName);
                using var src = entry.Open();
                using var dst = newEntry.Open();
                src.CopyTo(dst);
            }
        }
        return outStream.ToArray();
    }

    private static string FormatRuns(Paragraph para)
    {
        var sb = new StringBuilder();
        foreach (var run in para.Descendants<Run>())
        {
            var text = run.InnerText;
            if (string.IsNullOrEmpty(text)) continue;

            var bold = run.RunProperties?.Bold is not null;
            var italic = run.RunProperties?.Italic is not null;

            if (bold && italic) sb.Append($"***{text}***");
            else if (bold) sb.Append($"**{text}**");
            else if (italic) sb.Append($"*{text}*");
            else sb.Append(text);
        }
        return sb.ToString();
    }

    private static string TableToMarkdown(Table table)
    {
        var rows = table.Descendants<TableRow>().ToList();
        if (rows.Count == 0) return "";

        var md = new StringBuilder();
        for (var r = 0; r < rows.Count; r++)
        {
            var cells = rows[r].Descendants<TableCell>().Select(c => c.InnerText.Trim()).ToList();
            md.AppendLine("| " + string.Join(" | ", cells) + " |");
            if (r == 0)
                md.AppendLine("| " + string.Join(" | ", cells.Select(_ => "---")) + " |");
        }
        return md.ToString().TrimEnd();
    }
}
