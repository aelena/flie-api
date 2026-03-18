namespace Aelena.FileApi.Core.Models;

/// <summary>Comprehensive metrics for a PDF file.</summary>
public sealed record PdfMetrics(
    string FileName,
    long FileSizeBytes,
    int WordCount,
    int CharCount,
    int TokenCount,
    string? Language,
    string? CreationDate,
    string? LastModifiedDate,
    int PageCount,
    double AvgCharsPerPage,
    int ImageCount,
    int TableCount,
    bool IsCorrupt,
    bool IsSigned,
    bool OcrNeeded,
    IReadOnlyList<int> OcrPages,
    int PagesNeedingOcrCount) : BaseMetrics(
        FileName, FileSizeBytes, WordCount, CharCount, TokenCount,
        Language, CreationDate, LastModifiedDate);

/// <summary>Full text extraction from a PDF, organised by page.</summary>
public sealed record PdfTextResponse(
    string FileName,
    int TotalPages,
    IReadOnlyList<PageContent> Pages);

/// <summary>Embedded metadata from a PDF document.</summary>
public sealed record PdfMetadataResponse(
    string FileName,
    string? Title = null,
    string? Author = null,
    string? Subject = null,
    string? Keywords = null,
    string? Creator = null,
    string? Producer = null,
    string? CreationDate = null,
    string? ModificationDate = null,
    string? Trapped = null,
    int PageCount = 0,
    string? PdfVersion = null,
    string? PageSize = null,
    IReadOnlyDictionary<string, string>? CustomMetadata = null);

/// <summary>Markdown representation of a PDF.</summary>
public sealed record MarkdownResponse(
    string FileName,
    int TotalPages,
    string Markdown);

/// <summary>OCR result for a single page.</summary>
public sealed record OcrPageContent(
    int Page,
    string Text,
    bool OcrApplied);

/// <summary>OCR result for an entire PDF.</summary>
public sealed record PdfOcrResponse(
    string FileName,
    int TotalPages,
    string Engine,
    string Language,
    IReadOnlyList<OcrPageContent> Pages);

/// <summary>A table extracted from a PDF page.</summary>
public sealed record ExtractedTable(
    int Page,
    int TableIndex,
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<string>> Rows);

/// <summary>All tables found in a PDF.</summary>
public sealed record PdfTableResponse(
    string FileName,
    int TotalPages,
    int TotalTables,
    IReadOnlyList<ExtractedTable> Tables);

/// <summary>Metadata about a single image embedded in a PDF.</summary>
public sealed record ImageInfo(
    int Page,
    int Index,
    string Format,
    int Width,
    int Height,
    long SizeBytes);

/// <summary>All images found in a PDF.</summary>
public sealed record PdfImageListResponse(
    string FileName,
    int TotalPages,
    int TotalImages,
    IReadOnlyList<ImageInfo> Images);

/// <summary>A single fillable field inside a PDF form.</summary>
public sealed record FormField(
    string Name,
    string FieldType,
    string? Value = null,
    string? DefaultValue = null,
    IReadOnlyList<string>? Options = null,
    bool IsReadOnly = false,
    int? Page = null);

/// <summary>All form fields in a PDF.</summary>
public sealed record FormFieldsResponse(
    string FileName,
    int TotalFields,
    IReadOnlyList<FormField> Fields);

/// <summary>A single issue found during PDF health check.</summary>
public sealed record HealthIssue(
    string Check,
    string Severity,
    string Message,
    IReadOnlyDictionary<string, object>? Details = null);

/// <summary>Comprehensive health report for a PDF.</summary>
public sealed record PdfHealthResponse(
    string FileName,
    bool Healthy,
    int IssueCount,
    int ErrorCount,
    int WarningCount,
    int InfoCount,
    IReadOnlyList<HealthIssue> Issues);

/// <summary>A single redaction applied to a PDF.</summary>
public sealed record RedactionMatch(
    int Page,
    string Text,
    int Count);

/// <summary>Result of PDF redaction operation.</summary>
public sealed record PdfRedactResponse(
    string FileName,
    int TotalRedactions,
    int PagesAffected,
    IReadOnlyList<RedactionMatch> Redactions);
