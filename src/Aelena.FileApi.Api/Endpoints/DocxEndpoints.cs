using Aelena.FileApi.Core.Services.Docx;

namespace Aelena.FileApi.Api.Endpoints;

/// <summary>DOCX processing endpoints — metrics, metadata, extraction, search, health, markdown.</summary>
public static class DocxEndpoints
{
    private const string DocxMime = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    public static RouteGroupBuilder MapDocxEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/metrics", async (IFormFile file) =>
            Results.Ok(DocxService.GetMetrics(await Read(file), file.FileName))
        ).WithName("DocxMetrics").DisableAntiforgery().Produces<DocxMetrics>(200);

        group.MapPost("/extract-pages", async (IFormFile file, string pages) =>
            Results.Ok(DocxService.ExtractPages(await Read(file), file.FileName, pages))
        ).WithName("DocxExtractPages").DisableAntiforgery().Produces<ExtractionResponse>(200);

        group.MapPost("/metadata", async (IFormFile file) =>
            Results.Ok(DocxService.GetMetadata(await Read(file), file.FileName))
        ).WithName("DocxMetadata").DisableAntiforgery().Produces<DocxMetadataResponse>(200);

        group.MapPost("/extract-markdown", async (IFormFile file) =>
            Results.Ok(DocxService.ExtractToMarkdown(await Read(file), file.FileName))
        ).WithName("DocxExtractMarkdown").DisableAntiforgery().Produces<DocxMarkdownResponse>(200);

        group.MapPost("/health", async (IFormFile file) =>
            Results.Ok(DocxService.HealthCheck(await Read(file), file.FileName))
        ).WithName("DocxHealth").DisableAntiforgery().Produces<DocxHealthResponse>(200);

        group.MapPost("/search", async (IFormFile file, string? query, string? pattern) =>
        {
            var (name, matches) = DocxService.Search(await Read(file), file.FileName, query, pattern);
            return Results.Ok(new SearchResponse(name, matches.Count, matches));
        }).WithName("DocxSearch").DisableAntiforgery().Produces<SearchResponse>(200);

        group.MapPost("/remove-metadata", async (IFormFile file) =>
        {
            var (name, bytes) = DocxService.RemoveMetadata(await Read(file), file.FileName);
            return Results.File(bytes, DocxMime, name);
        }).WithName("DocxRemoveMetadata").DisableAntiforgery();

        group.MapPost("/convert-pdf", async (IFormFile file) =>
        {
            var (name, bytes) = DocxService.ConvertToPdf(await Read(file), file.FileName);
            return Results.File(bytes, "application/pdf", name);
        }).WithName("DocxConvertPdf").DisableAntiforgery();

        group.MapPost("/watermark", async (IFormFile file, string text,
            string? color, int? fontSize, int? angle) =>
        {
            var (name, bytes) = DocxService.AddWatermark(await Read(file), file.FileName,
                text, color ?? "silver", fontSize ?? 72, angle ?? -45);
            return Results.File(bytes, DocxMime, name);
        }).WithName("DocxWatermark").DisableAntiforgery();

        group.MapPost("/remove-watermark", async (IFormFile file) =>
        {
            var (name, bytes) = DocxService.RemoveWatermark(await Read(file), file.FileName);
            return Results.File(bytes, DocxMime, name);
        }).WithName("DocxRemoveWatermark").DisableAntiforgery();

        return group;
    }

    private static async Task<byte[]> Read(IFormFile file)
    {
        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        return ms.ToArray();
    }
}
