using Aelena.FileApi.Core.Errors;
using Aelena.FileApi.Core.Services.Common;
using AwesomeAssertions;
using Xunit;

namespace Aelena.FileApi.Tests.Services;

public class TextSearchTests
{
    private const string SampleText = "The quick brown fox jumps over the lazy dog. The dog barked.";

    [Fact]
    public void Search_LiteralQuery_FindsMatches()
    {
        var matches = TextSearch.Search(SampleText, query: "the");
        matches.Should().HaveCount(3); // "The" x2 + "the" x1 (case-insensitive)
    }

    [Fact]
    public void Search_LiteralQuery_CaseInsensitive()
    {
        var matches = TextSearch.Search(SampleText, query: "DOG");
        matches.Should().HaveCount(2);
        matches[0].Match.Should().Be("dog");
    }

    [Fact]
    public void Search_RegexPattern_FindsMatches()
    {
        var matches = TextSearch.Search(SampleText, pattern: @"\b\w{5}\b");
        // "quick", "brown", "jumps", "barked" — 5-letter words
        matches.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    [Fact]
    public void Search_IncludesContext()
    {
        var matches = TextSearch.Search(SampleText, query: "fox", contextChars: 10);
        matches.Should().HaveCount(1);
        matches[0].Context.Should().Contain("fox");
        matches[0].Context.Length.Should().BeLessThan(SampleText.Length);
    }

    [Fact]
    public void Search_ReturnsCorrectPositions()
    {
        var matches = TextSearch.Search(SampleText, query: "fox");
        matches[0].Start.Should().Be(SampleText.IndexOf("fox", StringComparison.OrdinalIgnoreCase));
        matches[0].End.Should().Be(matches[0].Start + 3);
    }

    [Fact]
    public void Search_NoMatch_ReturnsEmpty()
    {
        var matches = TextSearch.Search(SampleText, query: "elephant");
        matches.Should().BeEmpty();
    }

    // ── Rejections ───────────────────────────────────────────────────────
    //
    // All of these are the caller's mistake and must read as 400s. They were
    // ArgumentException before, which the HTTP host could only render as a 500.

    [Fact]
    public void Search_BothQueryAndPattern_IsBadRequest()
    {
        var ex = FluentActions.Invoking(() => TextSearch.Search("text", query: "a", pattern: "b"))
            .Should().Throw<FileApiException>().Which;

        ex.StatusCode.Should().Be(400);
        ex.Detail.Should().Contain("not both");
    }

    [Fact]
    public void Search_NeitherQueryNorPattern_IsBadRequest()
    {
        var ex = FluentActions.Invoking(() => TextSearch.Search("text"))
            .Should().Throw<FileApiException>().Which;

        ex.StatusCode.Should().Be(400);
        ex.Detail.Should().Contain("either");
    }

    [Fact]
    public void Search_MalformedPattern_IsBadRequestQuotingThePattern()
    {
        var ex = FluentActions.Invoking(() => TextSearch.Search("text", pattern: "(unclosed"))
            .Should().Throw<FileApiException>().Which;

        ex.StatusCode.Should().Be(400);
        ex.Title.Should().Be("Invalid Pattern");
        ex.Detail.Should().Contain("(unclosed");
    }

    [Fact]
    public void Search_NegativeContext_IsBadRequest() =>
        FluentActions.Invoking(() => TextSearch.Search("text", query: "t", contextChars: -1))
            .Should().Throw<FileApiException>()
            .Which.StatusCode.Should().Be(400);

    [Fact]
    public void Search_NullText_ThrowsArgumentNullException() =>
        FluentActions.Invoking(() => TextSearch.Search(null!, query: "a"))
            .Should().Throw<ArgumentNullException>();

    [Fact]
    public void Search_ContextIsClampedToTheTextBounds()
    {
        // A context window wider than the document must not index out of range.
        var matches = TextSearch.Search("abc", query: "b", contextChars: 1000);

        matches.Should().ContainSingle();
        matches[0].Context.Should().Be("abc");
    }

    [Fact]
    public void Search_CatastrophicPattern_TimesOutAsBadRequest()
    {
        // Both the pattern and the text come from the caller, so a nested quantifier
        // against a long non-matching run is a denial of service on a shared process.
        // Without a match timeout this call never returns.
        var evil = new string('a', 40) + "!";

        var ex = FluentActions.Invoking(() => TextSearch.Search(evil, pattern: "^(a+)+$"))
            .Should().Throw<FileApiException>().Which;

        ex.StatusCode.Should().Be(400);
        ex.Title.Should().Be("Pattern Too Slow");
    }
}
