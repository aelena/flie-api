using System.Text;
using Aelena.FileApi.Core.Services.Common;

namespace Aelena.FileApi.Api.Endpoints;

/// <summary>Document readability scoring endpoint.</summary>
public static class ReadabilityEndpoints
{
    public static RouteGroupBuilder MapReadabilityEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("", async (IFormFile file, string? language, HttpContext ctx, CancellationToken ct) =>
        {
            var text = Encoding.UTF8.GetString(await file.ReadAllBytesAsync(ctx, ct));

            if (string.IsNullOrWhiteSpace(text))
                throw new FileApiException(400, "Document contains no extractable text");

            return Results.Ok(ReadabilityService.Analyse(text, file.FileName, language ?? "en"));
        })
        .WithName("DocumentReadability")
        .WithDescription("Compute Flesch, Gunning Fog, and SMOG readability scores.")
        .DisableAntiforgery()
        .Produces<ReadabilityResponse>(200);

        return group;
    }
}
