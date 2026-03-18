using System.IO.Compression;
using System.Text;
using Aelena.FileApi.Core.Services.Common;
using FluentAssertions;
using Xunit;

namespace Aelena.FileApi.Tests.Services;

public class ZipServiceTests
{
    private static byte[] CreateTestZip(params (string Name, string Content)[] files)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in files)
            {
                var entry = archive.CreateEntry(name);
                using var writer = new StreamWriter(entry.Open());
                writer.Write(content);
            }
        }
        return ms.ToArray();
    }

    [Fact]
    public void Inspect_ValidZip_ReturnsEntries()
    {
        var data = CreateTestZip(
            ("hello.txt", "Hello, world!"),
            ("readme.md", "# Title"));

        var result = ZipService.Inspect(data, "test.zip");

        result.FileName.Should().Be("test.zip");
        result.TotalFiles.Should().Be(2);
        result.TotalDirs.Should().Be(0);
        result.TotalEntries.Should().Be(2);
        result.Entries.Should().HaveCount(2);
        result.Entries[0].Filename.Should().Be("hello.txt");
    }

    [Fact]
    public void Inspect_WithDirectory_CountsDirsAndFiles()
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            archive.CreateEntry("subdir/");
            var entry = archive.CreateEntry("subdir/file.txt");
            using var writer = new StreamWriter(entry.Open());
            writer.Write("content");
        }

        var result = ZipService.Inspect(ms.ToArray(), "nested.zip");

        result.TotalDirs.Should().Be(1);
        result.TotalFiles.Should().Be(1);
    }

    [Fact]
    public void Inspect_EmptyZip_ReturnsZeroEntries()
    {
        using var ms = new MemoryStream();
        using (new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true)) { }

        var result = ZipService.Inspect(ms.ToArray(), "empty.zip");
        result.TotalEntries.Should().Be(0);
    }

    [Fact]
    public void Inspect_InvalidData_ThrowsFileApiException()
    {
        var garbage = Encoding.UTF8.GetBytes("not a zip file");

        FluentActions.Invoking(() => ZipService.Inspect(garbage, "bad.zip"))
            .Should().Throw<Aelena.FileApi.Core.Errors.FileApiException>()
            .Where(e => e.StatusCode == 400);
    }
}
