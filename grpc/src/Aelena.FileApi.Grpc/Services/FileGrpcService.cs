using Aelena.FileApi.Core.Services.Common;
using Aelena.FileApi.Core.Services.Docx;
using Aelena.FileApi.Core.Services.Image;
using Aelena.FileApi.Grpc.Proto;
using Google.Protobuf;
using Grpc.Core;

namespace Aelena.FileApi.Grpc.Services;

/// <summary>
/// gRPC service implementation. All methods delegate to the same static Core services
/// used by the HTTP API and CLI (ports and adapters pattern).
/// </summary>
public sealed partial class FileGrpcService : FileService.FileServiceBase
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

    // ── Streaming: PDF Split (server streaming) ──────────────────────────

}
