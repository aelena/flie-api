using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Aelena.FileApi.Core.Services.Persistence;

namespace Aelena.FileApi.Api.Endpoints;

/// <summary>Share link CRUD endpoints — create, access, list, and revoke shareable report links.</summary>
public static class ShareEndpoints
{
    public static RouteGroupBuilder MapShareEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("", (CreateShareRequest request, ShareRepository repo) =>
        {
            var token = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(12));
            var reportJson = JsonSerializer.Serialize(request.Report);
            var passwordHash = request.Password is not null ? HashPassword(token, request.Password) : null;
            var emailsJson = request.AllowedEmails is { Count: > 0 }
                ? JsonSerializer.Serialize(request.AllowedEmails)
                : null;

            repo.Create(token, request.Report.JobId, reportJson,
                request.AccessType, emailsJson, passwordHash, request.ExpiresAt);

            return Results.Created($"/share/{token}", new CreateShareResponse(token, $"/share/{token}"));
        }).WithName("CreateShare").DisableAntiforgery();

        group.MapGet("/job/{jobId}", (string jobId, ShareRepository repo) =>
        {
            var shares = repo.ListForJob(jobId)
                .Select(share => new ShareMetadata(
                    share.Token,
                    share.AccessType,
                    share.CreatedAt,
                    share.ExpiresAt,
                    (int)share.AccessCount,
                    share.PasswordHash is not null,
                    DeserializeEmails(share.AllowedEmails)))
                .ToList();

            return Results.Ok(shares);
        }).WithName("ListSharesForJob");

        group.MapGet("/{token}", (string token, string? password, ShareRepository repo, HttpContext ctx) =>
        {
            var share = repo.GetByToken(token)
                ?? throw new FileApiException(404, "Share link not found.");

            // Expiry, password, and the recipient list were all recorded at creation
            // and then never consulted: an expired link kept working indefinitely, and
            // a password-protected link opened for anyone holding the URL. Each of
            // these is checked before the report is handed over.
            if (share.IsExpired)
                throw new FileApiException(410,
                    "This share link expired on " + share.ExpiresAt + " and is no longer available.");

            RequirePassword(share, password);
            RequireRecipient(share, ctx);

            repo.IncrementAccessCount(token);

            return Results.Ok(JsonSerializer.Deserialize<ComparisonReport>(share.Report));
        }).WithName("AccessShare");

        group.MapDelete("/{token}", (string token, ShareRepository repo) =>
        {
            if (!repo.Delete(token)) throw new FileApiException(404, "Share link not found.");
            return Results.NoContent();
        }).WithName("RevokeShare");

        return group;
    }

    /// <summary>Salt the password with the share token so equal passwords hash differently.</summary>
    private static string HashPassword(string token, string password) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"{token}:{password}")));

    private static void RequirePassword(ShareRecord share, string? supplied)
    {
        if (share.PasswordHash is null) return;

        if (string.IsNullOrEmpty(supplied))
            throw new FileApiException(401, "This share link is password-protected. Supply ?password=…");

        // Fixed-time comparison: a byte-by-byte string compare leaks how much of the
        // password matched, and these links are guessable at leisure by anyone holding
        // the URL.
        var candidate = HashPassword(share.Token, supplied);
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(candidate),
                Encoding.UTF8.GetBytes(share.PasswordHash)))
        {
            throw new FileApiException(401, "Incorrect password for this share link.");
        }
    }

    private static void RequireRecipient(ShareRecord share, HttpContext ctx)
    {
        if (string.Equals(share.AccessType, "anyone", StringComparison.OrdinalIgnoreCase))
            return;

        var allowed = DeserializeEmails(share.AllowedEmails);
        if (allowed is not { Count: > 0 }) return;

        var viewer = ctx.Items["user_email"] as string;
        if (viewer is not null && allowed.Contains(viewer, StringComparer.OrdinalIgnoreCase))
            return;

        throw new FileApiException(403,
            "This share link is restricted to named recipients. Sign in with an address it was shared with.");
    }

    private static List<string>? DeserializeEmails(string? json) =>
        json is null ? null : JsonSerializer.Deserialize<List<string>>(json);
}
