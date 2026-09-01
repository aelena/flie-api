# Aelena.FileApi.Core

Pure document processing for .NET — DOCX, images, email, hashing, PII detection,
readability, text analysis and ZIP inspection.

No ASP.NET dependencies and no copyleft dependencies, so it works equally well in
a console app, a desktop app, an Azure Function, or behind an HTTP or gRPC host.
Every operation is a static method taking `byte[]` and returning an immutable
record — no streams to manage, no `IFormFile`, no shared mutable state.

**MIT licensed.** Nothing in this package's dependency graph is copyleft.

> 📖 **Full documentation, examples and the complete endpoint list are in the
> [repository README](https://github.com/aelena/file-api#readme).**

---

## Install

```bash
dotnet add package Aelena.FileApi.Core
```

Targets `net10.0` and `net11.0`.

## What it does

| Area | Operations |
|------|-----------|
| **DOCX** | Metrics, metadata, paragraph extraction, Markdown conversion, search, health check, metadata removal |
| **Images** | Resize, rotate, crop, convert (PNG/JPEG/WebP/BMP/GIF/TIFF), thumbnail, flip, blur, grayscale, compress, strip metadata, EXIF, auto-orient, invert, edge detect, equalize, colour palette, base64 |
| **Email** | `.eml` (RFC 5322 / MIME) parsing — headers, body, attachment metadata |
| **Hashing** | SHA-256, MD5, SHA-1, and a composite hash that folds in filename and size |
| **PII** | Emails, credit cards, IBANs, SSNs, phone numbers, national IDs (US, ES, FR, DE, IT, UK, PT), dates of birth |
| **Text** | Word/char/token counts, language detection, literal and regex search with context |
| **Readability** | Flesch Reading Ease, Flesch-Kincaid, Gunning Fog, SMOG |
| **ZIP** | Entry listing with sizes, compression and CRC-32, without extracting |
| **Persistence** | SQLite-backed share links; a bounded in-memory job store |

## Example

```csharp
using Aelena.FileApi.Core.Services.Common;

var bytes = await File.ReadAllBytesAsync("contract.docx");

// Content fingerprints
var hashes = HashService.ComputeHash(bytes, "contract.docx");
Console.WriteLine(hashes.Sha256);

// Personal data, with position and surrounding context
var pii = PiiService.Detect(text, "contract.docx");
foreach (var match in pii.Matches)
    Console.WriteLine($"{match.PiiType}: {match.Value} at {match.Start}");

// Readability
var score = ReadabilityService.Analyse(text, "contract.docx", language: "en");
Console.WriteLine($"Flesch {score.FleschReadingEase:F1} — {score.Interpretation}");
```

Expected failures — an unsupported format, an out-of-range page, a malformed
regex — throw `FileApiException`, which carries an HTTP status code and a
message written for the caller rather than the maintainer. Caller-supplied
regular expressions run with a match timeout.

## PDF is a separate package, deliberately

PDF operations live in
**[`Aelena.FileApi.Core.Pdf`](https://www.nuget.org/packages/Aelena.FileApi.Core.Pdf)**,
which is **AGPL-3.0-or-later** because it is built on iText 7.

That separation is the reason this package can be MIT: it has no reference to
iText, direct or transitive. CI asserts it on every push by inspecting both the
declared dependencies and the files inside the packed `.nupkg`, so the boundary
cannot quietly erode. Add the PDF package only if you have read the AGPL and
accept it, or hold a commercial iText licence.

## The three packages

| Package | Licence | Depends on | Contains |
|---------|---------|-----------|----------|
| [`Aelena.FileApi.Core`](https://www.nuget.org/packages/Aelena.FileApi.Core) | MIT | — | Everything except PDF |
| [`Aelena.FileApi.Core.Pdf`](https://www.nuget.org/packages/Aelena.FileApi.Core.Pdf) | AGPL-3.0-or-later | `Core`, iText 7 | PDF only |
| [`Aelena.FileApi.Cli`](https://www.nuget.org/packages/Aelena.FileApi.Cli) | AGPL-3.0-or-later | bundled, not declared | Both, as a `dotnet tool` |

```
Aelena.FileApi.Core          MIT, no iText anywhere in its graph
        ▲
        │ depends on
        │
Aelena.FileApi.Core.Pdf      AGPL — adds iText 7, and only this package does
```

`Core.Pdf` depends on `Core`, never the reverse: installing the PDF package gives
you the whole toolkit, while installing `Core` alone keeps every copyleft
dependency out of your build. The CLI is a tool package, so it bundles its
dependencies rather than declaring them — its empty dependency list does not mean
iText is absent from it.


## Note on ImageSharp

Imaging goes through `SixLabors.ImageSharp`, under the Six Labors Split License.
The clause that matters grants Apache 2.0 to anyone "consuming the Work as a
**Transitive Package Dependency**" — which is what installing this package makes
it. So you receive ImageSharp under Apache 2.0 regardless of your organisation's
size; the commercial threshold applies to a *direct* dependency on ImageSharp,
not to users of this package.

## More

- 📖 [Repository and full documentation](https://github.com/aelena/file-api#readme)
- ⚖️ [Licensing explained in detail](https://github.com/aelena/file-api/blob/main/LICENSING.md)
- 📝 [Changelog](https://github.com/aelena/file-api/blob/main/CHANGELOG.md)
- 🐛 [Issues](https://github.com/aelena/file-api/issues)
