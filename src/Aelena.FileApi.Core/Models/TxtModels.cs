namespace Aelena.FileApi.Core.Models;

/// <summary>Metrics for a plain text file.</summary>
public sealed record TxtMetrics(
    string FileName,
    long FileSizeBytes,
    int WordCount,
    int CharCount,
    int TokenCount,
    string? Language,
    string? CreationDate,
    string? LastModifiedDate,
    int LineCount) : BaseMetrics(
        FileName, FileSizeBytes, WordCount, CharCount, TokenCount,
        Language, CreationDate, LastModifiedDate);
