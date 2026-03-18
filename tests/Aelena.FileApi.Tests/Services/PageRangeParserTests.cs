using Aelena.FileApi.Core.Services.Common;
using FluentAssertions;
using Xunit;

namespace Aelena.FileApi.Tests.Services;

public class PageRangeParserTests
{
    [Theory]
    [InlineData("1", 10, new[] { 0 })]
    [InlineData("1,3,5", 10, new[] { 0, 2, 4 })]
    [InlineData("1-3", 10, new[] { 0, 1, 2 })]
    [InlineData("1,3,5-8,12", 15, new[] { 0, 2, 4, 5, 6, 7, 11 })]
    [InlineData("10", 10, new[] { 9 })]
    [InlineData("1-1", 5, new[] { 0 })]
    public void Parse_ValidInput_ReturnsExpectedIndices(string pages, int maxPage, int[] expected) =>
        PageRangeParser.Parse(pages, maxPage).Should().Equal(expected);

    [Fact]
    public void Parse_DuplicatePages_AreDeduped()
    {
        var result = PageRangeParser.Parse("1,1,2-3,3", 10);
        result.Should().Equal(0, 1, 2);
    }

    [Fact]
    public void Parse_EmptyParts_AreSkipped()
    {
        var result = PageRangeParser.Parse("1,,3,", 10);
        result.Should().Equal(0, 2);
    }

    [Theory]
    [InlineData("0", 10)]
    [InlineData("11", 10)]
    [InlineData("1-11", 10)]
    [InlineData("0-5", 10)]
    public void Parse_OutOfBounds_ThrowsArgumentException(string pages, int maxPage) =>
        FluentActions.Invoking(() => PageRangeParser.Parse(pages, maxPage))
            .Should().Throw<ArgumentException>()
            .WithMessage("*out of bounds*");

    [Fact]
    public void Parse_NegativeNumber_ThrowsException() =>
        FluentActions.Invoking(() => PageRangeParser.Parse("-1", 10))
            .Should().Throw<Exception>(); // FormatException from empty string before hyphen

    [Fact]
    public void Parse_InvalidFormat_ThrowsFormatException() =>
        FluentActions.Invoking(() => PageRangeParser.Parse("abc", 10))
            .Should().Throw<FormatException>();
}
