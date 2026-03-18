using System.Text;
using Aelena.FileApi.Core.Services.Common;
using FluentAssertions;
using Xunit;

namespace Aelena.FileApi.Tests.Services;

public class HashServiceTests
{
    [Fact]
    public void ComputeHash_ReturnsAllFourHashes()
    {
        var data = Encoding.UTF8.GetBytes("hello world");
        var result = HashService.ComputeHash(data, "test.txt", "text/plain");

        result.Sha256.Should().NotBeNullOrEmpty();
        result.Md5.Should().NotBeNullOrEmpty();
        result.Sha1.Should().NotBeNullOrEmpty();
        result.CompositeSha256.Should().NotBeNullOrEmpty();
        result.FileName.Should().Be("test.txt");
        result.FileSizeBytes.Should().Be(11);
        result.ContentType.Should().Be("text/plain");
    }

    [Fact]
    public void ComputeHash_SameContent_SameSha256()
    {
        var data = Encoding.UTF8.GetBytes("identical content");
        var r1 = HashService.ComputeHash(data, "file1.txt");
        var r2 = HashService.ComputeHash(data, "file2.txt");

        r1.Sha256.Should().Be(r2.Sha256);
        r1.Md5.Should().Be(r2.Md5);
        r1.Sha1.Should().Be(r2.Sha1);
    }

    [Fact]
    public void ComputeHash_SameContent_DifferentNames_DifferentComposite()
    {
        var data = Encoding.UTF8.GetBytes("identical content");
        var r1 = HashService.ComputeHash(data, "file1.txt");
        var r2 = HashService.ComputeHash(data, "file2.txt");

        r1.CompositeSha256.Should().NotBe(r2.CompositeSha256);
    }

    [Fact]
    public void ComputeHash_DifferentContent_DifferentHashes()
    {
        var r1 = HashService.ComputeHash(Encoding.UTF8.GetBytes("aaa"), "f.txt");
        var r2 = HashService.ComputeHash(Encoding.UTF8.GetBytes("bbb"), "f.txt");

        r1.Sha256.Should().NotBe(r2.Sha256);
    }

    [Fact]
    public void ComputeHash_EmptyFile_Works()
    {
        var result = HashService.ComputeHash([], "empty.bin");
        result.FileSizeBytes.Should().Be(0);
        result.Sha256.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("hello world", "b94d27b9934d3e08a52e52d7da7dabfac484efe37a5380ee9088f7ace2efcde9")]
    public void ComputeHash_KnownSha256(string input, string expectedSha256)
    {
        var result = HashService.ComputeHash(Encoding.UTF8.GetBytes(input), "test.txt");
        result.Sha256.Should().Be(expectedSha256);
    }
}
