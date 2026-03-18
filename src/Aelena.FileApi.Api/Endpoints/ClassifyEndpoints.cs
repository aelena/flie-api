namespace Aelena.FileApi.Api.Endpoints;

/// <summary>LLM-powered document classification endpoint.</summary>
public static class ClassifyEndpoints
{
    public static RouteGroupBuilder MapClassifyEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("", (IFormFile file, string? confidentiality) =>
            Stub("Document classification requires LLM integration. Coming soon.")
        ).WithName("ClassifyDocument").DisableAntiforgery();

        return group;
    }

    private static IResult Stub(string msg) => throw new FileApiException(501, msg);
}
