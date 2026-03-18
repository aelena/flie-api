# FileApi — Document Processing & AI Analysis Platform

[![Build](https://img.shields.io/badge/build-passing-brightgreen)]()
[![Tests](https://img.shields.io/badge/tests-175%20passing-brightgreen)]()
[![.NET](https://img.shields.io/badge/.NET-8.0-blue)]()
[![License](https://img.shields.io/badge/license-MIT-green)]()

A comprehensive .NET 8 / C# 12 API for document analysis, comparison, transformation, and AI-powered insights. Designed as both a **standalone NuGet library** (`Aelena.FileApi.Core`) and an **HTTP API** (`Aelena.FileApi.Api`) using ASP.NET Core Minimal APIs.

## Architecture

```
Aelena.FileApi.Core        →  NuGet package (usable standalone in any .NET project)
Aelena.FileApi.Api         →  Thin HTTP wrapper (ASP.NET Core Minimal APIs)
Aelena.FileApi.Grpc        →  gRPC services for high-performance scenarios (planned)
```

### Design Principles

- **Terse & functional** — C# 12 records, pattern matching, expression-bodied lambdas, static services
- **Dual delivery** — All business logic in the Core NuGet package; the API is just the HTTP wrapper
- **Observability** — OpenTelemetry traces + metrics + logs, Serilog structured logging
- **Cloud-ready** — Docker multi-stage build, deployable to Azure Container Apps, App Service, AKS, or serverless

## Features

| Category | Description | Status |
|----------|-------------|--------|
| **PDF Toolkit** | 30+ operations: metrics, metadata, extract text/pages/markdown/tables/annotations/bookmarks, merge, split, rotate, reorder, delete pages, watermark, encrypt/decrypt, compress, page numbers, redact, form fields, health check | Implemented |
| **DOCX Processing** | Metrics, metadata, paragraph extraction, markdown conversion, search, health check, metadata removal | Implemented |
| **Image Processing** | Resize, rotate, crop, convert (PNG/JPEG/WebP/BMP/GIF/TIFF), thumbnail, flip, blur, grayscale, compress, strip metadata, EXIF, auto-orient, invert, edge detect, equalize, color palette, base64 | Implemented |
| **PII Detection** | Regex-based scanning for emails, credit cards (Visa/MC/Amex), IBANs, SSNs, phone numbers, national IDs (US/ES/FR/DE/IT/UK/PT), dates of birth | Implemented |
| **Text Analysis** | Metrics, search (literal + regex), readability scores (Flesch, Gunning Fog, SMOG) | Implemented |
| **Email Parsing** | .eml (RFC 5322 / MIME) parsing with MimeKit — headers, body, attachments | Implemented |
| **File Hashing** | SHA-256, MD5, SHA-1, composite hash | Implemented |
| **ZIP Inspection** | List entries with sizes, compression, CRC-32 | Implemented |
| **Share Links** | CRUD with SQLite persistence, password protection, expiration, access control | Implemented |
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
| Redact | `/redact` | 1 | Black-box text redaction |

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
dotnet run --project src/Aelena.FileApi.Api
```

### Run Tests

```bash
dotnet test
# 175 tests: 157 unit + 18 endpoint/integration
```

### Build NuGet Package

```bash
dotnet pack src/Aelena.FileApi.Core -c Release -o artifacts/
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
| `AppSettings__JwtSecretKey` | `your-secret-key-change-in-production` | JWT signing key |
| `AppSettings__CorsOrigins` | `http://localhost:9600` | Allowed CORS origins |
| `AppSettings__MaxRequestsPerDay` | `0` (unlimited) | Daily request cap per user |
| `AppSettings__MaxFileSizeBytes` | `0` (unlimited) | Max upload size |
| `OpenTelemetry__Endpoint` | | OTLP exporter endpoint |

## Solution Structure

```
netdocserve/
├── Aelena.FileApi.sln
├── Directory.Build.props          # C# 12, nullable, TreatWarningsAsErrors
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
│   │                              # EmailService, WebhookService
│   │
│   └── Aelena.FileApi.Api/       # HTTP wrapper (Minimal APIs)
│       ├── Program.cs             # Top-level: DI, Serilog, OpenTelemetry, all routes
│       ├── Endpoints/             # 22 endpoint files
│       ├── Middleware/            # Exception, Audit, AuthRateLimit
│       ├── Auth/                  # JwtCookieAuth
│       └── Configuration/        # AppSettings
│
└── tests/
    ├── Aelena.FileApi.Tests/     # 157 unit tests (xUnit + FluentAssertions)
    └── Aelena.FileApi.Api.Tests/ # 18 endpoint tests (WebApplicationFactory)
```

## Tech Stack

| Component | Library |
|-----------|---------|
| PDF | iText7 8.x (AGPL) |
| DOCX/PPTX | DocumentFormat.OpenXml 3.x |
| Images | SixLabors.ImageSharp 3.x |
| Email | MimeKit 4.x |
| LLM | OpenAI-compatible HTTP client |
| Templates | Scriban 5.x |
| Tokens | SharpToken 2.x |
| SQLite | Microsoft.Data.Sqlite + Dapper |
| Logging | Serilog + OpenTelemetry |
| Testing | xUnit + FluentAssertions + NSubstitute |

## Deferred to Separate Projects

| Dependency | Status | Notes |
|------------|--------|-------|
| `imagehash` | Separate NuGet | Perceptual hashing (aHash/pHash/dHash/wHash) |
| `docling` | Separate project | IBM ML document parser — no .NET equivalent |
| GDAL | Partial | Using NetTopologySuite + LibTiff.NET instead |

## License

MIT — see [LICENSE](LICENSE) for details.
