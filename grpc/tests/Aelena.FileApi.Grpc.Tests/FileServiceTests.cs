using System.Text;
using Aelena.FileApi.Grpc.Proto;
using FluentAssertions;
using Google.Protobuf;
using Grpc.Net.Client;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Aelena.FileApi.Grpc.Tests;

public class FileServiceTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly FileService.FileServiceClient _client;

    public FileServiceTests(WebApplicationFactory<Program> factory)
    {
        var httpClient = factory.CreateDefaultClient();
        var channel = GrpcChannel.ForAddress(httpClient.BaseAddress!, new GrpcChannelOptions
        {
            HttpClient = httpClient
        });
        _client = new FileService.FileServiceClient(channel);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static byte[] MiniPdf(int pages = 1)
    {
        using var ms = new MemoryStream();
        using var w = new PdfWriter(ms);
        using var doc = new PdfDocument(w);
        using var layout = new Document(doc);
        for (var i = 0; i < pages; i++)
        {
            if (i > 0) layout.Add(new AreaBreak());
            layout.Add(new Paragraph($"Page {i + 1} content."));
        }
        layout.Close();
        return ms.ToArray();
    }

    private static byte[] MiniPng()
    {
        using var img = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(50, 50);
        using var ms = new MemoryStream();
        img.Save(ms, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
        return ms.ToArray();
    }

    // ── Hash ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Hash_ReturnsAllHashes()
    {
        var response = await _client.HashAsync(new FileRequest
        {
            FileName = "test.txt",
            Data = ByteString.CopyFrom("hello world"u8)
        });

        response.Sha256.Should().NotBeNullOrEmpty();
        response.Md5.Should().NotBeNullOrEmpty();
        response.Sha1.Should().NotBeNullOrEmpty();
        response.CompositeSha256.Should().NotBeNullOrEmpty();
        response.FileSizeBytes.Should().Be(11);
    }

    [Fact]
    public async Task Hash_KnownSha256()
    {
        var response = await _client.HashAsync(new FileRequest
        {
            FileName = "test.txt",
            Data = ByteString.CopyFrom("hello world"u8)
        });

        response.Sha256.Should().Be("b94d27b9934d3e08a52e52d7da7dabfac484efe37a5380ee9088f7ace2efcde9");
    }

    // ── PDF Metrics ──────────────────────────────────────────────────────

    [Fact]
    public async Task PdfMetrics_ReturnsPageCount()
    {
        var response = await _client.PdfMetricsAsync(new FileRequest
        {
            FileName = "test.pdf",
            Data = ByteString.CopyFrom(MiniPdf(3))
        });

        response.PageCount.Should().Be(3);
        response.WordCount.Should().BeGreaterThan(0);
        response.IsCorrupt.Should().BeFalse();
    }

    // ── TXT Metrics ──────────────────────────────────────────────────────

    [Fact]
    public async Task TxtMetrics_ReturnsLineCounts()
    {
        var response = await _client.TxtMetricsAsync(new FileRequest
        {
            FileName = "test.txt",
            Data = ByteString.CopyFromUtf8("line one\nline two\nline three")
        });

        response.LineCount.Should().Be(3);
        response.WordCount.Should().Be(6);
    }

    // ── PDF Rotate ───────────────────────────────────────────────────────

    [Fact]
    public async Task PdfRotate_ReturnsRotatedPdf()
    {
        var response = await _client.PdfRotateAsync(new PdfRotateRequest
        {
            FileName = "test.pdf",
            Data = ByteString.CopyFrom(MiniPdf()),
            Angle = 90
        });

        response.FileName.Should().Contain("rotated");
        response.Data.Length.Should().BeGreaterThan(0);
        response.ContentType.Should().Be("application/pdf");
    }

    // ── Image Resize ─────────────────────────────────────────────────────

    [Fact]
    public async Task ImageResize_ReturnsResizedImage()
    {
        var response = await _client.ImageResizeAsync(new ImageResizeRequest
        {
            FileName = "test.png",
            Data = ByteString.CopyFrom(MiniPng()),
            Width = 25,
            MaintainAspect = true
        });

        response.FileName.Should().Contain("resized");
        response.Data.Length.Should().BeGreaterThan(0);
    }

    // ── Image Convert ────────────────────────────────────────────────────

    [Fact]
    public async Task ImageConvert_ChangesFormat()
    {
        var response = await _client.ImageConvertAsync(new ImageConvertRequest
        {
            FileName = "test.png",
            Data = ByteString.CopyFrom(MiniPng()),
            TargetFormat = "jpeg"
        });

        response.ContentType.Should().Be("image/jpeg");
        response.FileName.Should().EndWith(".jpg");
    }

    // ── Image Compress ───────────────────────────────────────────────────

    [Fact]
    public async Task ImageCompress_ReturnsJpeg()
    {
        var response = await _client.ImageCompressAsync(new ImageCompressRequest
        {
            FileName = "test.png",
            Data = ByteString.CopyFrom(MiniPng()),
            Quality = 50
        });

        response.ContentType.Should().Be("image/jpeg");
    }
}
