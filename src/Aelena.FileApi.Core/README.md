# Aelena.FileApi.Core

Pure document processing for .NET. No ASP.NET dependencies, no copyleft
dependencies — usable from a console app, a desktop app, a cloud function, or
behind any host.

**MIT licensed.** Nothing in this package's dependency graph is copyleft.

## What's here

DOCX metrics, metadata and Markdown conversion · image processing (resize,
rotate, crop, convert, filters, EXIF) · email (.eml) parsing · SHA-256/MD5/SHA-1
and composite hashing · PII detection for 7 countries · readability scores
(Flesch, Gunning Fog, SMOG) · text metrics and regex search · ZIP inspection ·
share-link persistence · an in-memory job store.

```csharp
using Aelena.FileApi.Core.Services.Common;

var hashes = HashService.ComputeHash(bytes, "invoice.pdf");
var pii    = PiiService.Detect(text, "contract.txt");
var score  = ReadabilityService.Analyse(text, "essay.txt", "en");
```

## PDF is a separate package

PDF operations live in **`Aelena.FileApi.Core.Pdf`**, which is
**AGPL-3.0-or-later** because it is built on iText 7. That is why this package is
MIT: it has no reference to iText, direct or transitive, and CI asserts that on
every push.

Only add the PDF package if you have read the AGPL and accept it, or hold a
commercial iText licence.

## Links

- [Repository](https://github.com/aelena/file-api)
- [Licensing in detail](https://github.com/aelena/file-api/blob/main/LICENSING.md)
- [Changelog](https://github.com/aelena/file-api/blob/main/CHANGELOG.md)
