using Aelena.FileApi.Core.Services.Common;
using FluentAssertions;
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

    [Fact]
    public void Search_BothQueryAndPattern_Throws() =>
        FluentActions.Invoking(() => TextSearch.Search("text", query: "a", pattern: "b"))
            .Should().Throw<ArgumentException>()
            .WithMessage("*not both*");

    [Fact]
    public void Search_NeitherQueryNorPattern_Throws() =>
        FluentActions.Invoking(() => TextSearch.Search("text"))
            .Should().Throw<ArgumentException>()
            .WithMessage("*either*");
}
