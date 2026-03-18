using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Aelena.FileApi.Api.Tests;

[Collection("FileApi")]
public class Phase2EndpointTests(WebApplicationFactory<Program> factory) : FileApiFixture(factory)
{
    private static MultipartFormDataContent CreateFileContent(string content, string fileName, string fieldName = "file")
    {
        var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(fileContent, fieldName, fileName);
        return form;
    }

    // ── Hash ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Hash_ReturnsAllHashes()
    {
        using var form = CreateFileContent("hello world", "test.txt");
        var response = await Client.PostAsync("/hash", form);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        json.RootElement.GetProperty("sha256").GetString().Should().NotBeNullOrEmpty();
        json.RootElement.GetProperty("md5").GetString().Should().NotBeNullOrEmpty();
        json.RootElement.GetProperty("sha1").GetString().Should().NotBeNullOrEmpty();
        json.RootElement.GetProperty("compositeSha256").GetString().Should().NotBeNullOrEmpty();
    }

    // ── TXT ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task TxtMetrics_ReturnsWordAndLineCounts()
    {
        using var form = CreateFileContent("Hello world.\nLine two.", "sample.txt");
        var response = await Client.PostAsync("/txt/metrics", form);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        json.RootElement.GetProperty("wordCount").GetInt32().Should().BeGreaterThan(0);
        json.RootElement.GetProperty("lineCount").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task TxtSearch_FindsMatches()
    {
        using var form = CreateFileContent("The quick brown fox jumps over the lazy dog.", "story.txt");
        var response = await Client.PostAsync("/txt/search?query=the", form);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        json.RootElement.GetProperty("totalMatches").GetInt32().Should().BeGreaterThan(0);
    }

    // ── ZIP ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ZipInspect_ListsEntries()
    {
        // Create a real ZIP in memory
        using var ms = new MemoryStream();
        using (var archive = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Create, true))
        {
            var entry = archive.CreateEntry("test.txt");
            using var writer = new StreamWriter(entry.Open());
            writer.Write("hello");
        }

        var form = new MultipartFormDataContent();
        var zipContent = new ByteArrayContent(ms.ToArray());
        zipContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        form.Add(zipContent, "file", "test.zip");

        var response = await Client.PostAsync("/zip/inspect", form);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        json.RootElement.GetProperty("totalFiles").GetInt32().Should().Be(1);
    }

    // ── Readability ──────────────────────────────────────────────────────

    [Fact]
    public async Task Readability_ReturnsScores()
    {
        var text = "The cat sat on the mat. The dog ran away quickly. It was indeed a beautiful day outside.";
        using var form = CreateFileContent(text, "story.txt");
        var response = await Client.PostAsync("/readability", form);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        json.RootElement.GetProperty("fleschReadingEase").GetDouble().Should().BeGreaterThan(0);
        json.RootElement.GetProperty("interpretation").GetString().Should().NotBeNullOrEmpty();
    }

    // ── Search ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Search_FindsMatchesInText()
    {
        using var form = CreateFileContent("The quick brown fox jumps over the lazy dog.", "sample.txt");
        var response = await Client.PostAsync("/search?query=fox", form);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        json.RootElement.GetProperty("totalMatches").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Search_NoQueryOrPattern_Returns400()
    {
        using var form = CreateFileContent("some text", "file.txt");
        var response = await Client.PostAsync("/search", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Strip (stub) ─────────────────────────────────────────────────────

    [Fact]
    public async Task StripImages_ReturnsNotImplemented()
    {
        using var form = CreateFileContent("dummy", "test.pdf");
        var response = await Client.PostAsync("/strip/images", form);

        response.StatusCode.Should().Be(HttpStatusCode.NotImplemented);
    }

    // ── Redact (stub) ────────────────────────────────────────────────────

    [Fact]
    public async Task Redact_ReturnsNotImplemented()
    {
        using var form = CreateFileContent("dummy", "test.pdf");
        var response = await Client.PostAsync("/redact?query=test", form);

        response.StatusCode.Should().Be(HttpStatusCode.NotImplemented);
    }
}
