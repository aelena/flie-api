using System.Security.Cryptography;
using System.Text;
using Aelena.FileApi.Core.Models;

namespace Aelena.FileApi.Core.Services.Common;

/// <summary>
/// Computes content-based and composite file hashes using SHA-256, MD5, and SHA-1.
/// </summary>
public static class HashService
{
    /// <summary>
    /// Compute content-based and composite hashes for file bytes.
    /// </summary>
    /// <param name="data">Raw file bytes.</param>
    /// <param name="fileName">Original filename (used in composite hash).</param>
    /// <param name="contentType">MIME content type, if known.</param>
    public static FileHashResponse ComputeHash(byte[] data, string fileName, string? contentType = null)
    {
        var sha256 = ToHex(SHA256.HashData(data));
        var md5 = ToHex(MD5.HashData(data));
        var sha1 = ToHex(SHA1.HashData(data));

        // Composite: fold in filename and size so identical content under
        // different names produces a distinct fingerprint.
        var prefix = Encoding.UTF8.GetBytes($"{fileName}:{data.Length}:");
        var compositeInput = new byte[prefix.Length + data.Length];
        prefix.CopyTo(compositeInput, 0);
        data.CopyTo(compositeInput, prefix.Length);
        var compositeSha256 = ToHex(SHA256.HashData(compositeInput));

        return new FileHashResponse(
            FileName: fileName,
            FileSizeBytes: data.Length,
            ContentType: contentType,
            Sha256: sha256,
            Md5: md5,
            Sha1: sha1,
            CompositeSha256: compositeSha256);
    }

    private static string ToHex(byte[] bytes) =>
        Convert.ToHexString(bytes).ToLowerInvariant();
}
