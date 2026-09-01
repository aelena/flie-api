using Aelena.FileApi.Api.Middleware;

namespace Aelena.FileApi.Api.Endpoints;

/// <summary>Reading uploaded files into the <c>byte[]</c> that Core operates on.</summary>
public static class FormFileExtensions
{
    /// <summary>
    /// Read the whole upload into a single array, and record it for the audit log.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Eleven endpoint files each had their own copy of this, all shaped as
    /// <c>CopyToAsync(new MemoryStream())</c> followed by <c>ToArray()</c>. That is
    /// three copies of every upload live at once — the stream's internal buffer, the
    /// doubling it does as it grows, and the array — for a service whose whole job is
    /// handling large documents. Sizing the buffer from <see cref="IFormFile.Length"/>
    /// up front and handing back its own storage leaves one.
    /// </para>
    /// <para>
    /// It also threads the request's cancellation token, which none of the copies did:
    /// a caller who hung up mid-upload was still read to completion.
    /// </para>
    /// </remarks>
    public static async Task<byte[]> ReadAllBytesAsync(
        this IFormFile file, HttpContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(context);

        var data = await file.ReadAllBytesAsync(cancellationToken);

        // Handlers are the only place that knows which upload a request was about;
        // recording it here means every endpoint audits it without remembering to.
        context.Items[AuditLogMiddleware.FileNameItem] = file.FileName;
        context.Items[AuditLogMiddleware.FileSizeItem] = (long)data.Length;

        return data;
    }

    /// <summary>Read the whole upload into a single array.</summary>
    public static async Task<byte[]> ReadAllBytesAsync(
        this IFormFile file, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        // IFormFile.Length is the exact byte count, so the buffer never has to grow.
        // MemoryStream hands back this same array from GetBuffer when it owns it and
        // the length matches, so nothing is copied on the way out.
        if (file.Length == 0)
            return [];

        var buffer = new byte[file.Length];
        using var destination = new MemoryStream(buffer, writable: true);

        await using var source = file.OpenReadStream();
        await source.CopyToAsync(destination, cancellationToken);

        // A stream shorter than the declared length means a truncated upload.
        if (destination.Position != buffer.Length)
            throw new FileApiException(400,
                $"The upload ended after {destination.Position} of {buffer.Length} declared bytes. "
                + "The transfer was interrupted; send the file again.",
                title: "Incomplete Upload");

        return buffer;
    }
}
