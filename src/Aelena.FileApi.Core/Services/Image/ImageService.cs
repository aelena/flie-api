using Aelena.FileApi.Core.Errors;
using Aelena.FileApi.Core.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Aelena.FileApi.Core.Services.Image;

/// <summary>
/// Stateless image processing service built on SixLabors.ImageSharp.
/// Covers resize, rotate, crop, convert, thumbnail, flip, adjust, grayscale,
/// blur, compress, strip-metadata, EXIF extraction, auto-orient, invert, and base64.
/// </summary>
public static class ImageService
{
    // ── Read Operations ──────────────────────────────────────────────────

    /// <summary>Extract EXIF metadata from an image.</summary>
    public static ExifResponse GetExif(byte[] data, string fileName)
    {
        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(data);
        var exif = image.Metadata.ExifProfile;

        Dictionary<string, string>? exifDict = null;
        Dictionary<string, string>? gpsDict = null;

        if (exif is not null)
        {
            exifDict = [];
            gpsDict = [];
            foreach (var tag in exif.Values)
            {
                var name = tag.Tag.ToString();
                var value = tag.GetValue()?.ToString() ?? "";
                if (name.StartsWith("GPS", StringComparison.OrdinalIgnoreCase))
                    gpsDict[name] = value;
                else
                    exifDict[name] = value;
            }
            if (exifDict.Count == 0) exifDict = null;
            if (gpsDict.Count == 0) gpsDict = null;
        }

        var format = SixLabors.ImageSharp.Image.DetectFormat(data);

        return new ExifResponse(
            FileName: fileName,
            FileSizeBytes: data.Length,
            Format: format?.Name,
            Width: image.Width,
            Height: image.Height,
            Mode: image.PixelType.ToString(),
            Exif: exifDict,
            Gps: gpsDict);
    }

    /// <summary>Encode image to base64 data URI.</summary>
    public static Base64Response ToBase64(byte[] data, string fileName)
    {
        var format = SixLabors.ImageSharp.Image.DetectFormat(data);
        var mediaType = format?.DefaultMimeType ?? "application/octet-stream";
        var b64 = System.Convert.ToBase64String(data);
        return new Base64Response(fileName, mediaType, b64, $"data:{mediaType};base64,{b64}", data.Length);
    }

    // ── Transform Operations (return name, bytes, mediaType) ─────────────

    /// <summary>Resize an image. Provide width, height, or both.</summary>
    public static (string Name, byte[] Data, string MediaType) Resize(
        byte[] data, string fileName, int? width, int? height, bool maintainAspect = true)
    {
        if (width is null && height is null)
            throw new FileApiException(400, "Provide at least one of width or height.");

        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(data);

        var opts = new ResizeOptions
        {
            Size = new Size(width ?? 0, height ?? 0),
            Mode = maintainAspect ? ResizeMode.Max : ResizeMode.Stretch
        };
        image.Mutate(x => x.Resize(opts));

        return Save(image, fileName, data, "resized");
    }

    /// <summary>Rotate an image clockwise by the given angle.</summary>
    public static (string Name, byte[] Data, string MediaType) Rotate(
        byte[] data, string fileName, float angle)
    {
        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(data);
        image.Mutate(x => x.Rotate(angle));
        return Save(image, fileName, data, "rotated");
    }

    /// <summary>Crop an image to the given bounding box.</summary>
    public static (string Name, byte[] Data, string MediaType) Crop(
        byte[] data, string fileName, int left, int top, int right, int bottom)
    {
        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(data);

        if (right <= left || bottom <= top || right > image.Width || bottom > image.Height)
            throw new FileApiException(400, "Invalid crop bounds.");

        image.Mutate(x => x.Crop(new Rectangle(left, top, right - left, bottom - top)));
        return Save(image, fileName, data, "cropped");
    }

    /// <summary>Convert an image to a different format.</summary>
    public static (string Name, byte[] Data, string MediaType) Convert(
        byte[] data, string fileName, string targetFormat)
    {
        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(data);
        var (encoder, ext, mime) = GetEncoder(targetFormat);
        var stem = System.IO.Path.GetFileNameWithoutExtension(fileName);

        using var ms = new MemoryStream();
        image.Save(ms, encoder);
        return ($"{stem}.{ext}", ms.ToArray(), mime);
    }

    /// <summary>Generate a thumbnail preserving aspect ratio.</summary>
    public static (string Name, byte[] Data, string MediaType) Thumbnail(
        byte[] data, string fileName, int maxWidth = 200, int maxHeight = 200)
    {
        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(data);
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(maxWidth, maxHeight),
            Mode = ResizeMode.Max
        }));
        return Save(image, fileName, data, "thumb");
    }

    /// <summary>Flip an image horizontally or vertically.</summary>
    public static (string Name, byte[] Data, string MediaType) Flip(
        byte[] data, string fileName, string direction = "horizontal")
    {
        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(data);
        var mode = direction.ToLowerInvariant() switch
        {
            "horizontal" => FlipMode.Horizontal,
            "vertical" => FlipMode.Vertical,
            _ => throw new FileApiException(400, "Direction must be 'horizontal' or 'vertical'.")
        };
        image.Mutate(x => x.Flip(mode));
        return Save(image, fileName, data, "flipped");
    }

    /// <summary>Adjust brightness, contrast, and saturation.</summary>
    public static (string Name, byte[] Data, string MediaType) Adjust(
        byte[] data, string fileName, float brightness = 1f, float contrast = 1f,
        float sharpness = 1f, float saturation = 1f)
    {
        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(data);
        image.Mutate(x =>
        {
            if (Math.Abs(brightness - 1f) > 0.01f) x.Brightness(brightness);
            if (Math.Abs(contrast - 1f) > 0.01f) x.Contrast(contrast);
            if (Math.Abs(saturation - 1f) > 0.01f) x.Saturate(saturation);
            // ImageSharp doesn't have a direct sharpness multiplier; use GaussianSharpen
            if (sharpness > 1.1f) x.GaussianSharpen((sharpness - 1f) * 3f);
        });
        return Save(image, fileName, data, "adjusted");
    }

    /// <summary>Convert an image to grayscale.</summary>
    public static (string Name, byte[] Data, string MediaType) Grayscale(byte[] data, string fileName)
    {
        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(data);
        image.Mutate(x => x.Grayscale());
        return Save(image, fileName, data, "grayscale");
    }

    /// <summary>Apply Gaussian blur.</summary>
    public static (string Name, byte[] Data, string MediaType) Blur(
        byte[] data, string fileName, float radius = 2f)
    {
        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(data);
        image.Mutate(x => x.GaussianBlur(radius));
        return Save(image, fileName, data, "blurred");
    }

    /// <summary>Compress an image as JPEG with the given quality.</summary>
    public static (string Name, byte[] Data, string MediaType) Compress(
        byte[] data, string fileName, int quality = 85)
    {
        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(data);
        var stem = System.IO.Path.GetFileNameWithoutExtension(fileName);

        using var ms = new MemoryStream();
        image.Save(ms, new JpegEncoder { Quality = quality });
        return ($"{stem}_compressed.jpg", ms.ToArray(), "image/jpeg");
    }

    /// <summary>Remove all EXIF/metadata from an image.</summary>
    public static (string Name, byte[] Data, string MediaType) StripMetadata(byte[] data, string fileName)
    {
        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(data);
        image.Metadata.ExifProfile = null;
        image.Metadata.IccProfile = null;
        image.Metadata.IptcProfile = null;
        image.Metadata.XmpProfile = null;
        return Save(image, fileName, data, "stripped");
    }

    /// <summary>Fix image rotation based on EXIF orientation tag.</summary>
    public static (string Name, byte[] Data, string MediaType) AutoOrient(byte[] data, string fileName)
    {
        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(data);
        image.Mutate(x => x.AutoOrient());
        return Save(image, fileName, data, "oriented");
    }

    /// <summary>Invert all colors in an image.</summary>
    public static (string Name, byte[] Data, string MediaType) Invert(byte[] data, string fileName)
    {
        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(data);
        image.Mutate(x => x.Invert());
        return Save(image, fileName, data, "inverted");
    }

    /// <summary>Apply edge detection filter.</summary>
    public static (string Name, byte[] Data, string MediaType) EdgeDetect(byte[] data, string fileName)
    {
        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(data);
        image.Mutate(x => x.DetectEdges());
        return Save(image, fileName, data, "edges");
    }

    /// <summary>Apply histogram equalization for contrast improvement.</summary>
    public static (string Name, byte[] Data, string MediaType) Equalize(byte[] data, string fileName)
    {
        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(data);
        image.Mutate(x => x.HistogramEqualization());
        return Save(image, fileName, data, "equalized");
    }

    /// <summary>Extract dominant color palette by quantization.</summary>
    public static ColorPaletteResponse ExtractColorPalette(byte[] data, string fileName, int numColors = 8)
    {
        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(data);

        // Downsample for speed
        var small = image.Clone(x => x.Resize(64, 64));
        var colorCounts = new Dictionary<(byte R, byte G, byte B), int>();

        small.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    var pixel = row[x];
                    // Reduce color space by rounding to nearest 16
                    var key = ((byte)(pixel.R / 16 * 16), (byte)(pixel.G / 16 * 16), (byte)(pixel.B / 16 * 16));
                    colorCounts.TryGetValue(key, out var count);
                    colorCounts[key] = count + 1;
                }
            }
        });

        var totalPixels = (double)(small.Width * small.Height);
        var palette = colorCounts
            .OrderByDescending(kv => kv.Value)
            .Take(numColors)
            .Select(kv => new ColorInfo(
                Hex: $"#{kv.Key.R:x2}{kv.Key.G:x2}{kv.Key.B:x2}",
                Rgb: [kv.Key.R, kv.Key.G, kv.Key.B],
                Percentage: Math.Round(kv.Value / totalPixels * 100, 1)))
            .ToList();

        var dominant = palette.Count > 0 ? palette[0].Hex : "#000000";
        return new ColorPaletteResponse(fileName, palette.Count, dominant, palette);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static (string Name, byte[] Data, string MediaType) Save(
        Image<Rgba32> image, string fileName, byte[] originalData, string suffix)
    {
        var format = SixLabors.ImageSharp.Image.DetectFormat(originalData);
        var ext = format?.FileExtensions.FirstOrDefault() ?? "png";
        var mime = format?.DefaultMimeType ?? "image/png";
        var (encoder, _, _) = GetEncoder(ext);
        var stem = System.IO.Path.GetFileNameWithoutExtension(fileName);

        using var ms = new MemoryStream();
        image.Save(ms, encoder);
        return ($"{stem}_{suffix}.{ext}", ms.ToArray(), mime);
    }

    private static (IImageEncoder Encoder, string Ext, string Mime) GetEncoder(string format) =>
        format.ToLowerInvariant() switch
        {
            "png" => (new PngEncoder(), "png", "image/png"),
            "jpeg" or "jpg" => (new JpegEncoder(), "jpg", "image/jpeg"),
            "webp" => (new WebpEncoder(), "webp", "image/webp"),
            "bmp" => (new BmpEncoder(), "bmp", "image/bmp"),
            "gif" => (new GifEncoder(), "gif", "image/gif"),
            "tiff" or "tif" => (new TiffEncoder(), "tiff", "image/tiff"),
            _ => throw new FileApiException(400, $"Unsupported format: {format}. Use png, jpeg, webp, bmp, gif, or tiff.")
        };
}
