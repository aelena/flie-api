namespace Aelena.FileApi.Core.Models;

/// <summary>EXIF and basic metadata from an image file.</summary>
public sealed record ExifResponse(
    string FileName,
    long FileSizeBytes,
    string? Format = null,
    int? Width = null,
    int? Height = null,
    string? Mode = null,
    IReadOnlyDictionary<string, string>? Exif = null,
    IReadOnlyDictionary<string, string>? Gps = null);

/// <summary>Text extracted from an image via OCR.</summary>
public sealed record ImageOcrResponse(
    string FileName,
    string Text,
    string Language,
    int WordCount,
    double? Confidence = null);

/// <summary>A single decoded QR code or barcode.</summary>
public sealed record DecodedCode(
    string Data,
    string Type,
    IReadOnlyDictionary<string, int>? Rect = null);

/// <summary>All QR codes and barcodes found in an image.</summary>
public sealed record QrDecodeResponse(
    string FileName,
    int TotalCodes,
    IReadOnlyList<DecodedCode> Codes);

/// <summary>A single colour in a palette.</summary>
public sealed record ColorInfo(
    string Hex,
    IReadOnlyList<int> Rgb,
    double Percentage);

/// <summary>Dominant colours extracted from an image.</summary>
public sealed record ColorPaletteResponse(
    string FileName,
    int NumColors,
    string DominantColor,
    IReadOnlyList<ColorInfo> Palette);

/// <summary>Perceptual hashes for image similarity / duplicate detection.</summary>
public sealed record PerceptualHashResponse(
    string FileName,
    string AverageHash,
    string PerceptualHash,
    string DifferenceHash,
    string WaveletHash);

/// <summary>Base64-encoded image for JSON/HTML embedding.</summary>
public sealed record Base64Response(
    string FileName,
    string MediaType,
    string Base64,
    string DataUri,
    long SizeBytes);

/// <summary>Natural-language description of an image (LLM-powered).</summary>
public sealed record ImageDescribeResponse(
    string FileName,
    string Description,
    string DetailLevel);

/// <summary>Auto-generated tags/keywords for an image (LLM-powered).</summary>
public sealed record ImageTagResponse(
    string FileName,
    IReadOnlyList<string> Tags);

/// <summary>A single object detected in an image.</summary>
public sealed record DetectedObject(
    string Name,
    string? Confidence = null);

/// <summary>All objects detected in an image (LLM-powered).</summary>
public sealed record ImageObjectsResponse(
    string FileName,
    int TotalObjects,
    IReadOnlyList<DetectedObject> Objects);

/// <summary>Content moderation assessment of an image (LLM-powered).</summary>
public sealed record ImageModerationResponse(
    string FileName,
    bool Safe,
    IReadOnlyDictionary<string, bool> Categories,
    string Reasoning);

/// <summary>Structured data extracted from an image (LLM-powered).</summary>
public sealed record ImageDataExtractionResponse(
    string FileName,
    string DataType,
    IReadOnlyDictionary<string, object> Fields);

/// <summary>Answer to a question about an image (LLM-powered).</summary>
public sealed record ImageAskResponse(
    string FileName,
    string Question,
    string Answer);
