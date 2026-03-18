using Aelena.FileApi.Core.Errors;
using Aelena.FileApi.Core.Services.Image;
using FluentAssertions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Aelena.FileApi.Tests.Services;

public class ImageServiceTests
{
    private static byte[] CreateTestImage(int width = 100, int height = 80)
    {
        using var image = new Image<Rgba32>(width, height, new Rgba32(255, 0, 0, 255)); // red
        // Add some variation for color palette tests
        for (var x = 0; x < width / 2; x++)
            for (var y = 0; y < height; y++)
                image[x, y] = new Rgba32(0, 0, 255, 255); // blue left half

        using var ms = new MemoryStream();
        image.Save(ms, new PngEncoder());
        return ms.ToArray();
    }

    // ── EXIF ─────────────────────────────────────────────────────────────

    [Fact]
    public void GetExif_ReturnsWidthAndHeight()
    {
        var data = CreateTestImage(200, 150);
        var result = ImageService.GetExif(data, "test.png");

        result.Width.Should().Be(200);
        result.Height.Should().Be(150);
        result.Format.Should().Be("PNG");
        result.FileSizeBytes.Should().BeGreaterThan(0);
    }

    // ── Resize ───────────────────────────────────────────────────────────

    [Fact]
    public void Resize_ByWidth_MaintainsAspect()
    {
        var data = CreateTestImage(200, 100);
        var (name, bytes, mime) = ImageService.Resize(data, "img.png", width: 100, height: null);

        name.Should().Contain("resized");
        mime.Should().Contain("png");

        using var result = Image.Load<Rgba32>(bytes);
        result.Width.Should().Be(100);
        result.Height.Should().Be(50); // aspect ratio preserved
    }

    [Fact]
    public void Resize_NoWidthOrHeight_Throws()
    {
        var data = CreateTestImage();
        FluentActions.Invoking(() => ImageService.Resize(data, "img.png", null, null))
            .Should().Throw<FileApiException>();
    }

    // ── Rotate ───────────────────────────────────────────────────────────

    [Fact]
    public void Rotate_90Degrees_SwapsDimensions()
    {
        var data = CreateTestImage(100, 50);
        var (_, bytes, _) = ImageService.Rotate(data, "img.png", 90);

        using var result = Image.Load<Rgba32>(bytes);
        result.Width.Should().Be(50);
        result.Height.Should().Be(100);
    }

    // ── Crop ─────────────────────────────────────────────────────────────

    [Fact]
    public void Crop_ValidBounds_ReturnsCroppedImage()
    {
        var data = CreateTestImage(100, 80);
        var (_, bytes, _) = ImageService.Crop(data, "img.png", 10, 10, 50, 50);

        using var result = Image.Load<Rgba32>(bytes);
        result.Width.Should().Be(40);
        result.Height.Should().Be(40);
    }

    [Fact]
    public void Crop_InvalidBounds_Throws()
    {
        var data = CreateTestImage(100, 80);
        FluentActions.Invoking(() => ImageService.Crop(data, "img.png", 50, 50, 10, 10))
            .Should().Throw<FileApiException>();
    }

    // ── Convert ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("jpeg", "image/jpeg")]
    [InlineData("bmp", "image/bmp")]
    [InlineData("gif", "image/gif")]
    public void Convert_ToFormat_ChangesMediaType(string format, string expectedMime)
    {
        var data = CreateTestImage();
        var (name, _, mime) = ImageService.Convert(data, "img.png", format);

        mime.Should().Be(expectedMime);
        name.Should().EndWith($".{(format == "jpeg" ? "jpg" : format)}");
    }

    [Fact]
    public void Convert_UnsupportedFormat_Throws()
    {
        var data = CreateTestImage();
        FluentActions.Invoking(() => ImageService.Convert(data, "img.png", "xyz"))
            .Should().Throw<FileApiException>();
    }

    // ── Thumbnail ────────────────────────────────────────────────────────

    [Fact]
    public void Thumbnail_FitsWithinBounds()
    {
        var data = CreateTestImage(400, 300);
        var (_, bytes, _) = ImageService.Thumbnail(data, "big.png", 100, 100);

        using var result = Image.Load<Rgba32>(bytes);
        result.Width.Should().BeLessThanOrEqualTo(100);
        result.Height.Should().BeLessThanOrEqualTo(100);
    }

    // ── Flip ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("horizontal")]
    [InlineData("vertical")]
    public void Flip_ProducesOutput(string direction)
    {
        var data = CreateTestImage();
        var (name, bytes, _) = ImageService.Flip(data, "img.png", direction);
        bytes.Length.Should().BeGreaterThan(0);
        name.Should().Contain("flipped");
    }

    // ── Grayscale ────────────────────────────────────────────────────────

    [Fact]
    public void Grayscale_ProducesOutput()
    {
        var data = CreateTestImage();
        var (name, bytes, _) = ImageService.Grayscale(data, "img.png");
        bytes.Length.Should().BeGreaterThan(0);
        name.Should().Contain("grayscale");
    }

    // ── Blur ─────────────────────────────────────────────────────────────

    [Fact]
    public void Blur_ProducesOutput()
    {
        var data = CreateTestImage();
        var (_, bytes, _) = ImageService.Blur(data, "img.png", 3f);
        bytes.Length.Should().BeGreaterThan(0);
    }

    // ── Compress ─────────────────────────────────────────────────────────

    [Fact]
    public void Compress_ProducesJpeg()
    {
        var data = CreateTestImage();
        var (name, _, mime) = ImageService.Compress(data, "img.png", 50);
        mime.Should().Be("image/jpeg");
        name.Should().Contain("compressed");
    }

    // ── StripMetadata ────────────────────────────────────────────────────

    [Fact]
    public void StripMetadata_ProducesOutput()
    {
        var data = CreateTestImage();
        var (name, bytes, _) = ImageService.StripMetadata(data, "img.png");
        name.Should().Contain("stripped");
        bytes.Length.Should().BeGreaterThan(0);
    }

    // ── AutoOrient, Invert, EdgeDetect, Equalize ─────────────────────────

    [Fact]
    public void AutoOrient_ProducesOutput()
    {
        var (_, bytes, _) = ImageService.AutoOrient(CreateTestImage(), "img.png");
        bytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Invert_ProducesOutput()
    {
        var (_, bytes, _) = ImageService.Invert(CreateTestImage(), "img.png");
        bytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void EdgeDetect_ProducesOutput()
    {
        var (_, bytes, _) = ImageService.EdgeDetect(CreateTestImage(), "img.png");
        bytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Equalize_ProducesOutput()
    {
        var (_, bytes, _) = ImageService.Equalize(CreateTestImage(), "img.png");
        bytes.Length.Should().BeGreaterThan(0);
    }

    // ── Base64 ───────────────────────────────────────────────────────────

    [Fact]
    public void ToBase64_ReturnsDataUri()
    {
        var data = CreateTestImage();
        var result = ImageService.ToBase64(data, "img.png");
        result.DataUri.Should().StartWith("data:image/png;base64,");
        result.Base64.Should().NotBeNullOrEmpty();
        result.SizeBytes.Should().Be(data.Length);
    }

    // ── Color Palette ────────────────────────────────────────────────────

    [Fact]
    public void ExtractColorPalette_ReturnsColors()
    {
        var data = CreateTestImage();
        var result = ImageService.ExtractColorPalette(data, "img.png", 4);

        result.NumColors.Should().BeGreaterThan(0);
        result.DominantColor.Should().StartWith("#");
        result.Palette.Should().NotBeEmpty();
    }
}
