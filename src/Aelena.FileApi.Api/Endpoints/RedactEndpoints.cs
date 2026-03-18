namespace Aelena.FileApi.Api.Endpoints;

/// <summary>PDF redaction endpoint — black-box text redaction.</summary>
public static class RedactEndpoints
{
    public static RouteGroupBuilder MapRedactEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("", (IFormFile file, string? query, string? pattern) =>
        {
            // Depends on iText7 for PDF manipulation — Phase 3 implementation.
            throw new FileApiException(501, "Redaction is not yet implemented. Coming in Phase 3 (PDF toolkit).");
        })
        .WithName("RedactDocument")
        .WithDescription("Redact matching text from a PDF by blacking it out.")
        .DisableAntiforgery()
        .Produces(200)
        .Produces<ProblemDetail>(501);

        return group;
    }
}
