using Aelena.FileApi.Core.Services.Common;

namespace Aelena.FileApi.Api.Endpoints;

/// <summary>ZIP archive inspection endpoint.</summary>
public static class ZipEndpoints
{
    public static RouteGroupBuilder MapZipEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/inspect", async (IFormFile file, HttpContext ctx, CancellationToken ct) =>
        {
            return Results.Ok(ZipService.Inspect(await file.ReadAllBytesAsync(ctx, ct), file.FileName));
        })
        .WithName("ZipInspect")
        .WithDescription("List all entries inside a ZIP archive with sizes, compression, and CRC-32.")
        .DisableAntiforgery()
        .Produces<ZipInspectResponse>(200);

        return group;
    }
}
