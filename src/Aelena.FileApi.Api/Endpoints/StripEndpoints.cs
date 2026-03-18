namespace Aelena.FileApi.Api.Endpoints;

/// <summary>Image stripping endpoints — remove all images from PDF or DOCX.</summary>
public static class StripEndpoints
{
    public static RouteGroupBuilder MapStripEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/images", (IFormFile file) =>
        {
            // Depends on iText7 (PDF) and Open XML SDK (DOCX) — Phase 3/4 implementation.
            throw new FileApiException(501, "Strip images is not yet implemented. Coming in Phase 3 (PDF) and Phase 4 (DOCX).");
        })
        .WithName("StripImages")
        .WithDescription("Remove all images from a PDF or DOCX file.")
        .DisableAntiforgery()
        .Produces(200)
        .Produces<ProblemDetail>(501);

        return group;
    }
}
