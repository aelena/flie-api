using System.Globalization;
using System.IO.Compression;
using Aelena.FileApi.Core.Errors;
using Aelena.FileApi.Core.Models;

namespace Aelena.FileApi.Core.Services.Common;

/// <summary>
/// ZIP archive inspection — lists entries with sizes, compression, CRC-32, and dates.
/// </summary>
public static class ZipService
{
    /// <summary>
    /// Inspect a ZIP archive and list all entries.
    /// </summary>
    /// <param name="data">Raw archive bytes.</param>
    /// <param name="fileName">Original file name for the response.</param>
    /// <exception cref="FileApiException">Status 400 when the archive cannot be read.</exception>
    /// <remarks>
    /// Entries are described from the central directory only; nothing is decompressed,
    /// so a zip bomb costs no more than a well-behaved archive of the same size.
    /// </remarks>
    public static ZipInspectResponse Inspect(byte[] data, string fileName)
    {
        ArgumentNullException.ThrowIfNull(data);

        // MemoryStream over the caller's array rather than data.ToArray(): the previous
        // ReadOnlyMemory<byte> parameter forced a full copy of the archive on every call.
        using var stream = new MemoryStream(data, writable: false);

        try
        {
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

            var entries = new List<Models.ZipEntry>(archive.Entries.Count);
            var totalDirs = 0;
            var totalFiles = 0;
            long totalUncompressed = 0;

            foreach (var entry in archive.Entries)
            {
                var isDir = IsDirectory(entry);

                if (isDir)
                {
                    totalDirs++;
                }
                else
                {
                    totalFiles++;
                    totalUncompressed += entry.Length;
                }

                entries.Add(new Models.ZipEntry(
                    Filename: entry.FullName,
                    IsDir: isDir,
                    FileSize: entry.Length,
                    CompressedSize: entry.CompressedLength,
                    CompressionMethod: entry.CompressedLength < entry.Length ? "deflated" : "stored",
                    Crc32: entry.Crc32.ToString("x8", CultureInfo.InvariantCulture),
                    LastModified: LastModified(entry)));
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
        catch (Exception ex) when (ex is InvalidDataException or EndOfStreamException)
        {
            // The try used to cover only the ZipArchive constructor. An archive with a
            // readable header but a damaged or truncated central directory fails while
            // the entries are being walked, and that escaped as a raw InvalidDataException
            // which the HTTP host could only report as a 500.
            throw new FileApiException(400, $"The file is not a readable ZIP archive: {ex.Message}");
        }
    }

    /// <summary>
    /// A zip directory entry is a zero-length name ending in a separator.
    /// </summary>
    private static bool IsDirectory(ZipArchiveEntry entry) =>
        entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\');

    /// <summary>
    /// Read the entry's timestamp, tolerating the invalid DOS dates some writers emit.
    /// </summary>
    /// <remarks>
    /// <see cref="ZipArchiveEntry.LastWriteTime"/> throws
    /// <see cref="ArgumentOutOfRangeException"/> for a DOS timestamp outside 1980-2107,
    /// which several archivers produce for entries with no recorded date. One such
    /// entry used to fail the whole inspection.
    /// </remarks>
    private static string? LastModified(ZipArchiveEntry entry)
    {
        try
        {
            return entry.LastWriteTime != default
                ? entry.LastWriteTime.UtcDateTime.ToString("o", CultureInfo.InvariantCulture)
                : null;
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }
}
