using System.Text;
using Aelena.FileApi.Core.Services.Common;
using Aelena.FileApi.Core.Services.Image;
using Aelena.FileApi.Core.Services.Pdf;
using FluentAssertions;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Aelena.FileApi.Tests.Services;

/// <summary>
/// Proves that all static Core services are thread-safe by running
/// N parallel tasks with different data and verifying no cross-contamination.
/// </summary>
public class ConcurrencyTests
{
    private const int Parallelism = 20;

    // ── Hash ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task HashService_ParallelCalls_ProduceCorrectResults()
    {
        var tasks = Enumerable.Range(0, Parallelism).Select(i =>
        {
            var data = Encoding.UTF8.GetBytes($"unique content {i}");
            return Task.Run(() => HashService.ComputeHash(data, $"file{i}.txt"));
        }).ToArray();

        var results = await Task.WhenAll(tasks);

        // All hashes must be unique (different content)
        results.Select(r => r.Sha256).Distinct().Should().HaveCount(Parallelism);
        // Each result should match its expected filename
        for (var i = 0; i < Parallelism; i++)
            results[i].FileName.Should().Be($"file{i}.txt");
    }

    // ── PII ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task PiiService_ParallelCalls_NoFalsePositivesOrMissing()
    {
        var tasks = Enumerable.Range(0, Parallelism).Select(i =>
        {
            var hasEmail = i % 2 == 0;
            var text = hasEmail ? $"Contact user{i}@example.com" : $"Plain text number {i}";
            return Task.Run(() => (Index: i, HasEmail: hasEmail, Result: PiiService.Detect(text, $"f{i}.txt")));
        }).ToArray();

        var results = await Task.WhenAll(tasks);

        foreach (var (index, hasEmail, result) in results)
        {
            if (hasEmail)
                result.TotalMatches.Should().BeGreaterThanOrEqualTo(1, $"file {index} should have email PII");
            else
                result.Matches.Should().NotContain(m => m.PiiType == "email", $"file {index} should have no email PII");
        }
    }

    // ── TextSearch ───────────────────────────────────────────────────────

    [Fact]
    public async Task TextSearch_ParallelCalls_CorrectMatchCounts()
    {
        var tasks = Enumerable.Range(0, Parallelism).Select(i =>
        {
            var text = string.Concat(Enumerable.Repeat($"word{i} ", i + 1));
            return Task.Run(() => (Index: i, Result: TextSearch.Search(text, query: $"word{i}")));
        }).ToArray();

        var results = await Task.WhenAll(tasks);

        foreach (var (index, result) in results)
            result.Count.Should().Be(index + 1, $"word{index} should appear {index + 1} times");
    }

    // ── Image ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ImageService_ParallelResize_CorrectDimensions()
    {
        var tasks = Enumerable.Range(1, Parallelism).Select(i =>
        {
            var targetWidth = 10 + i * 5;
            return Task.Run(() =>
            {
                var png = CreateTestPng(100, 100);
                var (_, bytes, _) = ImageService.Resize(png, $"img{i}.png", width: targetWidth, height: null);
                using var result = SixLabors.ImageSharp.Image.Load<Rgba32>(bytes);
                return (Expected: targetWidth, Actual: result.Width);
            });
        }).ToArray();

        var results = await Task.WhenAll(tasks);

        foreach (var (expected, actual) in results)
            actual.Should().Be(expected);
    }

    // ── PDF ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task PdfService_ParallelMetrics_CorrectPageCounts()
    {
        var tasks = Enumerable.Range(1, 10).Select(pageCount =>
        {
            return Task.Run(() =>
            {
                var pdf = CreateTestPdf(pageCount);
                var metrics = PdfService.GetMetrics(pdf, $"doc{pageCount}.pdf");
                return (Expected: pageCount, Actual: metrics.PageCount);
            });
        }).ToArray();

        var results = await Task.WhenAll(tasks);

        foreach (var (expected, actual) in results)
            actual.Should().Be(expected, $"PDF with {expected} pages should report {expected}");
    }

    [Fact]
    public async Task PdfService_ParallelMergeAndSplit_NoCorruption()
    {
        var tasks = Enumerable.Range(0, 5).Select(_ =>
        {
            return Task.Run(() =>
            {
                var pdf1 = CreateTestPdf(2);
                var pdf2 = CreateTestPdf(3);
                var (_, merged) = PdfService.MergePdfs([(pdf1, "a.pdf"), (pdf2, "b.pdf")]);
                var metrics = PdfService.GetMetrics(merged, "merged.pdf");
                return metrics.PageCount;
            });
        }).ToArray();

        var results = await Task.WhenAll(tasks);
        results.Should().AllSatisfy(r => r.Should().Be(5));
    }

    // ── Readability ──────────────────────────────────────────────────────

    [Fact]
    public async Task ReadabilityService_ParallelCalls_ConsistentScores()
    {
        var text = "The cat sat on the mat. The dog ran away quickly.";
        var tasks = Enumerable.Range(0, Parallelism).Select(_ =>
            Task.Run(() => ReadabilityService.Analyse(text, "test.txt"))).ToArray();

        var results = await Task.WhenAll(tasks);

        // All results should be identical (same input)
        var first = results[0];
        results.Should().AllSatisfy(r =>
        {
            r.FleschReadingEase.Should().Be(first.FleschReadingEase);
            r.GunningFogIndex.Should().Be(first.GunningFogIndex);
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static byte[] CreateTestPng(int w, int h)
    {
        using var img = new Image<Rgba32>(w, h, new Rgba32(255, 0, 0));
        using var ms = new MemoryStream();
        img.Save(ms, new PngEncoder());
        return ms.ToArray();
    }

    private static byte[] CreateTestPdf(int pages)
    {
        using var ms = new MemoryStream();
        using var writer = new PdfWriter(ms);
        using var doc = new PdfDocument(writer);
        using var layout = new Document(doc);
        for (var i = 0; i < pages; i++)
        {
            if (i > 0) layout.Add(new AreaBreak());
            layout.Add(new Paragraph($"Page {i + 1}"));
        }
        layout.Close();
        return ms.ToArray();
    }
}
