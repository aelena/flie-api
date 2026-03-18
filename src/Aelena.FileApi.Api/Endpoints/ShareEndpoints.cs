using System.Security.Cryptography;
using System.Text;
using Aelena.FileApi.Core.Services.Persistence;

namespace Aelena.FileApi.Api.Endpoints;

/// <summary>Share link CRUD endpoints — create, access, list, and revoke shareable report links.</summary>
public static class ShareEndpoints
{
    public static RouteGroupBuilder MapShareEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("", (CreateShareRequest request, ShareRepository repo) =>
        {
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(12)).ToLowerInvariant();
            var reportJson = System.Text.Json.JsonSerializer.Serialize(request.Report);
            var pwHash = request.Password is not null
                ? Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{token}:{request.Password}"))).ToLowerInvariant()
                : null;
            var emailsJson = request.AllowedEmails is { Count: > 0 }
                ? System.Text.Json.JsonSerializer.Serialize(request.AllowedEmails)
                : null;

            repo.Create(token, request.Report.JobId, reportJson,
                request.AccessType, emailsJson, pwHash, request.ExpiresAt);

            return Results.Created($"/share/{token}", new CreateShareResponse(token, $"/share/{token}"));
        }).WithName("CreateShare").DisableAntiforgery();

        group.MapGet("/job/{jobId}", (string jobId, ShareRepository repo) =>
        {
            var shares = repo.ListForJob(jobId).Select(r => new ShareMetadata(
                (string)r.token, (string)r.access_type, (string)r.created_at,
                r.expires_at as string, (int)(long)r.access_count,
                r.password_hash is not null,
                r.allowed_emails is not null
                    ? System.Text.Json.JsonSerializer.Deserialize<List<string>>((string)r.allowed_emails)
                    : null
            )).ToList();
            return Results.Ok(shares);
        }).WithName("ListSharesForJob");

        group.MapGet("/{token}", (string token, ShareRepository repo) =>
        {
            var row = repo.GetByToken(token);
            if (row is null) throw new FileApiException(404, "Share link not found.");

            repo.IncrementAccessCount(token);
            var report = System.Text.Json.JsonSerializer.Deserialize<ComparisonReport>((string)row.report);
            return Results.Ok(report);
        }).WithName("AccessShare");

        group.MapDelete("/{token}", (string token, ShareRepository repo) =>
        {
            if (!repo.Delete(token)) throw new FileApiException(404, "Share link not found.");
            return Results.NoContent();
        }).WithName("RevokeShare");

        return group;
    }
}
