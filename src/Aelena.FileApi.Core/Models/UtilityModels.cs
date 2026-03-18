namespace Aelena.FileApi.Core.Models;

// ── Hash ──────────────────────────────────────────────────────────────────

/// <summary>
/// File fingerprint using multiple hash algorithms.
/// <c>CompositeSha256</c> folds in the filename and byte-length so two files
/// with identical content but different names produce different composites.
/// </summary>
public sealed record FileHashResponse(
    string FileName,
    long FileSizeBytes,
    string? ContentType,
    string Sha256,
    string Md5,
    string Sha1,
    string CompositeSha256);

// ── PII ───────────────────────────────────────────────────────────────────

/// <summary>A single PII match found in a document.</summary>
public sealed record PiiMatch(
    string PiiType,
    string Value,
    int Start,
    int End,
    string Context,
    string? Country = null);

/// <summary>PII detection scan result.</summary>
public sealed record PiiDetectionResponse(
    string FileName,
    int TotalMatches,
    IReadOnlyDictionary<string, int> ByType,
    IReadOnlyList<PiiMatch> Matches);

// ── Classification ────────────────────────────────────────────────────────

/// <summary>Structured metadata extracted during document classification.</summary>
public sealed record ClassificationMetadata(
    string? SubjectArea = null,
    string? Industry = null,
    string? FormalityLevel = null,
    string? TargetAudience = null,
    string? Language = null,
    IReadOnlyList<string>? KeyEntities = null,
    string? DocumentDate = null,
    bool? HasTables = null,
    bool? HasSignatures = null);

/// <summary>LLM-driven document type classification result.</summary>
public sealed record ClassifyResponse(
    string FileName,
    Enums.DocumentType DocumentType,
    double Confidence,
    ClassificationMetadata Metadata,
    string Rationale);

// ── Universal Search ──────────────────────────────────────────────────────

/// <summary>Cross-format search result.</summary>
public sealed record UniversalSearchResponse(
    string FileName,
    string FileType,
    string SectionType,
    int TotalMatches,
    IReadOnlyList<int> SectionsWithMatches,
    IReadOnlyList<SearchMatch> Matches);

// ── Q&A ───────────────────────────────────────────────────────────────────

/// <summary>Answer to a question grounded in document content (LLM-powered).</summary>
public sealed record QaResponse(
    string FileName,
    string Question,
    string Answer);

// ── Readability ───────────────────────────────────────────────────────────

/// <summary>Readability scores computed from document text.</summary>
public sealed record ReadabilityResponse(
    string FileName,
    int WordCount,
    int SentenceCount,
    int SyllableCount,
    int ComplexWordCount,
    double FleschReadingEase,
    double FleschKincaidGrade,
    double GunningFogIndex,
    double SmogIndex,
    string Interpretation);

// ── ZIP ───────────────────────────────────────────────────────────────────

/// <summary>A single entry inside a ZIP archive.</summary>
public sealed record ZipEntry(
    string Filename,
    bool IsDir,
    long FileSize,
    long CompressedSize,
    string CompressionMethod,
    string Crc32,
    string? LastModified = null);

/// <summary>Inspection result of a ZIP archive.</summary>
public sealed record ZipInspectResponse(
    string FileName,
    long FileSizeBytes,
    int TotalEntries,
    int TotalFiles,
    int TotalDirs,
    long TotalUncompressedSize,
    IReadOnlyList<ZipEntry> Entries);

// ── Video ─────────────────────────────────────────────────────────────────

/// <summary>Info about a single track (video, audio, subtitle).</summary>
public sealed record VideoTrack(
    string TrackType,
    string? Codec = null,
    double? DurationSeconds = null,
    int? Bitrate = null,
    int? Width = null,
    int? Height = null,
    double? FrameRate = null,
    string? AspectRatio = null,
    int? Channels = null,
    int? SampleRate = null,
    string? Language = null);

/// <summary>Container and track metadata from a video file.</summary>
public sealed record VideoMetadataResponse(
    string FileName,
    long FileSizeBytes,
    string? ContainerFormat = null,
    double? DurationSeconds = null,
    int? OverallBitrate = null,
    IReadOnlyList<VideoTrack> Tracks = default!,
    IReadOnlyDictionary<string, string>? Metadata = null);

// ── Email ─────────────────────────────────────────────────────────────────

/// <summary>An attachment found inside an email.</summary>
public sealed record EmailAttachment(
    string Filename,
    string ContentType,
    long SizeBytes);

/// <summary>Parsed email with headers, body, attachments, and optional LLM summary.</summary>
public sealed record EmailParseResponse(
    string FileName,
    string? Subject = null,
    string? FromAddress = null,
    IReadOnlyList<string>? To = null,
    IReadOnlyList<string>? Cc = null,
    IReadOnlyList<string>? Bcc = null,
    string? Date = null,
    string? MessageId = null,
    string? InReplyTo = null,
    string? BodyText = null,
    string? BodyHtml = null,
    IReadOnlyList<EmailAttachment>? Attachments = null,
    string? Summary = null,
    IReadOnlyList<string>? ActionItems = null);

// ── Batch ─────────────────────────────────────────────────────────────────

/// <summary>Result for a single file within a batch job.</summary>
public sealed record BatchFileResult(
    string JobId,
    string FileName,
    string Status,
    IReadOnlyDictionary<string, object>? Result = null,
    string? Error = null);

/// <summary>Status and results of a batch processing job.</summary>
public sealed record BatchJobResponse(
    string BatchId,
    string Status,
    string Operation,
    int TotalFiles,
    int Completed,
    int Failed,
    IReadOnlyList<BatchFileResult> Results);
