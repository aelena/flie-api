using Aelena.FileApi.Core.Enums;

namespace Aelena.FileApi.Core.Models;

/// <summary>A single change detected between two documents.</summary>
public sealed record DiffChange(
    string Type,
    int? SectionA = null,
    int? SectionB = null,
    string TextA = "",
    string TextB = "",
    string? Classification = null,
    string? Severity = null,
    string? Summary = null,
    bool? SemanticallyEquivalent = null,
    double? Confidence = null);

/// <summary>Aggregate statistics for a comparison.</summary>
public sealed record ComparisonSummary(
    int TotalChanges,
    int Additions,
    int Removals,
    int Modifications,
    IReadOnlyDictionary<string, int>? ByClassification = null,
    IReadOnlyDictionary<string, int>? BySeverity = null);

/// <summary>Full comparison report, returned from async job polling.</summary>
public sealed record ComparisonReport(
    string JobId,
    string Status,
    string FileA,
    string FileB,
    string Mode,
    string Alignment,
    string Confidentiality,
    string Output = "structured",
    string? Tone = null,
    ComparisonSummary? Summary = null,
    IReadOnlyList<DiffChange>? Changes = null,
    string? SummaryA = null,
    string? SummaryB = null,
    string? Narrative = null,
    string? Error = null);
