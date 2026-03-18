using System.Text;
using Aelena.FileApi.Core.Services.Common;

namespace Aelena.FileApi.Api.Endpoints;

/// <summary>Document readability scoring endpoint.</summary>
public static class ReadabilityEndpoints
{
    public static RouteGroupBuilder MapReadabilityEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("", async (IFormFile file, string? language) =>
        {
            await using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var text = Encoding.UTF8.GetString(ms.ToArray());

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
