# Aelena.FileApi.Grpc — High-Performance Document Processing over gRPC

A gRPC service exposing the compute-heavy, binary-intensive operations from `Aelena.FileApi.Core` for high-throughput scenarios. Independently buildable and deployable.

## Why gRPC?

| Benefit | Use Case |
|---------|----------|
| **Binary streaming** | PDF merge (client streaming), split (server streaming), image ops |
| **Protobuf serialization** | 2-5x smaller payloads than JSON for metrics responses |
| **HTTP/2 multiplexing** | High-frequency polling (metrics, hash) over single connection |
| **Strongly typed contracts** | `.proto` files as language-neutral API contract |

## Exposed Operations

### Compute-bound (small payload, high frequency)
- `Hash` — SHA-256/MD5/SHA-1/composite
- `PdfMetrics` — page count, words, OCR needs, signatures
- `DocxMetrics` — paragraphs, tables, images
- `TxtMetrics` — lines, words, tokens

### Binary (large payloads, streaming)
- `PdfMerge` — client-streaming: send N PDFs, receive merged PDF
- `PdfSplit` — server-streaming: send PDF + ranges, receive N parts
- `PdfRotate` — unary: send PDF + angle, receive rotated PDF
- `ImageResize` — unary: send image + dimensions, receive resized
- `ImageConvert` — unary: send image + target format, receive converted
- `ImageCompress` — unary: send image + quality, receive compressed JPEG

## Quick Start

```bash
cd grpc/
dotnet build
dotnet run --project src/Aelena.FileApi.Grpc
# Listening on https://localhost:5201
```

## Test with grpcurl

```bash
# Hash a file
grpcurl -plaintext -d '{"file_name":"test.txt","data":"aGVsbG8="}' \
  localhost:5201 fileapi.FileService/Hash

# PDF metrics
grpcurl -plaintext -d '{"file_name":"doc.pdf","data":"<base64>"}' \
  localhost:5201 fileapi.FileService/PdfMetrics
```

## Build & Pack

```bash
dotnet pack src/Aelena.FileApi.Grpc -c Release -o artifacts/
```
