using Aelena.FileApi.Core.Services.Common;
using Aelena.FileApi.Core.Services.Docx;
using Aelena.FileApi.Core.Services.Image;
using Aelena.FileApi.Core.Services.Pdf;
using Aelena.FileApi.Grpc.Proto;
using Google.Protobuf;
using Grpc.Core;

namespace Aelena.FileApi.Grpc.Services;

/// <summary>
/// gRPC service implementation. All methods delegate to the same static Core services
/// used by the HTTP API and CLI (ports and adapters pattern).
/// </summary>
public sealed class FileServiceImpl : FileService.FileServiceBase
{
    // ── Compute-bound (high frequency) ───────────────────────────────────

    public override Task<HashResponse> Hash(FileRequest request, ServerCallContext context)
    {
        var result = HashService.ComputeHash(request.Data.ToByteArray(), request.FileName);
        return Task.FromResult(new HashResponse
        {
            FileName = result.FileName,
            FileSizeBytes = result.FileSizeBytes,
            Sha256 = result.Sha256,
            Md5 = result.Md5,
            Sha1 = result.Sha1,
            CompositeSha256 = result.CompositeSha256
        });
    }

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

    public override Task<DocxMetricsResponse> DocxMetrics(FileRequest request, ServerCallContext context)
    {
        var m = DocxService.GetMetrics(request.Data.ToByteArray(), request.FileName);
        return Task.FromResult(new DocxMetricsResponse
        {
            FileName = m.FileName,
            FileSizeBytes = m.FileSizeBytes,
            ParagraphCount = m.ParagraphCount,
            WordCount = m.WordCount,
            CharCount = m.CharCount,
            TokenCount = m.TokenCount,
            Language = m.Language ?? "",
            ImageCount = m.ImageCount,
            TableCount = m.TableCount,
            PageCount = m.PageCount ?? 0
        });
    }

    public override Task<TxtMetricsResponse> TxtMetrics(FileRequest request, ServerCallContext context)
    {
        var m = TxtService.GetMetrics(request.Data.ToByteArray(), request.FileName);
        return Task.FromResult(new TxtMetricsResponse
        {
            FileName = m.FileName,
            FileSizeBytes = m.FileSizeBytes,
            LineCount = m.LineCount,
            WordCount = m.WordCount,
            CharCount = m.CharCount,
            TokenCount = m.TokenCount,
            Language = m.Language ?? ""
        });
    }

    // ── Binary operations ────────────────────────────────────────────────

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

    public override Task<FileResponse> ImageResize(ImageResizeRequest request, ServerCallContext context)
    {
        var (name, bytes, mime) = ImageService.Resize(
            request.Data.ToByteArray(), request.FileName,
            request.HasWidth ? request.Width : null,
            request.HasHeight ? request.Height : null,
            request.MaintainAspect);

        return Task.FromResult(new FileResponse
        {
            FileName = name,
            Data = ByteString.CopyFrom(bytes),
            ContentType = mime
        });
    }

    public override Task<FileResponse> ImageConvert(ImageConvertRequest request, ServerCallContext context)
    {
        var (name, bytes, mime) = ImageService.Convert(
            request.Data.ToByteArray(), request.FileName, request.TargetFormat);

        return Task.FromResult(new FileResponse
        {
            FileName = name,
            Data = ByteString.CopyFrom(bytes),
            ContentType = mime
        });
    }

    public override Task<FileResponse> ImageCompress(ImageCompressRequest request, ServerCallContext context)
    {
        var (name, bytes, mime) = ImageService.Compress(
            request.Data.ToByteArray(), request.FileName, request.Quality);

        return Task.FromResult(new FileResponse
        {
            FileName = name,
            Data = ByteString.CopyFrom(bytes),
            ContentType = mime
        });
    }

    // ── Streaming: PDF Merge (client streaming) ──────────────────────────

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

    // ── Streaming: PDF Split (server streaming) ──────────────────────────

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
