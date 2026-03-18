using Aelena.FileApi.Core.Services.Pdf;

namespace Aelena.FileApi.Api.Endpoints;

/// <summary>PDF toolkit endpoints — 40+ operations for reading, writing, and converting PDFs.</summary>
public static class PdfEndpoints
{
    public static RouteGroupBuilder MapPdfEndpoints(this RouteGroupBuilder group)
    {
        // ── Read Operations ──────────────────────────────────────────────

        group.MapPost("/metrics", async (IFormFile file) =>
        {
            var data = await ReadFile(file);
            return Results.Ok(PdfService.GetMetrics(data, file.FileName));
        }).WithName("PdfMetrics").DisableAntiforgery().Produces<PdfMetrics>(200);

        group.MapPost("/metadata", async (IFormFile file) =>
        {
            var data = await ReadFile(file);
            return Results.Ok(PdfService.GetMetadata(data, file.FileName));
        }).WithName("PdfMetadata").DisableAntiforgery().Produces<PdfMetadataResponse>(200);

        group.MapPost("/form-fields", async (IFormFile file) =>
        {
            var data = await ReadFile(file);
            return Results.Ok(PdfService.ExtractFormFields(data, file.FileName));
        }).WithName("PdfFormFields").DisableAntiforgery().Produces<FormFieldsResponse>(200);

        group.MapPost("/health", async (IFormFile file) =>
        {
            var data = await ReadFile(file);
            return Results.Ok(PdfService.HealthCheck(data, file.FileName));
        }).WithName("PdfHealth").DisableAntiforgery().Produces<PdfHealthResponse>(200);

        group.MapPost("/extract-text", async (IFormFile file, string? pages) =>
        {
            var data = await ReadFile(file);
            return Results.Ok(PdfService.ExtractText(data, file.FileName, pages));
        }).WithName("PdfExtractText").DisableAntiforgery().Produces<PdfTextResponse>(200);

        group.MapPost("/extract-pages", async (IFormFile file, string pages) =>
        {
            var data = await ReadFile(file);
            return Results.Ok(PdfService.ExtractPages(data, file.FileName, pages));
        }).WithName("PdfExtractPages").DisableAntiforgery().Produces<ExtractionResponse>(200);

        group.MapPost("/extract-markdown", async (IFormFile file, string? pages) =>
        {
            var data = await ReadFile(file);
            return Results.Ok(PdfService.ExtractToMarkdown(data, file.FileName, pages));
        }).WithName("PdfExtractMarkdown").DisableAntiforgery().Produces<MarkdownResponse>(200);

        group.MapPost("/extract-tables", async (IFormFile file, string? pages) =>
        {
            var data = await ReadFile(file);
            return Results.Ok(PdfService.ExtractTables(data, file.FileName, pages));
        }).WithName("PdfExtractTables").DisableAntiforgery().Produces<PdfTableResponse>(200);

        group.MapPost("/search", async (IFormFile file, string? query, string? pattern) =>
        {
            var data = await ReadFile(file);
            var (fileName, matches) = PdfService.Search(data, file.FileName, query, pattern);
            return Results.Ok(new SearchResponse(fileName, matches.Count, matches));
        }).WithName("PdfSearch").DisableAntiforgery().Produces<SearchResponse>(200);

        group.MapPost("/extract-annotations", async (IFormFile file) =>
        {
            var data = await ReadFile(file);
            return Results.Ok(PdfService.ExtractAnnotations(data, file.FileName));
        }).WithName("PdfExtractAnnotations").DisableAntiforgery();

        group.MapPost("/extract-bookmarks", async (IFormFile file) =>
        {
            var data = await ReadFile(file);
            return Results.Ok(PdfService.ExtractBookmarks(data, file.FileName));
        }).WithName("PdfExtractBookmarks").DisableAntiforgery();

        group.MapPost("/extract-form-data", async (IFormFile file) =>
        {
            var data = await ReadFile(file);
            return Results.Ok(PdfService.ExtractFormFields(data, file.FileName));
        }).WithName("PdfExtractFormData").DisableAntiforgery();

        // ── Write Operations ─────────────────────────────────────────────

        group.MapPost("/merge", async (IFormFileCollection files) =>
        {
            var pdfFiles = new List<(byte[] Data, string Name)>();
            foreach (var f in files)
            {
                var d = await ReadFile(f);
                pdfFiles.Add((d, f.FileName));
            }
            var (name, bytes) = PdfService.MergePdfs(pdfFiles);
            return Results.File(bytes, "application/pdf", name);
        }).WithName("PdfMerge").DisableAntiforgery();

        group.MapPost("/split", async (IFormFile file, string ranges) =>
        {
            var data = await ReadFile(file);
            var (name, zipBytes) = PdfService.SplitPdf(data, file.FileName, ranges);
            return Results.File(zipBytes, "application/zip", name);
        }).WithName("PdfSplit").DisableAntiforgery();

        group.MapPost("/rotate", async (IFormFile file, int angle, string? pages) =>
        {
            var data = await ReadFile(file);
            var (name, bytes) = PdfService.RotatePages(data, file.FileName, angle, pages);
            return Results.File(bytes, "application/pdf", name);
        }).WithName("PdfRotate").DisableAntiforgery();

        group.MapPost("/reorder", async (IFormFile file, string order) =>
        {
            var data = await ReadFile(file);
            var (name, bytes) = PdfService.ReorderPages(data, file.FileName, order);
            return Results.File(bytes, "application/pdf", name);
        }).WithName("PdfReorder").DisableAntiforgery();

        group.MapPost("/delete-pages", async (IFormFile file, string pages) =>
        {
            var data = await ReadFile(file);
            var (name, bytes) = PdfService.DeletePages(data, file.FileName, pages);
            return Results.File(bytes, "application/pdf", name);
        }).WithName("PdfDeletePages").DisableAntiforgery();

        group.MapPost("/watermark", async (IFormFile file, string text,
            string? color, float? opacity, int? fontSize, int? angle, string? position) =>
        {
            var data = await ReadFile(file);
            var (name, bytes) = PdfService.AddWatermark(data, file.FileName, text,
                color ?? "gray", opacity ?? 0.3f, fontSize ?? 60, angle ?? 45, position ?? "center");
            return Results.File(bytes, "application/pdf", name);
        }).WithName("PdfWatermark").DisableAntiforgery();

        group.MapPost("/remove-metadata", async (IFormFile file) =>
        {
            var data = await ReadFile(file);
            var (name, bytes) = PdfService.RemoveMetadata(data, file.FileName);
            return Results.File(bytes, "application/pdf", name);
        }).WithName("PdfRemoveMetadata").DisableAntiforgery();

        group.MapPost("/encrypt", async (IFormFile file, string userPassword, string? ownerPassword) =>
        {
            var data = await ReadFile(file);
            var (name, bytes) = PdfService.EncryptPdf(data, file.FileName, userPassword, ownerPassword);
            return Results.File(bytes, "application/pdf", name);
        }).WithName("PdfEncrypt").DisableAntiforgery();

        group.MapPost("/decrypt", async (IFormFile file, string password) =>
        {
            var data = await ReadFile(file);
            var (name, bytes) = PdfService.DecryptPdf(data, file.FileName, password);
            return Results.File(bytes, "application/pdf", name);
        }).WithName("PdfDecrypt").DisableAntiforgery();

        group.MapPost("/compress", async (IFormFile file, int? imageQuality, int? dpi) =>
        {
            var data = await ReadFile(file);
            var (name, bytes, origSize, compSize) = PdfService.CompressPdf(data, file.FileName, imageQuality ?? 80, dpi ?? 150);
            return Results.File(bytes, "application/pdf", name);
        }).WithName("PdfCompress").DisableAntiforgery();

        group.MapPost("/insert-blank-pages", async (IFormFile file, string after, int? count) =>
        {
            var data = await ReadFile(file);
            var (name, bytes) = PdfService.InsertBlankPages(data, file.FileName, after, count ?? 1);
            return Results.File(bytes, "application/pdf", name);
        }).WithName("PdfInsertBlankPages").DisableAntiforgery();

        group.MapPost("/page-numbers", async (IFormFile file, string? position, int? fontSize,
            int? start, int? margin, string? fontColor, string? fmt) =>
        {
            var data = await ReadFile(file);
            var (name, bytes) = PdfService.AddPageNumbers(data, file.FileName,
                position ?? "bottom-center", fontSize ?? 12, start ?? 1,
                margin ?? 36, fontColor ?? "black", fmt ?? "{n}");
            return Results.File(bytes, "application/pdf", name);
        }).WithName("PdfPageNumbers").DisableAntiforgery();

        group.MapPost("/unlock", async (IFormFile file) =>
        {
            var data = await ReadFile(file);
            var (name, bytes) = PdfService.UnlockPdf(data, file.FileName);
            return Results.File(bytes, "application/pdf", name);
        }).WithName("PdfUnlock").DisableAntiforgery();

        group.MapPost("/redact", async (IFormFile file, string terms, string? regex, string? pages, string? replacement) =>
        {
            var data = await ReadFile(file);
            var (name, bytes, count) = PdfService.RedactText(data, file.FileName, terms, regex, pages, replacement);
            return Results.File(bytes, "application/pdf", name);
        }).WithName("PdfRedact").DisableAntiforgery();

        // ── Conversion stubs (Phase 3c) ──────────────────────────────────

        group.MapPost("/ocr", (IFormFile file, string? engine, string? pages, string? language) =>
        {
            if (engine == "docling")
                throw new FileApiException(501, "Docling OCR engine is not available in the .NET version. Use 'tesseract' engine instead.");
            throw new FileApiException(501, "OCR requires Tesseract system dependency. Coming in Phase 3c.");
        }).WithName("PdfOcr").DisableAntiforgery();

        group.MapPost("/from-pptx", (IFormFile file) =>
            Stub501("PPTX to PDF requires LibreOffice. Coming in Phase 3c.")
        ).WithName("PdfFromPptx").DisableAntiforgery();

        group.MapPost("/from-xlsx", (IFormFile file) =>
            Stub501("XLSX to PDF requires LibreOffice. Coming in Phase 3c.")
        ).WithName("PdfFromXlsx").DisableAntiforgery();

        group.MapPost("/from-html", (IFormFile? file, string? html) =>
            Stub501("HTML to PDF requires DinkToPdf. Coming in Phase 3c.")
        ).WithName("PdfFromHtml").DisableAntiforgery();

        group.MapPost("/from-images", () =>
            Stub501("Images to PDF coming in Phase 3c.")
        ).WithName("PdfFromImages").DisableAntiforgery();

        group.MapPost("/convert-pdfa", (IFormFile file, int? level) =>
            Stub501("PDF/A requires Ghostscript. Coming in Phase 3c.")
        ).WithName("PdfConvertPdfa").DisableAntiforgery();

        group.MapPost("/convert-to-images", (IFormFile file, string? fmt, int? dpi, string? pages) =>
            Stub501("PDF to images coming in Phase 3c.")
        ).WithName("PdfConvertToImages").DisableAntiforgery();

        return group;
    }

    private static IResult Stub501(string message) =>
        throw new FileApiException(501, message);

    private static async Task<byte[]> ReadFile(IFormFile file)
    {
        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        return ms.ToArray();
    }
}
