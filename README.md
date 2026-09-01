# FileApi — Document Processing & AI Analysis Platform

[![Build](https://img.shields.io/badge/build-passing-brightgreen)]()
[![Tests](https://img.shields.io/badge/tests-580%20passing-brightgreen)]()
[![.NET](https://img.shields.io/badge/.NET-10.0%20%7C%2011.0-blue)]()
[![Core](https://img.shields.io/badge/Core-MIT-green)]()
[![Core.Pdf](https://img.shields.io/badge/Core.Pdf-AGPL--3.0-red)]()

A comprehensive .NET 10 / C# 14 document processing platform. Four ports available:

- **HTTP API**
- **Rich CLI**
- **gRPC service**
- **NuGet library**

All powered by the same pure Core library with zero ASP.NET dependencies.

Builds and tests green on **.NET 10 (LTS)** and **.NET 11 preview**.

---

## ⚠️ Licensing — read this before taking a dependency

**This repository ships two libraries under two different licences, and the difference matters.**

| Package | Licence | Contains | Safe for closed-source use? |
|---------|---------|----------|------------------------------|
| `Aelena.FileApi.Core` | **MIT** | DOCX, images, email, hashing, PII, readability, text, ZIP, share links, jobs | **Yes** |
| `Aelena.FileApi.Core.Pdf` | **AGPL-3.0-or-later** | All PDF operations | **No** — see below |
| `Aelena.FileApi.Cli` (`fileapi` tool) | **AGPL-3.0-or-later** | Everything, including PDF | **No** — see below |

`Aelena.FileApi.Core.Pdf` is built on [iText 7](https://itextpdf.com/), which is
licensed under the **AGPL**. The AGPL is a strong copyleft licence: if you use it in
a network-facing application, that obligation extends to **your** application's
source. iText sells a commercial licence if that is not acceptable — that is a
matter between you and iText, and installing this package does not grant it.

PDF lives in its own package precisely so that everything else can stay MIT.
**If you do not need PDF, depend on `Aelena.FileApi.Core` alone and no copyleft
code enters your build.** The split is enforced by the project structure: `Core`
has no reference to iText, direct or transitive.

```bash
# MIT, no copyleft anywhere in the graph
dotnet add package Aelena.FileApi.Core

# AGPL — only if you understand and accept the obligation
dotnet add package Aelena.FileApi.Core.Pdf
```

The self-hosted **HTTP API and gRPC service** in this repository include PDF by
default, so a deployment of either is likewise subject to the AGPL.

### If you clone this repository

The NuGet split does not help you here — a clone contains everything, including
the AGPL part. So there is a supported way to build without it:

```bash
dotnet build   -p:IncludePdf=false
dotnet publish src/Aelena.FileApi.Api -f net10.0 -c Release -p:IncludePdf=false
```

`-p:IncludePdf=false` removes the `Aelena.FileApi.Core.Pdf` project reference, the
`/pdf/*` endpoints, the `fileapi pdf` command group, and the PDF gRPC methods. The
result contains **no iText assembly at all** — not a disabled feature flag, an
absent dependency. What remains is MIT throughout.

| | Default build | `-p:IncludePdf=false` |
|---|---|---|
| Effective licence | AGPL-3.0-or-later | MIT |
| `/pdf/*` endpoints | 30+ routes | absent (404) |
| `fileapi pdf …` | available | absent from `--help` |
| gRPC PDF methods | available | `Unimplemented` status |
| Everything else | available | available |
| iText in output | yes | **no** |

CI publishes both ways on every push and fails if an iText assembly appears in the
opt-out output, so this stays true rather than drifting.

Full detail, including the terms of every dependency, is in
**[LICENSING.md](LICENSING.md)**.

## Architecture

```
         ┌──────────────────┐   ┌────────────────────────┐
         │  Core (MIT)      │◄──│  Core.Pdf (AGPL)       │
         │  DOCX, images,   │   │  PDF only — iText 7    │
         │  email, hash,    │   │  Separated so that     │
         │  PII, text, zip  │   │  Core stays MIT        │
         └────────┬─────────┘   └───────────┬────────────┘
                  │                         │
                  └───────────┬─────────────┘
                             │
              ┌──────────────┼──────────────┐
              │              │              │
        ┌─────▼──────┐  ┌────▼────┐  ┌──────▼────┐
        │  HTTP API  │  │   CLI   │  │   gRPC    │
        │ (MinAPIs)  │  │ (rich)  │  │ (grpc/)   │
        └────────────┘  └─────────┘  └───────────┘
```

### Design Principles

- **Pure library** — Core has zero `Microsoft.Extensions.*` dependencies; usable in console apps, desktop apps, cloud functions, anywhere
- **Ports & adapters** — API and CLI are thin wrappers calling static Core services
- **Thread-safe** — All Core operations are static and proven safe under concurrent load
- **Terse & functional** — records, pattern matching, expression-bodied lambdas
- **Observability** — OpenTelemetry traces + metrics + logs (API layer), Serilog structured logging
- **Cloud-ready** — Docker multi-stage build, deployable to Azure Container Apps, App Service, AKS

## Features

| Category | Description | Status |
|----------|-------------|--------|
| **PDF Toolkit** | 30+ operations: metrics, metadata, extract text/pages/markdown/annotations/bookmarks, merge, split, rotate, reorder, delete pages, watermark, encrypt/decrypt, compress, page numbers, form fields, health check | Implemented |
| **DOCX Processing** | Metrics, metadata, paragraph extraction, markdown conversion, search, health check, metadata removal | Implemented |
| **Image Processing** | Resize, rotate, crop, convert (PNG/JPEG/WebP/BMP/GIF/TIFF), thumbnail, flip, blur, grayscale, compress, strip metadata, EXIF, auto-orient, invert, edge detect, equalize, color palette, base64 | Implemented |
| **PII Detection** | Regex-based scanning for emails, credit cards (Visa/MC/Amex), IBANs, SSNs, phone numbers, national IDs (US/ES/FR/DE/IT/UK/PT), dates of birth | Implemented |
| **Text Analysis** | Metrics, search (literal + regex), readability scores (Flesch, Gunning Fog, SMOG) | Implemented |
| **Email Parsing** | .eml (RFC 5322 / MIME) parsing with MimeKit — headers, body, attachments | Implemented |
| **File Hashing** | SHA-256, MD5, SHA-1, composite hash | Implemented |
| **ZIP Inspection** | List entries with sizes, compression, CRC-32 | Implemented |
| **Share Links** | CRUD with SQLite persistence, password protection, expiry, recipient restrictions — all enforced on access | Implemented |
| **Async Jobs** | Compare, summarize, batch — async job pattern with in-memory store and polling | Job pattern ready |
| **Document Comparison** | Lexical, semantic, summary modes with cross-format support | Job pattern ready; LLM pipeline pending |
| **AI Analysis** | Summarization, classification, Q&A via LLM | Endpoints ready; LLM pipeline pending |
| **Image AI (LLM)** | Describe, tag, detect objects, moderate, extract data, visual Q&A | Endpoints ready; LLM pipeline pending |
| **Geospatial** | KML, KMZ, GeoJSON, Shapefile, DXF feature extraction | Endpoint stubs; NetTopologySuite integration pending |
| **Video** | Container/track metadata extraction | Stub; MediaInfo integration pending |

## Endpoint Families (~100 routes)

| Family | Prefix | Routes | Description |
|--------|--------|--------|-------------|
| Health | `/health` | 1 | Liveness check |
| Auth | `/api/auth/*` | 1 | JWT cookie management |
| PDF | `/pdf/*` | 30+ | Full PDF manipulation toolkit |
| DOCX | `/docx/*` | 10 | Word document processing |
| TXT | `/txt/*` | 2 | Plain text metrics and search |
| Image | `/image/*` | 13 | Image manipulation (ImageSharp) |
| Image AI | `/image-ai/*` | 14 | Local + LLM-powered image analysis |
| Hash | `/hash` | 1 | Multi-algorithm file hashing |
| PII | `/pii/detect` | 1 | PII detection (20+ regex patterns) |
| Search | `/search` | 1 | Universal cross-format search |
| Readability | `/readability` | 1 | Flesch, Gunning Fog, SMOG scores |
| ZIP | `/zip/inspect` | 1 | Archive inspection |
| Email | `/email/parse` | 1 | Parse .eml/.msg files |
| Compare | `/compare` | 2 | Async document comparison |
| Summarize | `/summarize` | 2 | Async document summarization |
| Batch | `/batch/*` | 2 | Parallel multi-file processing |
| Classify | `/classify` | 1 | Document type classification |
| Q&A | `/qa` | 1 | Document-grounded Q&A |
| Share | `/share/*` | 4 | Shareable report links |
| Geospatial | `/geospatial/*` | 4 | Feature extraction from geo formats |
| Video | `/video/metadata` | 1 | Container/track metadata |
| Markdown | `/markdown/to-pdf` | 1 | Markdown to PDF conversion |
| Strip | `/strip/images` | 1 | Remove images from documents |
| Redact | `/redact`, `/pdf/redact` | 2 | Text redaction — **not implemented, returns 501** |

## Authentication

All endpoints require a JWT token as an `auth_token` httpOnly cookie.

**Public paths** (no auth): `/health`, `/docs`, `/swagger`, `/openapi.json`, `/api/auth/set-cookie`

## Processing Modes

| Mode | Pattern | Description |
|------|---------|-------------|
| **Sync** | Direct response | Fast operations (<2s): metrics, hash, search, text extraction |
| **Async** | POST → 202 + job_id, GET → poll | Slow/LLM operations: compare, summarize |
| **Batch** | POST /batch/{op} → 202 | Parallel multi-file with per-file webhooks |

## Error Responses

All errors follow [RFC 9457 Problem Details](https://datatracker.ietf.org/doc/html/rfc9457):

```json
{
  "type": "about:blank",
  "title": "Bad Request",
  "status": 400,
  "detail": "File must be a PDF",
  "instance": "/pdf/metrics"
}
```

## Confidentiality Routing

| Level | Description |
|-------|-------------|
| `private` | Documents processed locally via OpenWebUI/Ollama (default) |
| `public` | Documents sent to cloud LLM (e.g. OpenAI GPT-4o) |
| `air_gapped` | Fully offline processing, no LLM calls |

## Quick Start

### Docker

```bash
docker-compose up --build
# API at http://localhost:9401
# Swagger UI at http://localhost:9401/swagger
```

### Local Development

```bash
dotnet restore
dotnet build
dotnet run --project src/Aelena.FileApi.Api -f net10.0
```

The projects multi-target `net10.0` and `net11.0`, so `run`, `publish`, and a
single-project `build` need `-f`. Without the .NET 11 SDK installed, build the
LTS target alone:

```bash
dotnet build -p:TargetFrameworks=net10.0
```

### Run Tests

```bash
dotnet test
```

`dotnet test` reports **580 passing**. That is 290 distinct tests — 193 unit plus
97 endpoint — run once against each target framework:

| Suite | Tests | Frameworks | Executions |
|-------|-------|-----------|-----------|
| `Aelena.FileApi.Tests` (unit, concurrency) | 193 | net10.0, net11.0 | 386 |
| `Aelena.FileApi.Api.Tests` (endpoint, error-contract, auth, share) | 97 | net10.0, net11.0 | 194 |
| **Main solution total** | **290** | | **580** |
| `Aelena.FileApi.Grpc.Tests` (separate solution) | 8 | net10.0, net11.0 | 16 |

To run a single framework: `dotnet test -f net10.0`.

### Build NuGet Package

```bash
dotnet pack src/Aelena.FileApi.Core -c Release -o artifacts/
```

## CLI — Rich Console Interface

The `fileapi` CLI provides direct access to all Core operations from the terminal, with rich Spectre.Console output.

### Install / Run

```bash
# Run via dotnet
dotnet run --project src/Aelena.FileApi.Cli -f net10.0 -- <command> [options]

# Or build and use directly
dotnet build src/Aelena.FileApi.Cli -c Release -f net10.0
./src/Aelena.FileApi.Cli/bin/Release/net10.0/fileapi <command>
```

### Commands

```bash
# PDF operations
fileapi pdf metrics document.pdf          # Page count, words, OCR needs, signatures
fileapi pdf extract-text document.pdf     # Extract all text
fileapi pdf metadata document.pdf         # Title, author, dates, version
fileapi pdf health document.pdf           # Corruption, fonts, JavaScript checks
fileapi pdf merge -o merged.pdf a.pdf b.pdf  # Merge PDFs
fileapi pdf rotate --angle 90 doc.pdf     # Rotate pages
fileapi pdf encrypt --password s3cret doc.pdf  # Password protect
fileapi pdf decrypt --password s3cret doc.pdf  # Remove protection
fileapi pdf search --query "contract" doc.pdf  # Search text

# DOCX operations
fileapi docx metrics report.docx          # Paragraphs, words, tables, images
fileapi docx metadata report.docx         # Title, author, revision
fileapi docx markdown report.docx         # Convert to Markdown
fileapi docx health report.docx           # Tracked changes, macros

# Image operations
fileapi image exif photo.jpg              # EXIF metadata + GPS
fileapi image resize -w 800 photo.jpg     # Resize with aspect ratio
fileapi image rotate --angle 90 photo.jpg # Rotate
fileapi image convert --format webp photo.png  # Format conversion
fileapi image grayscale photo.jpg         # Grayscale
fileapi image blur --radius 5 photo.jpg   # Gaussian blur
fileapi image compress --quality 60 photo.jpg  # JPEG compression

# Utilities
fileapi hash invoice.pdf                  # SHA-256, MD5, SHA-1
fileapi readability essay.txt             # Flesch, Gunning Fog, SMOG scores
fileapi pii detect contract.pdf           # Detect emails, SSNs, credit cards
fileapi txt metrics notes.txt             # Line, word, token counts
fileapi txt search --query "TODO" notes.txt
fileapi zip archive.zip                   # List entries with sizes
fileapi email message.eml                 # Parse headers, body, attachments
```

## Configuration

All settings via environment variables or `appsettings.json` (section `AppSettings`):

| Variable | Default | Description |
|----------|---------|-------------|
| `AppSettings__PublicLlmBaseUrl` | `https://api.openai.com/v1` | Cloud LLM endpoint |
| `AppSettings__PublicLlmApiKey` | | Cloud LLM API key |
| `AppSettings__PublicLlmModel` | `gpt-4o` | Cloud LLM model |
| `AppSettings__PrivateLlmBaseUrl` | `http://host.docker.internal:3000/api/v1` | Local LLM endpoint |
| `AppSettings__PrivateLlmApiKey` | | Local LLM API key |
| `AppSettings__JwtSecretKey` | `your-secret-key-change-in-production` | JWT signing key. **The default is a placeholder** — outside `Development` the app refuses to start until it is replaced with a random value of at least 32 bytes. |
| `AppSettings__JwtAlgorithm` | `HS256` | Signing algorithm; the only one accepted on validation. `HS256`, `HS384`, or `HS512`. |
| `AppSettings__CorsOrigins` | `http://localhost:9600` | Allowed CORS origins |
| `AppSettings__MaxRequestsPerDay` | `0` (unlimited) | Daily request cap per user |
| `AppSettings__MaxFileSizeBytes` | `0` (unlimited) | Max upload size |
| `OpenTelemetry__Endpoint` | | OTLP exporter endpoint |

## Solution Structure

```
file-api/
├── Aelena.FileApi.sln
├── Directory.Build.props          # net10.0, C# 14, nullable, TreatWarningsAsErrors
├── Directory.Packages.props       # Central Package Management: one pinned version per package
├── Directory.Build.targets        # Test-project settings (imports after each csproj)
├── docker-compose.yml
├── prompts/                       # Scriban templates for LLM prompts
│
├── src/
│   ├── Aelena.FileApi.Core/      # NuGet library — ALL business logic
│   │   ├── Models/                # 60+ C# record types
│   │   ├── Enums/                 # Confidentiality, CompareMode, DocumentType, etc.
│   │   ├── Errors/                # FileApiException → ProblemDetails
│   │   ├── Abstractions/          # ILlmClient, ILlmClientFactory
│   │   └── Services/
│   │       ├── Pdf/               # PdfService (iText7) — 23 static methods
│   │       ├── Docx/              # DocxService (Open XML SDK) — 10 methods
│   │       ├── Image/             # ImageService (ImageSharp) — 18 methods
│   │       ├── Llm/               # LlmClientFactory, PromptRenderer, OpenAiCompatibleClient
│   │       ├── Jobs/              # InMemoryJobStore<T>
│   │       ├── Persistence/       # ShareRepository (SQLite/Dapper)
│   │       └── Common/            # TextAnalysis, PageRangeParser, TextSearch, HashService,
│   │                              # TxtService, ZipService, ReadabilityService, PiiService,
│   │                              # EmailService, UserRegex
│   │
│   ├── Aelena.FileApi.Core.Pdf/  # AGPL — PDF only, the sole iText consumer
│   │   └── Services/Pdf/          # PdfService. Kept out of Core so Core is MIT.
│   │
│   ├── Aelena.FileApi.Api/       # HTTP wrapper (Minimal APIs)
│   │   ├── Program.cs             # Top-level: DI, Serilog, OpenTelemetry, all routes
│   │   ├── Endpoints/             # 22 endpoint files + FormFileExtensions
│   │   ├── Middleware/            # Exception, Audit, AuthRateLimit
│   │   ├── Logging/               # Source-generated LoggerMessage delegates
│   │   ├── Auth/                  # JwtCookieAuth
│   │   ├── Services/              # WebhookService
│   │   └── Configuration/         # AppSettings
│   │
│   └── Aelena.FileApi.Cli/       # `fileapi` console app (System.CommandLine 2.0)
│       ├── Commands/              # One file per command group
│       └── Helpers/               # Output, ExitCode, CommandExtensions, Format
│
├── grpc/                          # gRPC port — its own solution, same Core
│   ├── src/Aelena.FileApi.Grpc/
│   └── tests/
│
└── tests/
    ├── Aelena.FileApi.Tests/     # 193 unit tests (xUnit + AwesomeAssertions)
    └── Aelena.FileApi.Api.Tests/ # 97 endpoint, error-contract, auth, and share tests
```

## Tech Stack

| Component | Library |
|-----------|---------|
| PDF | iText7 9.x (**AGPL** — isolated in `Aelena.FileApi.Core.Pdf`) |
| DOCX/PPTX | DocumentFormat.OpenXml 3.x |
| Images | SixLabors.ImageSharp 3.x |
| Email | MimeKit 4.x |
| CLI | System.CommandLine 2.0 + Spectre.Console |
| gRPC | Grpc.AspNetCore 2.x |
| LLM | OpenAI-compatible HTTP client |
| Templates | Scriban 7.x |
| Tokens | SharpToken 2.x |
| SQLite | Microsoft.Data.Sqlite + Dapper |
| Logging | Serilog + OpenTelemetry |
| Testing | xUnit + AwesomeAssertions + NSubstitute |

Package versions are pinned centrally in `Directory.Packages.props`. Nothing
floats — `NuGetAudit` runs at `low` severity across the whole graph and fails
the build on a known advisory.

## Deferred to Separate Projects

| Dependency | Status | Notes |
|------------|--------|-------|
| `imagehash` | Separate NuGet | Perceptual hashing (aHash/pHash/dHash/wHash) |
| `docling` | Separate project | IBM ML document parser — no .NET equivalent |
| GDAL | Partial | Using NetTopologySuite + LibTiff.NET instead |

## Changelog

See [CHANGELOG.md](CHANGELOG.md). The 0.3.0 entry documents the modernization
pass: the .NET 10/11 retarget, and the bugs it turned up — including a redaction
endpoint that returned unredacted documents and share links that ignored their
own passwords and expiry.

## License

Two licences, by package — see the [licensing section](#️-licensing--read-this-before-taking-a-dependency) above.

- **`Aelena.FileApi.Core`** — MIT, see [LICENSE](LICENSE). No copyleft dependencies.
- **`Aelena.FileApi.Core.Pdf`** and the **`fileapi` CLI** — AGPL-3.0-or-later,
  inherited from iText 7. The repository's own source is MIT; the AGPL obligation
  comes from the dependency, and applies to anything that ships or serves it.

Other dependencies keep their own terms. Notably `SixLabors.ImageSharp` is under the
Six Labors Split License: free for open-source and small-business use, commercial
licence otherwise.
