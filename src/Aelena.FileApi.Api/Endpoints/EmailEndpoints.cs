using Aelena.FileApi.Core.Services.Common;

namespace Aelena.FileApi.Api.Endpoints;

/// <summary>Email parsing endpoint — extracts headers, body, and attachments from .eml/.msg files.</summary>
public static class EmailEndpoints
{
    public static RouteGroupBuilder MapEmailEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/parse", async (IFormFile file, bool? summarize) =>
        {
            await using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var result = EmailService.Parse(ms.ToArray(), file.FileName);

            // LLM-based summarization deferred to Phase 6
            if (summarize == true)
                throw new FileApiException(501,
                    "Email summarization requires LLM integration. Coming in Phase 6.");

            return Results.Ok(result);
        })
        .WithName("ParseEmail")
        .WithDescription("Parse an email file (.eml or .msg) and extract headers, body, and attachments.")
        .DisableAntiforgery()
        .Produces<EmailParseResponse>(200);

        return group;
    }
}
