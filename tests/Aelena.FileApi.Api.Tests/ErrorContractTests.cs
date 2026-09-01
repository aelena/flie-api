using System.Net;
using System.Net.Http.Json;
using System.Text;
using Aelena.FileApi.Core.Models;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Aelena.FileApi.Api.Tests;

/// <summary>
/// Asserts the status code and body a caller actually receives when they get
/// something wrong.
/// </summary>
/// <remarks>
/// Every case here answered <c>500 "An unexpected error occurred."</c> before: Core
/// signalled these with <see cref="ArgumentException"/>, <see cref="FormatException"/>,
/// or a raw iText exception, and only <see cref="Aelena.FileApi.Core.Errors.FileApiException"/>
/// was mapped to a status code. A caller who mistyped a page range was told the server
/// had broken, with nothing naming the range.
/// </remarks>
public class ErrorContractTests(WebApplicationFactory<Program> factory) : FileApiFixture(factory)
{
    private static MultipartFormDataContent Upload(byte[] content, string fileName)
    {
        return new MultipartFormDataContent
        {
            { new ByteArrayContent(content), "file", fileName }
        };
    }

    private static MultipartFormDataContent TextUpload(string text, string fileName = "sample.txt") =>
        Upload(Encoding.UTF8.GetBytes(text), fileName);

    /// <summary>A one-page PDF, minimal but genuinely parseable.</summary>
    private static byte[] MinimalPdf()
    {
        using var ms = new MemoryStream();
        using (var writer = new iText.Kernel.Pdf.PdfWriter(ms))
        using (var doc = new iText.Kernel.Pdf.PdfDocument(writer))
        {
            doc.AddNewPage();
        }
        return ms.ToArray();
    }

    // ── Malformed input must not read as a server fault ──────────────────

    [Theory]
    [InlineData("abc")]
    [InlineData("99")]
    [InlineData("8-5")]
    [InlineData("0")]
    public async Task PdfExtractPages_BadRange_IsBadRequestNotServerError(string pages)
    {
        using var form = Upload(MinimalPdf(), "one-page.pdf");

        var response = await Client.PostAsync($"/pdf/extract-pages?pages={Uri.EscapeDataString(pages)}", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetail>();
        problem!.Status.Should().Be(400);
        problem.Detail.Should().NotBe("An unexpected error occurred.");
        problem.Detail.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task PdfMetadata_NotAPdf_IsUnprocessableWithAnExplanation()
    {
        using var form = Upload(Encoding.UTF8.GetBytes("this is plain text, not a PDF"), "renamed.pdf");

        var response = await Client.PostAsync("/pdf/metadata", form);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetail>();
        problem!.Title.Should().Be("Unreadable PDF");
        problem.Detail.Should().Contain("could not be read as a PDF");
    }

    [Fact]
    public async Task PdfMetrics_NotAPdf_StillAnswersWithCorruptFlag()
    {
        // Metrics is deliberately the exception: callers use it to ask whether a
        // document is usable at all, so it reports corruption instead of refusing.
        using var form = Upload(Encoding.UTF8.GetBytes("not a PDF"), "renamed.pdf");

        var response = await Client.PostAsync("/pdf/metrics", form);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"isCorrupt\":true");
    }

    [Fact]
    public async Task Search_BothQueryAndPattern_IsBadRequest()
    {
        using var form = TextUpload("hello world");

        var response = await Client.PostAsync("/search?query=a&pattern=b", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetail>();
        problem!.Detail.Should().Contain("not both");
    }

    [Fact]
    public async Task Search_MalformedPattern_IsBadRequestNotServerError()
    {
        using var form = TextUpload("hello world");

        var response = await Client.PostAsync("/search?pattern=%28unclosed", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetail>();
        problem!.Title.Should().Be("Invalid Pattern");
    }

    [Fact]
    public async Task Search_CatastrophicPattern_IsRefusedRatherThanHangingTheProcess()
    {
        // The pattern and the text are both caller-supplied, so an exponential
        // backtracker is a denial of service on a shared host.
        using var form = TextUpload(new string('a', 40) + "!");

        var response = await Client.PostAsync("/search?pattern=%5E%28a%2B%29%2B%24", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetail>();
        problem!.Title.Should().Be("Pattern Too Slow");
    }

    // ── Unimplemented capability must say so, not fake success ───────────

    [Fact]
    public async Task PdfRedact_ReportsNotImplementedInsteadOfReturningAnUnredactedFile()
    {
        // This used to answer 200 with "<name>_redacted.pdf" whose text was fully
        // intact and extractable — the document was copied through untouched.
        using var form = Upload(MinimalPdf(), "one-page.pdf");

        var response = await Client.PostAsync("/pdf/redact?terms=secret", form);

        response.StatusCode.Should().Be(HttpStatusCode.NotImplemented);
        response.Content.Headers.ContentType?.MediaType.Should().NotBe("application/pdf");
    }

    // ── Problem Details shape ────────────────────────────────────────────

    [Fact]
    public async Task Failures_UseProblemJsonAndCarryTheRequestPath()
    {
        using var form = Upload(Encoding.UTF8.GetBytes("nope"), "renamed.pdf");

        var response = await Client.PostAsync("/pdf/metadata", form);

        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetail>();
        problem!.Instance.Should().Be("/pdf/metadata");
        problem.Status.Should().Be((int)response.StatusCode);
    }
}
