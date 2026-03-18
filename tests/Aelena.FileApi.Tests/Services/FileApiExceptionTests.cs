using Aelena.FileApi.Core.Errors;
using FluentAssertions;
using Xunit;

namespace Aelena.FileApi.Tests.Services;

public class FileApiExceptionTests
{
    [Theory]
    [InlineData(400, "Bad Request")]
    [InlineData(401, "Unauthorized")]
    [InlineData(403, "Forbidden")]
    [InlineData(404, "Not Found")]
    [InlineData(413, "Payload Too Large")]
    [InlineData(415, "Unsupported Media Type")]
    [InlineData(422, "Unprocessable Content")]
    [InlineData(429, "Too Many Requests")]
    [InlineData(500, "Internal Server Error")]
    [InlineData(501, "Not Implemented")]
    [InlineData(999, "Error")]
    public void DefaultTitle_MapsStatusCodesToExpectedTitles(int statusCode, string expectedTitle)
    {
        var ex = new FileApiException(statusCode, "test detail");

        ex.Title.Should().Be(expectedTitle);
        ex.StatusCode.Should().Be(statusCode);
        ex.Detail.Should().Be("test detail");
        ex.ErrorType.Should().Be("about:blank");
    }

    [Fact]
    public void CustomTitle_OverridesDefault()
    {
        var ex = new FileApiException(400, "oops", title: "Custom Title", errorType: "urn:my:error");

        ex.Title.Should().Be("Custom Title");
        ex.ErrorType.Should().Be("urn:my:error");
    }

    [Fact]
    public void ExceptionMessage_EqualsDetail()
    {
        var ex = new FileApiException(500, "something broke");

        ex.Message.Should().Be("something broke");
    }
}
