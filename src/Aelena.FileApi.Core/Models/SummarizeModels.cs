namespace Aelena.FileApi.Core.Models;

/// <summary>Synchronous summarization result.</summary>
public sealed record SummarizeResponse(
    string FileName,
    string Summary);

/// <summary>Status and result of an async summarization job.</summary>
public sealed record SummarizeJobReport(
    string JobId,
    string Status,
    string FileName,
    string? Summary = null,
    string? Error = null);
