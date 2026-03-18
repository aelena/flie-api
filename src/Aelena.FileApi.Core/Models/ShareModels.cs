namespace Aelena.FileApi.Core.Models;

/// <summary>Request to create a shareable link for a comparison report.</summary>
public sealed record CreateShareRequest(
    ComparisonReport Report,
    string AccessType = "anyone",
    IReadOnlyList<string>? AllowedEmails = null,
    string? Password = null,
    string? ExpiresAt = null);

/// <summary>Response after creating a share link.</summary>
public sealed record CreateShareResponse(
    string Token,
    string Url);

/// <summary>Metadata about an existing share link.</summary>
public sealed record ShareMetadata(
    string Token,
    string AccessType,
    string CreatedAt,
    string? ExpiresAt,
    int AccessCount,
    bool HasPassword,
    IReadOnlyList<string>? AllowedEmails = null);
