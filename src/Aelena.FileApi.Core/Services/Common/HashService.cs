using System.Diagnostics.CodeAnalysis;
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
    [SuppressMessage("Security", "CA5350:Do Not Use Weak Cryptographic Algorithms",
        Justification = "MD5/SHA-1 are emitted as content fingerprints for interop with external "
                      + "catalogues that index by them. They are never used to authenticate or "
                      + "sign; SHA-256 is the primary digest.")]
    [SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms",
        Justification = "MD5/SHA-1 are emitted as content fingerprints for interop with external "
                      + "catalogues that index by them. They are never used to authenticate or "
                      + "sign; SHA-256 is the primary digest.")]
    public static FileHashResponse ComputeHash(byte[] data, string fileName, string? contentType = null)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(fileName);

        return new FileHashResponse(
            FileName: fileName,
            FileSizeBytes: data.Length,
            ContentType: contentType,
            Sha256: ToHex(SHA256.HashData(data)),
            Md5: ToHex(MD5.HashData(data)),
            Sha1: ToHex(SHA1.HashData(data)),
            CompositeSha256: CompositeSha256(data, fileName));
    }

    /// <summary>
    /// Fold the filename and length in ahead of the content so that identical bytes
    /// stored under different names produce distinct fingerprints.
    /// </summary>
    /// <remarks>
    /// Hashed incrementally rather than into a concatenated buffer: the previous
    /// implementation allocated a second copy of the whole file on every request,
    /// which doubled peak memory for large uploads.
    /// </remarks>
    private static string CompositeSha256(ReadOnlySpan<byte> data, string fileName)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(Encoding.UTF8.GetBytes($"{fileName}:{data.Length}:"));
        hash.AppendData(data);

        Span<byte> digest = stackalloc byte[SHA256.HashSizeInBytes];
        hash.GetHashAndReset(digest);
        return ToHex(digest);
    }

    private static string ToHex(ReadOnlySpan<byte> bytes) =>
        Convert.ToHexStringLower(bytes);
}
