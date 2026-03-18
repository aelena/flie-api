using Aelena.FileApi.Core.Services.Common;

namespace Aelena.FileApi.Api.Endpoints;

/// <summary>File hashing endpoints — SHA-256, MD5, SHA-1, and composite hashes.</summary>
public static class HashEndpoints
{
    public static RouteGroupBuilder MapHashEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("", async (IFormFile file) =>
        {
            await using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var data = ms.ToArray();
            return Results.Ok(HashService.ComputeHash(data, file.FileName, file.ContentType));
        })
        .WithName("FileHash")
        .WithDescription("Generate SHA-256, MD5, SHA-1, and composite hashes for any file.")
        .DisableAntiforgery()
        .Produces<FileHashResponse>(200);

        return group;
    }
}
