using Aelena.FileApi.Core.Services.Common;
using FluentAssertions;
using Xunit;

namespace Aelena.FileApi.Tests.Services;

public class ReadabilityServiceTests
{
    [Fact]
    public void Analyse_SimpleText_ReturnsScores()
    {
        var text = "The cat sat on the mat. The dog ran away. It was a nice day.";
        var result = ReadabilityService.Analyse(text, "story.txt");

        result.WordCount.Should().BeGreaterThan(0);
        result.SentenceCount.Should().Be(3);
        result.SyllableCount.Should().BeGreaterThan(0);
        result.FleschReadingEase.Should().BeGreaterThan(0);
        result.FleschKincaidGrade.Should().NotBe(0);
        result.GunningFogIndex.Should().BeGreaterThan(0);
        result.SmogIndex.Should().BeGreaterThan(0);
        result.Interpretation.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Analyse_EmptyText_ReturnsZeros()
    {
        var result = ReadabilityService.Analyse("", "empty.txt");

        result.WordCount.Should().Be(0);
        result.Interpretation.Should().Contain("no analysable text");
    }

    [Fact]
    public void Analyse_SpanishLanguage_ReturnsSpanishInterpretation()
    {
        var text = "The cat sat on the mat. The dog ran away.";
        var result = ReadabilityService.Analyse(text, "story.txt", "es");

        result.Interpretation.Should().NotBeNullOrEmpty();
        // Spanish interpretation should contain Spanish words
        result.Interpretation.Should().ContainAny("fácil", "difícil", "estándar", "grado", "nivel", "posgrado");
    }

    [Theory]
    [InlineData("cat", 1)]
    [InlineData("beautiful", 3)]
    [InlineData("the", 1)]
    [InlineData("education", 4)]
    [InlineData("a", 1)]
    public void CountSyllables_KnownWords(string word, int expected) =>
        ReadabilityService.CountSyllables(word).Should().Be(expected);

    [Fact]
    public void Analyse_ComplexText_HigherDifficulty()
    {
        var simple = "The cat sat. The dog ran. A bird flew.";
        var complex = "The philosophical implications of epistemological uncertainty in contemporary metaphysical discourse remain fundamentally unresolvable.";

        var simpleResult = ReadabilityService.Analyse(simple, "simple.txt");
        var complexResult = ReadabilityService.Analyse(complex, "complex.txt");

        // Simple text should have higher (easier) Flesch score
        simpleResult.FleschReadingEase.Should().BeGreaterThan(complexResult.FleschReadingEase);
    }
}
