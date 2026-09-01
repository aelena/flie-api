using Aelena.FileApi.Core.Services.Pdf;
using Aelena.FileApi.Grpc.Proto;
using Google.Protobuf;
using Grpc.Core;

namespace Aelena.FileApi.Grpc.Services;

/// <summary>
/// The PDF half of the gRPC service.
/// </summary>
/// <remarks>
/// Kept in its own file so that <c>-p:IncludePdf=false</c> can drop it along with
/// the AGPL-licensed Aelena.FileApi.Core.Pdf package. These methods are then simply
/// not overridden, and the generated base class answers them with gRPC's standard
/// <c>Unimplemented</c> status — a caller gets a clear refusal rather than a
/// service that fails to start.
/// </remarks>
public sealed partial class FileGrpcService
{
    public override Task<PdfMetricsResponse> PdfMetrics(FileRequest request, ServerCallContext context)
    {
        var m = PdfService.GetMetrics(request.Data.ToByteArray(), request.FileName);
        var response = new PdfMetricsResponse
        {
            FileName = m.FileName,
            FileSizeBytes = m.FileSizeBytes,
            PageCount = m.PageCount,
            WordCount = m.WordCount,
            CharCount = m.CharCount,
            TokenCount = m.TokenCount,
            Language = m.Language ?? "",
            ImageCount = m.ImageCount,
            TableCount = m.TableCount,
            IsCorrupt = m.IsCorrupt,
            IsSigned = m.IsSigned,
            OcrNeeded = m.OcrNeeded,
            AvgCharsPerPage = m.AvgCharsPerPage
        };
        response.OcrPages.AddRange(m.OcrPages);
        return Task.FromResult(response);
    }

    public override Task<FileResponse> PdfRotate(PdfRotateRequest request, ServerCallContext context)
    {
        var (name, bytes) = PdfService.RotatePages(
            request.Data.ToByteArray(), request.FileName,
            request.Angle, request.HasPages ? request.Pages : null);

        return Task.FromResult(new FileResponse
        {
            FileName = name,
            Data = ByteString.CopyFrom(bytes),
            ContentType = "application/pdf"
        });
    }

    public override async Task<FileResponse> PdfMerge(
        IAsyncStreamReader<FileChunk> requestStream, ServerCallContext context)
    {
        var files = new List<(byte[] Data, string Name)>();
        var buffer = new MemoryStream();
        string currentName = "";

        await foreach (var chunk in requestStream.ReadAllAsync(context.CancellationToken))
        {
            if (!string.IsNullOrEmpty(chunk.FileName) && chunk.FileName != currentName)
            {
                // New file starting — flush previous if any
                if (buffer.Length > 0)
                {
                    files.Add((buffer.ToArray(), currentName));
                    buffer = new MemoryStream();
                }
                currentName = chunk.FileName;
            }

            buffer.Write(chunk.Data.Span);

            if (chunk.IsLast && buffer.Length > 0)
            {
                files.Add((buffer.ToArray(), currentName));
                buffer = new MemoryStream();
                currentName = "";
            }
        }

        // Flush any remaining
        if (buffer.Length > 0)
            files.Add((buffer.ToArray(), currentName));

        var (name, merged) = PdfService.MergePdfs(files);
        return new FileResponse
        {
            FileName = name,
            Data = ByteString.CopyFrom(merged),
            ContentType = "application/pdf"
        };
    }

    public override async Task PdfSplit(
        PdfSplitRequest request, IServerStreamWriter<FileChunk> responseStream,
        ServerCallContext context)
    {
        var (_, zipBytes) = PdfService.SplitPdf(
            request.Data.ToByteArray(), request.FileName, request.Ranges);

        // Stream the ZIP as chunks (or individual PDFs extracted from ZIP)
        using var zipStream = new MemoryStream(zipBytes);
        using var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Read);

        foreach (var entry in archive.Entries)
        {
            using var entryStream = entry.Open();
            using var ms = new MemoryStream();
            await entryStream.CopyToAsync(ms, context.CancellationToken);

            await responseStream.WriteAsync(new FileChunk
            {
                FileName = entry.FullName,
                Data = ByteString.CopyFrom(ms.ToArray()),
                IsLast = true
            });
        }
    }
}
