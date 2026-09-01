using System.Text;
using Aelena.FileApi.Core.Services.Common;

namespace Aelena.FileApi.Api.Endpoints;

/// <summary>PII detection endpoint — regex-based scanning for personal data.</summary>
public static class PiiEndpoints
{
    public static RouteGroupBuilder MapPiiEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/detect", async (IFormFile file, HttpContext ctx, CancellationToken ct) =>
        {
            var text = Encoding.UTF8.GetString(await file.ReadAllBytesAsync(ctx, ct));
            return Results.Ok(PiiService.Detect(text, file.FileName));
        }).WithName("DetectPii").DisableAntiforgery().Produces<PiiDetectionResponse>(200);

        return group;
    }
}
