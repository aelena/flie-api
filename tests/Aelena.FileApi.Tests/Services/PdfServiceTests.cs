using System.Text;
using Aelena.FileApi.Core.Errors;
using Aelena.FileApi.Core.Services.Pdf;
using FluentAssertions;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using Xunit;

namespace Aelena.FileApi.Tests.Services;

public class PdfServiceTests
{
    private static byte[] CreateSimplePdf(int pages = 1, string? text = null)
    {
        using var ms = new MemoryStream();
        using var writer = new PdfWriter(ms);
        using var doc = new PdfDocument(writer);
        using var layout = new Document(doc);

        for (var i = 0; i < pages; i++)
        {
            if (i > 0) layout.Add(new AreaBreak());
            layout.Add(new Paragraph(text ?? $"Page {i + 1} content. The quick brown fox jumps over the lazy dog."));
        }

        layout.Close();
        return ms.ToArray();
    }

    // ── Metrics ──────────────────────────────────────────────────────────

    [Fact]
    public void GetMetrics_SinglePage_ReturnsCorrectPageCount()
    {
        var data = CreateSimplePdf(1);
        var result = PdfService.GetMetrics(data, "test.pdf");

        result.PageCount.Should().Be(1);
        result.FileName.Should().Be("test.pdf");
        result.FileSizeBytes.Should().BeGreaterThan(0);
        result.WordCount.Should().BeGreaterThan(0);
        result.IsCorrupt.Should().BeFalse();
    }

    [Fact]
    public void GetMetrics_MultiPage_CountsAllPages()
    {
        var data = CreateSimplePdf(5);
        var result = PdfService.GetMetrics(data, "multi.pdf");

        result.PageCount.Should().Be(5);
    }

    [Fact]
    public void GetMetrics_CorruptData_ReturnsCorruptFlag()
    {
        var result = PdfService.GetMetrics(Encoding.UTF8.GetBytes("not a pdf"), "bad.pdf");

        result.IsCorrupt.Should().BeTrue();
        result.PageCount.Should().Be(0);
    }

    // ── Metadata ─────────────────────────────────────────────────────────

    [Fact]
    public void GetMetadata_ReturnsPageCountAndVersion()
    {
        var data = CreateSimplePdf(2);
        var result = PdfService.GetMetadata(data, "meta.pdf");

        result.PageCount.Should().Be(2);
        result.PdfVersion.Should().NotBeNullOrEmpty();
        result.PageSize.Should().NotBeNullOrEmpty();
    }

    // ── Text Extraction ──────────────────────────────────────────────────

    [Fact]
    public void ExtractText_AllPages_ReturnsAllContent()
    {
        var data = CreateSimplePdf(3);
        var result = PdfService.ExtractText(data, "text.pdf");

        result.TotalPages.Should().Be(3);
        result.Pages.Should().HaveCount(3);
        result.Pages[0].Text.Should().Contain("Page 1");
    }

    [Fact]
    public void ExtractText_SpecificPages_ReturnsSubset()
    {
        var data = CreateSimplePdf(5);
        var result = PdfService.ExtractText(data, "text.pdf", "1,3,5");

        result.Pages.Should().HaveCount(3);
        result.Pages[0].Page.Should().Be(1);
        result.Pages[1].Page.Should().Be(3);
        result.Pages[2].Page.Should().Be(5);
    }

    // ── Search ───────────────────────────────────────────────────────────

    [Fact]
    public void Search_LiteralQuery_FindsOnCorrectPage()
    {
        var data = CreateSimplePdf(3);
        var (_, matches) = PdfService.Search(data, "test.pdf", "Page 2", null);

        matches.Should().HaveCountGreaterThanOrEqualTo(1);
        matches.Any(m => m.Page == 2).Should().BeTrue();
    }

    [Fact]
    public void Search_NoQueryOrPattern_Throws()
    {
        var data = CreateSimplePdf(1);
        FluentActions.Invoking(() => PdfService.Search(data, "test.pdf", null, null))
            .Should().Throw<FileApiException>()
            .Where(e => e.StatusCode == 400);
    }

    // ── Merge ────────────────────────────────────────────────────────────

    [Fact]
    public void MergePdfs_TwoFiles_ProducesValidPdf()
    {
        var pdf1 = CreateSimplePdf(2, "Document One");
        var pdf2 = CreateSimplePdf(3, "Document Two");

        var (name, bytes) = PdfService.MergePdfs([(pdf1, "one.pdf"), (pdf2, "two.pdf")]);

        name.Should().Be("merged.pdf");
        bytes.Length.Should().BeGreaterThan(0);

        // Verify merged page count
        var metrics = PdfService.GetMetrics(bytes, "merged.pdf");
        metrics.PageCount.Should().Be(5);
    }

    [Fact]
    public void MergePdfs_LessThanTwo_Throws()
    {
        var pdf1 = CreateSimplePdf(1);
        FluentActions.Invoking(() => PdfService.MergePdfs([(pdf1, "one.pdf")]))
            .Should().Throw<FileApiException>()
            .Where(e => e.StatusCode == 400);
    }

    // ── Split ────────────────────────────────────────────────────────────

    [Fact]
    public void SplitPdf_TwoRanges_ProducesZipWithTwoParts()
    {
        var data = CreateSimplePdf(4);
        var (name, zipBytes) = PdfService.SplitPdf(data, "doc.pdf", "1-2;3-4");

        name.Should().Contain("split.zip");
        zipBytes.Length.Should().BeGreaterThan(0);

        // Verify ZIP contains 2 entries
        using var zip = new System.IO.Compression.ZipArchive(new MemoryStream(zipBytes));
        zip.Entries.Should().HaveCount(2);
    }

    // ── Rotate ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData(90)]
    [InlineData(180)]
    [InlineData(270)]
    public void RotatePages_ValidAngle_ProducesOutput(int angle)
    {
        var data = CreateSimplePdf(1);
        var (name, bytes) = PdfService.RotatePages(data, "test.pdf", angle);

        name.Should().Contain("rotated");
        bytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void RotatePages_InvalidAngle_Throws()
    {
        var data = CreateSimplePdf(1);
        FluentActions.Invoking(() => PdfService.RotatePages(data, "test.pdf", 45))
            .Should().Throw<FileApiException>()
            .Where(e => e.StatusCode == 400);
    }

    // ── Delete Pages ─────────────────────────────────────────────────────

    [Fact]
    public void DeletePages_RemovesCorrectPages()
    {
        var data = CreateSimplePdf(4);
        var (_, bytes) = PdfService.DeletePages(data, "test.pdf", "2,4");

        var metrics = PdfService.GetMetrics(bytes, "trimmed.pdf");
        metrics.PageCount.Should().Be(2);
    }

    [Fact]
    public void DeletePages_AllPages_Throws()
    {
        var data = CreateSimplePdf(2);
        FluentActions.Invoking(() => PdfService.DeletePages(data, "test.pdf", "1-2"))
            .Should().Throw<FileApiException>()
            .Where(e => e.StatusCode == 400);
    }

    // ── Watermark ────────────────────────────────────────────────────────

    [Fact]
    public void AddWatermark_ProducesOutput()
    {
        var data = CreateSimplePdf(1);
        var (name, bytes) = PdfService.AddWatermark(data, "test.pdf", "CONFIDENTIAL",
            "gray", 0.3f, 60, 45, "center");

        name.Should().Contain("watermarked");
        bytes.Length.Should().BeGreaterThan(0);
    }

    // ── Encrypt / Decrypt ────────────────────────────────────────────────

    [Fact]
    public void EncryptAndDecrypt_RoundTrips()
    {
        var data = CreateSimplePdf(1, "Secret content");
        var (_, encrypted) = PdfService.EncryptPdf(data, "test.pdf", "pass123", null);
        var (_, decrypted) = PdfService.DecryptPdf(encrypted, "test.pdf", "pass123");

        var text = PdfService.ExtractText(decrypted, "decrypted.pdf");
        text.Pages[0].Text.Should().Contain("Secret content");
    }

    [Fact]
    public void DecryptPdf_WrongPassword_Throws()
    {
        var data = CreateSimplePdf(1);
        var (_, encrypted) = PdfService.EncryptPdf(data, "test.pdf", "correct", null);

        FluentActions.Invoking(() => PdfService.DecryptPdf(encrypted, "test.pdf", "wrong"))
            .Should().Throw<FileApiException>()
            .Where(e => e.StatusCode == 422);
    }

    // ── Health Check ─────────────────────────────────────────────────────

    [Fact]
    public void HealthCheck_ValidPdf_ReturnsHealthy()
    {
        var data = CreateSimplePdf(1);
        var result = PdfService.HealthCheck(data, "test.pdf");

        result.Healthy.Should().BeTrue();
        result.ErrorCount.Should().Be(0);
    }

    // ── Compress ─────────────────────────────────────────────────────────

    [Fact]
    public void CompressPdf_ReturnsValidOutput()
    {
        var data = CreateSimplePdf(1);
        var (name, bytes, origSize, compSize) = PdfService.CompressPdf(data, "test.pdf", 80, 150);

        name.Should().Contain("compressed");
        bytes.Length.Should().BeGreaterThan(0);
        origSize.Should().Be(data.Length);
    }

    // ── Form Fields ──────────────────────────────────────────────────────

    [Fact]
    public void ExtractFormFields_NoForms_ReturnsEmpty()
    {
        var data = CreateSimplePdf(1);
        var result = PdfService.ExtractFormFields(data, "test.pdf");

        result.TotalFields.Should().Be(0);
        result.Fields.Should().BeEmpty();
    }

    // ── Markdown ─────────────────────────────────────────────────────────

    [Fact]
    public void ExtractToMarkdown_ContainsPageContent()
    {
        var data = CreateSimplePdf(2);
        var result = PdfService.ExtractToMarkdown(data, "test.pdf");

        result.Markdown.Should().Contain("Page 1");
        result.Markdown.Should().Contain("Page 2");
    }

    // ── Reorder ──────────────────────────────────────────────────────────

    [Fact]
    public void ReorderPages_ReverseOrder_ProducesOutput()
    {
        var data = CreateSimplePdf(3);
        var (_, bytes) = PdfService.ReorderPages(data, "test.pdf", "3,2,1");

        var text = PdfService.ExtractText(bytes, "reordered.pdf");
        text.Pages[0].Text.Should().Contain("Page 3");
    }
}
