using Aelena.FileApi.Core.Services.Common;
using AwesomeAssertions;
using Xunit;

namespace Aelena.FileApi.Tests.Services;

public class TextAnalysisTests
{
    [Fact]
    public void CountTokens_EmptyString_ReturnsZero() =>
        TextAnalysis.CountTokens("").Should().Be(0);

    [Fact]
    public void CountTokens_SimpleText_ReturnsPositive() =>
        TextAnalysis.CountTokens("Hello, world!").Should().BeGreaterThan(0);

    [Fact]
    public void CountTokens_LongerText_CountsCorrectly()
    {
        // "The quick brown fox jumps over the lazy dog" ≈ 9 tokens with cl100k_base
        var count = TextAnalysis.CountTokens("The quick brown fox jumps over the lazy dog");
        count.Should().BeInRange(8, 11);
    }

    [Fact]
    public void CountWords_EmptyString_ReturnsZero() =>
        TextAnalysis.CountWords("").Should().Be(0);

    [Fact]
    public void CountWords_MultipleWords_CountsCorrectly() =>
        TextAnalysis.CountWords("  hello   world   foo  ").Should().Be(3);

    [Fact]
    public void CountChars_ReturnsLength() =>
        TextAnalysis.CountChars("abc").Should().Be(3);

    [Theory]
    [InlineData("", null)]
    [InlineData("hi", null)]
    [InlineData("short text", null)]
    public void DetectLanguage_ShortText_ReturnsNull(string text, string? expected) =>
        TextAnalysis.DetectLanguage(text).Should().Be(expected);

    [Fact]
    public void DetectLanguage_EnglishText_ReturnsEn()
    {
        var text = "The quick brown fox jumps over the lazy dog and the cat is sitting on the mat.";
        TextAnalysis.DetectLanguage(text).Should().Be("en");
    }

    [Fact]
    public void DetectLanguage_SpanishText_ReturnsEs()
    {
        var text = "El rápido zorro marrón salta sobre el perro perezoso y el gato está sentado en la alfombra.";
        TextAnalysis.DetectLanguage(text).Should().Be("es");
    }

    // ── Marker words at the edges ────────────────────────────────────────
    //
    // Detection counts whole words now, where it previously scanned for the substring
    // " the " — a space on either side. That scan could not see a marker opening the
    // text, closing it, or sitting against punctuation or a newline, so its scores
    // were badly understated even when the winner happened to come out right. These
    // put the markers in exactly those positions and pin the counting behaviour, so
    // the rewrite cannot regress it.

    [Fact]
    public void DetectLanguage_MarkersAtStartAndEnd_AreCounted()
    {
        TextAnalysis.DetectLanguage("The document is complete and correct to the letter")
            .Should().Be("en");
    }

    [Fact]
    public void DetectLanguage_MarkersAgainstPunctuation_AreCounted()
    {
        TextAnalysis.DetectLanguage("Ready? The report is done, and the summary is of use.")
            .Should().Be("en");
    }

    [Fact]
    public void DetectLanguage_MarkersAcrossNewlines_AreCounted()
    {
        TextAnalysis.DetectLanguage(
            """
            The heading
            and the body
            is the point
            of the document
            """)
            .Should().Be("en");
    }

    [Fact]
    public void DetectLanguage_IsCaseInsensitive()
    {
        TextAnalysis.DetectLanguage("THE DOCUMENT IS COMPLETE AND CORRECT TO THE LETTER")
            .Should().Be("en");
    }

    [Fact]
    public void DetectLanguage_NoRecognisedMarkers_ReturnsNull() =>
        TextAnalysis.DetectLanguage("zzzz yyyy xxxx wwww vvvv uuuu tttt ssss rrrr")
            .Should().BeNull();

    [Fact]
    public void DetectLanguage_DistinguishesFrenchFromEnglish() =>
        TextAnalysis.DetectLanguage("Le document est une copie des originaux, et les pages est claire")
            .Should().Be("fr");

    [Fact]
    public void DetectLanguage_DistinguishesGermanFromEnglish() =>
        TextAnalysis.DetectLanguage("Der Bericht und die Zusammenfassung ist das Ergebnis der Arbeit")
            .Should().Be("de");
}
