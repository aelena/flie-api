using Aelena.FileApi.Core.Services.Common;
using FluentAssertions;
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
}
