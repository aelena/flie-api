using Aelena.FileApi.Core.Services.Image;

namespace Aelena.FileApi.Api.Endpoints;

/// <summary>Image AI endpoints — local ops (auto-orient, edge-detect, color palette, etc.) + LLM vision stubs.</summary>
public static class ImageAiEndpoints
{
    public static RouteGroupBuilder MapImageAiEndpoints(this RouteGroupBuilder group)
    {
        // ── Local operations (ImageSharp) ────────────────────────────────

        group.MapPost("/auto-orient", async (IFormFile file) =>
        {
            var (n, d, m) = ImageService.AutoOrient(await Read(file), file.FileName);
            return Results.File(d, m, n);
        }).WithName("ImageAiAutoOrient").DisableAntiforgery();

        group.MapPost("/edge-detect", async (IFormFile file) =>
        {
            var (n, d, m) = ImageService.EdgeDetect(await Read(file), file.FileName);
            return Results.File(d, m, n);
        }).WithName("ImageAiEdgeDetect").DisableAntiforgery();

        group.MapPost("/equalize", async (IFormFile file) =>
        {
            var (n, d, m) = ImageService.Equalize(await Read(file), file.FileName);
            return Results.File(d, m, n);
        }).WithName("ImageAiEqualize").DisableAntiforgery();

        group.MapPost("/invert", async (IFormFile file) =>
        {
            var (n, d, m) = ImageService.Invert(await Read(file), file.FileName);
            return Results.File(d, m, n);
        }).WithName("ImageAiInvert").DisableAntiforgery();

        group.MapPost("/color-palette", async (IFormFile file, int? numColors) =>
            Results.Ok(ImageService.ExtractColorPalette(await Read(file), file.FileName, numColors ?? 8))
        ).WithName("ImageAiColorPalette").DisableAntiforgery();

        group.MapPost("/base64", async (IFormFile file) =>
            Results.Ok(ImageService.ToBase64(await Read(file), file.FileName))
        ).WithName("ImageAiBase64").DisableAntiforgery();

        // ── Stubs for operations requiring external dependencies ─────────

        Stub(group, "/ocr", "ImageAiOcr", "Image OCR requires Tesseract system dependency.");
        Stub(group, "/qr-decode", "ImageAiQrDecode", "QR decode requires ZXing.Net. Coming in Phase 5b.");
        Stub(group, "/perceptual-hash", "ImageAiPerceptualHash", "Perceptual hashing deferred to separate project.");
        Stub(group, "/sepia", "ImageAiSepia", "Sepia filter coming soon.");
        Stub(group, "/border", "ImageAiBorder", "Border coming soon.");
        Stub(group, "/overlay", "ImageAiOverlay", "Overlay coming soon.");
        Stub(group, "/pixelate", "ImageAiPixelate", "Pixelate coming soon.");
        Stub(group, "/montage", "ImageAiMontage", "Montage coming soon.");

        // ── LLM Vision stubs ─────────────────────────────────────────────

        Stub(group, "/describe", "ImageAiDescribe", "LLM vision describe. Coming in Phase 6.");
        Stub(group, "/tag", "ImageAiTag", "LLM vision tagging. Coming in Phase 6.");
        Stub(group, "/objects", "ImageAiObjects", "LLM vision object detection. Coming in Phase 6.");
        Stub(group, "/moderate", "ImageAiModerate", "LLM vision moderation. Coming in Phase 6.");
        Stub(group, "/extract-data", "ImageAiExtractData", "LLM vision data extraction. Coming in Phase 6.");
        Stub(group, "/ask", "ImageAiAsk", "LLM vision Q&A. Coming in Phase 6.");

        return group;
    }

    private static IResult ThrowStub(string message) => throw new FileApiException(501, message);

    private static void Stub(RouteGroupBuilder group, string path, string name, string message) =>
        group.MapPost(path, () => ThrowStub(message)).WithName(name).DisableAntiforgery();

    private static async Task<byte[]> Read(IFormFile file)
    {
        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        return ms.ToArray();
    }
}
