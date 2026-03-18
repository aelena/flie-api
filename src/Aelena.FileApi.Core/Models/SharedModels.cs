namespace Aelena.FileApi.Core.Models;

/// <summary>RFC 9457 Problem Details for HTTP APIs.</summary>
public sealed record ProblemDetail(
    [property: JsonPropertyName("type")] string Type = "about:blank",
    [property: JsonPropertyName("title")] string Title = "",
    [property: JsonPropertyName("status")] int Status = 500,
    [property: JsonPropertyName("detail")] string Detail = "",
    [property: JsonPropertyName("instance")] string? Instance = null);

/// <summary>Common metrics shared across all document types.</summary>
public record BaseMetrics(
    string FileName,
    long FileSizeBytes,
    int WordCount,
    int CharCount,
    int TokenCount,
    string? Language = null,
    string? CreationDate = null,
    string? LastModifiedDate = null);

/// <summary>A single text match found during a search operation.</summary>
public sealed record SearchMatch(
    string Match,
    int Start,
    int End,
    string Context,
    int? Page = null);

/// <summary>Result of a text/regex search across a document.</summary>
public sealed record SearchResponse(
    string FileName,
    int TotalMatches,
    IReadOnlyList<SearchMatch> Matches);

/// <summary>Text content of a single page or section.</summary>
public sealed record PageContent(
    int Page,
    string Text);

/// <summary>Result of extracting pages/sections from a document.</summary>
public sealed record ExtractionResponse(
    string FileName,
    int TotalPages,
    IReadOnlyList<PageContent> Extracted);

/// <summary>Async job creation acknowledgement.</summary>
public sealed record JobCreatedResponse(
    string JobId,
    string Status = "processing");
