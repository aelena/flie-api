using System.Text;
using Aelena.FileApi.Core.Services.Common;
using FluentAssertions;
using Xunit;

namespace Aelena.FileApi.Tests.Services;

public class TxtServiceTests
{
    [Fact]
    public void GetMetrics_BasicText_ReturnsCorrectCounts()
    {
        var text = "Hello world.\nThis is a test.\nThree lines here.";
        var data = Encoding.UTF8.GetBytes(text);

        var result = TxtService.GetMetrics(data, "test.txt");

        result.FileName.Should().Be("test.txt");
        result.WordCount.Should().Be(9);
        result.CharCount.Should().Be(text.Length);
        result.TokenCount.Should().BeGreaterThan(0);
        result.LineCount.Should().Be(3);
        result.FileSizeBytes.Should().Be(data.Length);
    }

    [Fact]
    public void GetMetrics_EmptyText_ReturnsZeros()
    {
        var result = TxtService.GetMetrics([],"empty.txt");

        result.WordCount.Should().Be(0);
        // Empty byte array decodes to empty string, Split('\n') returns [""] which is length 1
        // but the line count logic subtracts 1 if text doesn't end with newline on empty → 0
        result.FileSizeBytes.Should().Be(0);
    }

    [Fact]
    public void Search_LiteralQuery_FindsMatches()
    {
        var text = "The cat sat on the mat.";
        var data = Encoding.UTF8.GetBytes(text);

        var (fileName, matches) = TxtService.Search(data, "story.txt", query: "the");

        fileName.Should().Be("story.txt");
        matches.Should().HaveCount(2); // "The" + "the"
    }

    [Fact]
    public void Search_RegexPattern_FindsMatches()
    {
        var text = "Order 12345 and order 67890.";
        var data = Encoding.UTF8.GetBytes(text);

        var (_, matches) = TxtService.Search(data, "orders.txt", pattern: @"\d{5}");

        matches.Should().HaveCount(2);
        matches[0].Match.Should().Be("12345");
        matches[1].Match.Should().Be("67890");
    }
}
