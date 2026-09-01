using Aelena.FileApi.Core.Services.Common;

namespace Aelena.FileApi.Api.Endpoints;

/// <summary>Plain text file endpoints — metrics and search.</summary>
public static class TxtEndpoints
{
    public static RouteGroupBuilder MapTxtEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/metrics", async (IFormFile file, HttpContext ctx, CancellationToken ct) =>
        {
            return Results.Ok(TxtService.GetMetrics(await file.ReadAllBytesAsync(ctx, ct), file.FileName));
        })
        .WithName("TxtMetrics")
        .WithDescription("Return word, character, token, and line counts for a TXT file.")
        .DisableAntiforgery()
        .Produces<TxtMetrics>(200);

        group.MapPost("/search", async (IFormFile file, string? query, string? pattern,
            HttpContext ctx, CancellationToken ct) =>
        {
            // TextSearch reports a bad query/pattern combination as a 400 itself now,
            // so the ArgumentException translation that used to live here is gone.
            var (fileName, matches) = TxtService.Search(
                await file.ReadAllBytesAsync(ctx, ct), file.FileName, query, pattern);

            return Results.Ok(new SearchResponse(fileName, matches.Count, matches));
        })
        .WithName("TxtSearch")
        .WithDescription("Search a TXT file for literal text or regex matches.")
        .DisableAntiforgery()
        .Produces<SearchResponse>(200);

        return group;
    }
}
