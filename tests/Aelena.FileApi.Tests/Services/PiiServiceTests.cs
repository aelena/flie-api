using Aelena.FileApi.Core.Services.Common;
using FluentAssertions;
using Xunit;

namespace Aelena.FileApi.Tests.Services;

public class PiiServiceTests
{
    [Fact]
    public void Detect_Email_FindsMatch()
    {
        var result = PiiService.Detect("Contact john.doe@example.com for details.", "test.txt");

        result.TotalMatches.Should().BeGreaterThanOrEqualTo(1);
        result.Matches.Should().Contain(m => m.PiiType == "email");
        result.ByType.Should().ContainKey("email");
    }

    [Theory]
    [InlineData("4111-1111-1111-1111", "credit_card_visa")]
    [InlineData("5500 0000 0000 0004", "credit_card_mastercard")]
    public void Detect_CreditCard_FindsMatch(string cardNumber, string expectedType)
    {
        var result = PiiService.Detect($"Card number: {cardNumber}", "test.txt");

        result.Matches.Should().Contain(m => m.PiiType == expectedType);
    }

    [Fact]
    public void Detect_USssn_FindsMatch()
    {
        var result = PiiService.Detect("SSN: 123-45-6789", "test.txt");

        result.Matches.Should().Contain(m => m.PiiType == "ssn" && m.Country == "US");
    }

    [Fact]
    public void Detect_SpanishDNI_FindsMatch()
    {
        var result = PiiService.Detect("DNI: 12345678A", "test.txt");

        result.Matches.Should().Contain(m => m.PiiType == "dni" && m.Country == "ES");
    }

    [Fact]
    public void Detect_DateOfBirth_FindsMatch()
    {
        var result = PiiService.Detect("Born on 15/03/1990 in Madrid.", "test.txt");

        result.Matches.Should().Contain(m => m.PiiType == "date_of_birth");
    }

    [Fact]
    public void Detect_NoPii_ReturnsEmpty()
    {
        var result = PiiService.Detect("The quick brown fox jumps over the lazy dog.", "test.txt");

        result.TotalMatches.Should().Be(0);
        result.Matches.Should().BeEmpty();
    }

    [Fact]
    public void Detect_EmptyText_ReturnsEmpty()
    {
        var result = PiiService.Detect("", "empty.txt");
        result.TotalMatches.Should().Be(0);
    }

    [Fact]
    public void Detect_MultiplePiiTypes_CountsByType()
    {
        var text = "Email: test@example.com, SSN: 123-45-6789, DOB: 01/01/2000";
        var result = PiiService.Detect(text, "multi.txt");

        result.TotalMatches.Should().BeGreaterThanOrEqualTo(3);
        result.ByType.Keys.Should().Contain("email");
        result.ByType.Keys.Should().Contain("ssn");
    }

    [Fact]
    public void Detect_IncludesContext()
    {
        var result = PiiService.Detect("Please contact user@example.com for info.", "test.txt");

        result.Matches[0].Context.Should().Contain("contact");
        result.Matches[0].Start.Should().BeGreaterThan(0);
        result.Matches[0].End.Should().BeGreaterThan(result.Matches[0].Start);
    }
}
