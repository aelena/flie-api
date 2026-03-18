using System.IO.Compression;
using Aelena.FileApi.Core.Models;

namespace Aelena.FileApi.Core.Services.Common;

/// <summary>
/// ZIP archive inspection — lists entries with sizes, compression, CRC-32, and dates.
/// </summary>
public static class ZipService
{
    private static readonly Dictionary<CompressionLevel, string> CompressionNames = new()
    {
        [CompressionLevel.NoCompression] = "stored",
        [CompressionLevel.Fastest] = "deflated",
        [CompressionLevel.Optimal] = "deflated",
        [CompressionLevel.SmallestSize] = "deflated"
    };

    /// <summary>
    /// Inspect a ZIP archive and list all entries.
    /// </summary>
    public static ZipInspectResponse Inspect(ReadOnlyMemory<byte> data, string fileName)
    {
        using var stream = new MemoryStream(data.ToArray());
        ZipArchive archive;

        try
        {
            archive = new ZipArchive(stream, ZipArchiveMode.Read);
        }
        catch (InvalidDataException ex)
        {
            throw new Errors.FileApiException(400, $"Invalid ZIP file: {ex.Message}");
        }

        using (archive)
        {
            var entries = new List<Models.ZipEntry>();
            var totalDirs = 0;
            var totalFiles = 0;
            long totalUncompressed = 0;

            foreach (var entry in archive.Entries)
            {
                var isDir = entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\');

                if (isDir)
                    totalDirs++;
                else
                {
                    totalFiles++;
                    totalUncompressed += entry.Length;
                }

                var lastModified = entry.LastWriteTime != default
                    ? entry.LastWriteTime.UtcDateTime.ToString("o")
                    : null;

                entries.Add(new Models.ZipEntry(
                    Filename: entry.FullName,
                    IsDir: isDir,
                    FileSize: entry.Length,
                    CompressedSize: entry.CompressedLength,
                    CompressionMethod: entry.CompressedLength < entry.Length ? "deflated" : "stored",
                    Crc32: entry.Crc32.ToString("x8"),
                    LastModified: lastModified));
            }

            return new ZipInspectResponse(
                FileName: fileName,
                FileSizeBytes: data.Length,
                TotalEntries: entries.Count,
                TotalFiles: totalFiles,
                TotalDirs: totalDirs,
                TotalUncompressedSize: totalUncompressed,
                Entries: entries);
        }
    }
}
