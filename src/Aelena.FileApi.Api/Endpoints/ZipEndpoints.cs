using Aelena.FileApi.Core.Services.Common;

namespace Aelena.FileApi.Api.Endpoints;

/// <summary>ZIP archive inspection endpoint.</summary>
public static class ZipEndpoints
{
    public static RouteGroupBuilder MapZipEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/inspect", async (IFormFile file) =>
        {
            await using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            return Results.Ok(ZipService.Inspect(ms.ToArray(), file.FileName));
        })
        .WithName("ZipInspect")
        .WithDescription("List all entries inside a ZIP archive with sizes, compression, and CRC-32.")
        .DisableAntiforgery()
        .Produces<ZipInspectResponse>(200);

        return group;
    }
}
