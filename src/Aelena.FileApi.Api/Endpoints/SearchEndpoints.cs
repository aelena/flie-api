using System.Text;
using Aelena.FileApi.Core.Services.Common;

namespace Aelena.FileApi.Api.Endpoints;

/// <summary>Universal cross-format search endpoint.</summary>
public static class SearchEndpoints
{
    public static RouteGroupBuilder MapSearchEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("", async (IFormFile file, string? query, string? pattern) =>
        {
            if (query is null && pattern is null)
                throw new FileApiException(400, "Provide either 'query' or 'pattern' (or both)");

            await using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var data = ms.ToArray();
            var fileName = file.FileName;
            var ext = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();

            // For now, treat all files as plain text. Phase 3+ will add PDF/DOCX section extraction.
            var text = Encoding.UTF8.GetString(data);
            var sectionType = ext switch
            {
                "pdf" => "page",
                "docx" => "paragraph",
                "xlsx" => "sheet",
                "pptx" => "slide",
                _ => "section"
            };

            var matches = TextSearch.Search(text, query: query, pattern: pattern);
            var sectionsWithMatches = matches.Where(m => m.Page.HasValue).Select(m => m.Page!.Value).Distinct().Order().ToList();

            return Results.Ok(new UniversalSearchResponse(
                FileName: fileName,
                FileType: ext,
                SectionType: sectionType,
                TotalMatches: matches.Count,
                SectionsWithMatches: sectionsWithMatches,
                Matches: matches));
        })
        .WithName("SearchDocument")
        .WithDescription("Search any document for literal text or regex matches.")
        .DisableAntiforgery()
        .Produces<UniversalSearchResponse>(200);

        return group;
    }
}
