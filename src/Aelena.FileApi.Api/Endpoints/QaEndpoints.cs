namespace Aelena.FileApi.Api.Endpoints;

/// <summary>LLM-powered document QA endpoint.</summary>
public static class QaEndpoints
{
    public static RouteGroupBuilder MapQaEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("", (IFormFile file, string question, string? confidentiality) =>
            Stub("Document QA requires LLM integration. Coming soon.")
        ).WithName("DocumentQa").DisableAntiforgery();

        return group;
    }

    private static IResult Stub(string msg) => throw new FileApiException(501, msg);
}
