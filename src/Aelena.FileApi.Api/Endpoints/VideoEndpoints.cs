namespace Aelena.FileApi.Api.Endpoints;

/// <summary>Video metadata extraction endpoint.</summary>
public static class VideoEndpoints
{
    public static RouteGroupBuilder MapVideoEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/metadata", (IFormFile file) =>
            Stub("Video metadata requires MediaInfo system library. Coming soon.")
        ).WithName("VideoMetadata").DisableAntiforgery();

        return group;
    }

    private static IResult Stub(string msg) => throw new FileApiException(501, msg);
}
