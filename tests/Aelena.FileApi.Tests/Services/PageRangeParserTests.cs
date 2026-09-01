using System.Globalization;
using Aelena.FileApi.Core.Errors;
using Aelena.FileApi.Core.Services.Common;
using AwesomeAssertions;
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
    [InlineData(" 1 , 3 ", 10, new[] { 0, 2 })]
    [InlineData("3,1", 10, new[] { 0, 2 })]
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

    // ── Rejections ───────────────────────────────────────────────────────
    //
    // Every one of these is the caller's mistake, so each must surface as a 400
    // rather than as a bare ArgumentException or FormatException. Those were
    // reported by the HTTP host as 500 "An unexpected error occurred", which told
    // the caller nothing about the page range they had actually mistyped.

    [Theory]
    [InlineData("0", 10)]
    [InlineData("11", 10)]
    [InlineData("1-11", 10)]
    [InlineData("0-5", 10)]
    public void Parse_OutOfBounds_ThrowsBadRequestNamingTheLimit(string pages, int maxPage)
    {
        var ex = FluentActions.Invoking(() => PageRangeParser.Parse(pages, maxPage))
            .Should().Throw<FileApiException>().Which;

        ex.StatusCode.Should().Be(400);
        ex.Detail.Should().Contain("out of bounds").And.Contain(maxPage.ToString(CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("1,abc")]
    [InlineData("-1")]
    [InlineData("1-")]
    [InlineData("-")]
    [InlineData("1.5")]
    [InlineData("1 3")]
    public void Parse_Malformed_ThrowsBadRequestQuotingTheToken(string pages)
    {
        var ex = FluentActions.Invoking(() => PageRangeParser.Parse(pages, 10))
            .Should().Throw<FileApiException>().Which;

        ex.StatusCode.Should().Be(400);
        ex.Detail.Should().Contain("not a valid page selection");
    }

    [Fact]
    public void Parse_ReversedRange_IsRejectedRatherThanSelectingNothing()
    {
        // "8-5" used to run a loop zero times and quietly return an empty selection,
        // so the caller got an empty document back and a 200.
        var ex = FluentActions.Invoking(() => PageRangeParser.Parse("8-5", 10))
            .Should().Throw<FileApiException>().Which;

        ex.StatusCode.Should().Be(400);
        ex.Detail.Should().Contain("backwards").And.Contain("5-8");
    }

    [Theory]
    [InlineData("")]
    [InlineData(",")]
    [InlineData("  ,  ")]
    public void Parse_SelectsNothing_IsRejected(string pages)
    {
        // Same failure mode as a reversed range: a selection that matches no page
        // must not be mistaken for "the whole document" or for an empty success.
        var ex = FluentActions.Invoking(() => PageRangeParser.Parse(pages, 10))
            .Should().Throw<FileApiException>().Which;

        ex.StatusCode.Should().Be(400);
        ex.Detail.Should().Contain("does not name any page");
    }

    [Fact]
    public void Parse_DocumentWithNoPages_IsUnprocessable()
    {
        var ex = FluentActions.Invoking(() => PageRangeParser.Parse("1", 0))
            .Should().Throw<FileApiException>().Which;

        ex.StatusCode.Should().Be(422);
    }

    [Fact]
    public void Parse_NullSpecification_ThrowsArgumentNullException() =>
        FluentActions.Invoking(() => PageRangeParser.Parse(null!, 10))
            .Should().Throw<ArgumentNullException>();

    [Fact]
    public void Parse_SinglePageDocument_UsesSingularInMessage()
    {
        var ex = FluentActions.Invoking(() => PageRangeParser.Parse("2", 1))
            .Should().Throw<FileApiException>().Which;

        ex.Detail.Should().Contain("1 page.").And.NotContain("pages");
    }
}
