namespace Aelena.FileApi.Api.Endpoints;

/// <summary>Markdown to PDF conversion endpoint.</summary>
public static class MarkdownEndpoints
{
    public static RouteGroupBuilder MapMarkdownEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/to-pdf", (IFormFile file, string? css) =>
            Stub("Markdown to PDF requires DinkToPdf or Playwright. Coming soon.")
        ).WithName("MarkdownToPdf").DisableAntiforgery();

        return group;
    }

    private static IResult Stub(string msg) => throw new FileApiException(501, msg);
}
