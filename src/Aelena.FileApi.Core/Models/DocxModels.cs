namespace Aelena.FileApi.Core.Models;

/// <summary>Metrics for a DOCX file.</summary>
public sealed record DocxMetrics(
    string FileName,
    long FileSizeBytes,
    int WordCount,
    int CharCount,
    int TokenCount,
    string? Language,
    string? CreationDate,
    string? LastModifiedDate,
    int? PageCount,
    int ParagraphCount,
    int ImageCount,
    int TableCount) : BaseMetrics(
        FileName, FileSizeBytes, WordCount, CharCount, TokenCount,
        Language, CreationDate, LastModifiedDate);

/// <summary>Core and custom properties from a DOCX document.</summary>
public sealed record DocxMetadataResponse(
    string FileName,
    string? Title = null,
    string? Author = null,
    string? Subject = null,
    string? Keywords = null,
    string? Category = null,
    string? Comments = null,
    string? LastModifiedBy = null,
    int? Revision = null,
    string? ContentStatus = null,
    string? Created = null,
    string? Modified = null,
    int ParagraphCount = 0,
    int TableCount = 0,
    int ImageCount = 0,
    IReadOnlyDictionary<string, string>? CustomMetadata = null);

/// <summary>Markdown representation of a DOCX.</summary>
public sealed record DocxMarkdownResponse(
    string FileName,
    int ParagraphCount,
    string Markdown);

/// <summary>Health report for a DOCX file.</summary>
public sealed record DocxHealthResponse(
    string FileName,
    bool Healthy,
    int IssueCount,
    int ErrorCount,
    int WarningCount,
    int InfoCount,
    IReadOnlyList<HealthIssue> Issues);
