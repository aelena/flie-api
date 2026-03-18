using Aelena.FileApi.Core.Services.Image;

namespace Aelena.FileApi.Api.Endpoints;

/// <summary>Image processing endpoints — resize, rotate, crop, convert, blur, grayscale, etc.</summary>
public static class ImageEndpoints
{
    public static RouteGroupBuilder MapImageEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/exif", async (IFormFile file) =>
            Results.Ok(ImageService.GetExif(await Read(file), file.FileName))
        ).WithName("ImageExif").DisableAntiforgery();

        group.MapPost("/resize", async (IFormFile file, int? width, int? height, bool? maintainAspect) =>
        {
            var (n, d, m) = ImageService.Resize(await Read(file), file.FileName, width, height, maintainAspect ?? true);
            return Results.File(d, m, n);
        }).WithName("ImageResize").DisableAntiforgery();

        group.MapPost("/rotate", async (IFormFile file, float angle) =>
        {
            var (n, d, m) = ImageService.Rotate(await Read(file), file.FileName, angle);
            return Results.File(d, m, n);
        }).WithName("ImageRotate").DisableAntiforgery();

        group.MapPost("/watermark", (IFormFile file, string text) =>
            Stub501("Image text watermark requires font rendering. Coming soon.")
        ).WithName("ImageWatermark").DisableAntiforgery();

        group.MapPost("/crop", async (IFormFile file, int left, int top, int right, int bottom) =>
        {
            var (n, d, m) = ImageService.Crop(await Read(file), file.FileName, left, top, right, bottom);
            return Results.File(d, m, n);
        }).WithName("ImageCrop").DisableAntiforgery();

        group.MapPost("/convert", async (IFormFile file, string format) =>
        {
            var (n, d, m) = ImageService.Convert(await Read(file), file.FileName, format);
            return Results.File(d, m, n);
        }).WithName("ImageConvert").DisableAntiforgery();

        group.MapPost("/thumbnail", async (IFormFile file, int? maxWidth, int? maxHeight) =>
        {
            var (n, d, m) = ImageService.Thumbnail(await Read(file), file.FileName, maxWidth ?? 200, maxHeight ?? 200);
            return Results.File(d, m, n);
        }).WithName("ImageThumbnail").DisableAntiforgery();

        group.MapPost("/flip", async (IFormFile file, string? direction) =>
        {
            var (n, d, m) = ImageService.Flip(await Read(file), file.FileName, direction ?? "horizontal");
            return Results.File(d, m, n);
        }).WithName("ImageFlip").DisableAntiforgery();

        group.MapPost("/adjust", async (IFormFile file, float? brightness, float? contrast, float? sharpness, float? saturation) =>
        {
            var (n, d, m) = ImageService.Adjust(await Read(file), file.FileName,
                brightness ?? 1f, contrast ?? 1f, sharpness ?? 1f, saturation ?? 1f);
            return Results.File(d, m, n);
        }).WithName("ImageAdjust").DisableAntiforgery();

        group.MapPost("/grayscale", async (IFormFile file) =>
        {
            var (n, d, m) = ImageService.Grayscale(await Read(file), file.FileName);
            return Results.File(d, m, n);
        }).WithName("ImageGrayscale").DisableAntiforgery();

        group.MapPost("/blur", async (IFormFile file, float? radius) =>
        {
            var (n, d, m) = ImageService.Blur(await Read(file), file.FileName, radius ?? 2f);
            return Results.File(d, m, n);
        }).WithName("ImageBlur").DisableAntiforgery();

        group.MapPost("/compress", async (IFormFile file, int? quality) =>
        {
            var (n, d, m) = ImageService.Compress(await Read(file), file.FileName, quality ?? 85);
            return Results.File(d, m, n);
        }).WithName("ImageCompress").DisableAntiforgery();

        group.MapPost("/strip-metadata", async (IFormFile file) =>
        {
            var (n, d, m) = ImageService.StripMetadata(await Read(file), file.FileName);
            return Results.File(d, m, n);
        }).WithName("ImageStripMetadata").DisableAntiforgery();

        return group;
    }

    private static IResult Stub501(string msg) => throw new FileApiException(501, msg);

    private static async Task<byte[]> Read(IFormFile file)
    {
        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        return ms.ToArray();
    }
}
