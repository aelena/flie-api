using Aelena.FileApi.Core.Services.Common;

namespace Aelena.FileApi.Api.Endpoints;

/// <summary>Plain text file endpoints — metrics and search.</summary>
public static class TxtEndpoints
{
    public static RouteGroupBuilder MapTxtEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/metrics", async (IFormFile file) =>
        {
            await using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            return Results.Ok(TxtService.GetMetrics(ms.ToArray(), file.FileName));
        })
        .WithName("TxtMetrics")
        .WithDescription("Return word, character, token, and line counts for a TXT file.")
        .DisableAntiforgery()
        .Produces<TxtMetrics>(200);

        group.MapPost("/search", async (IFormFile file, string? query, string? pattern) =>
        {
            await using var ms = new MemoryStream();
            await file.CopyToAsync(ms);

            try
            {
                var (fileName, matches) = TxtService.Search(ms.ToArray(), file.FileName, query, pattern);
                return Results.Ok(new SearchResponse(fileName, matches.Count, matches));
            }
            catch (ArgumentException ex)
            {
                throw new FileApiException(400, ex.Message);
            }
        })
        .WithName("TxtSearch")
        .WithDescription("Search a TXT file for literal text or regex matches.")
        .DisableAntiforgery()
        .Produces<SearchResponse>(200);

        return group;
    }
}
