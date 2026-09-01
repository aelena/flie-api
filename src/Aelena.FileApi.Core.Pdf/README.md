# Aelena.FileApi.Core.Pdf

PDF operations for `Aelena.FileApi.Core`, built on iText 7.

## ⚠️ This package is AGPL-3.0-or-later

It depends on [iText 7](https://itextpdf.com/), which is licensed under the
**GNU Affero General Public License v3** or, at your option, under a commercial
licence sold by iText Software.

The AGPL is a strong copyleft licence, and unlike the GPL its obligation is
triggered by **running the software as a network service**, not only by
distributing it. If you use this package in a web application, that obligation
reaches your application.

If you cannot accept that, either buy a commercial iText licence — an agreement
between you and iText Software, which installing this package does not grant —
or do not use this package.

**[`Aelena.FileApi.Core`](https://www.nuget.org/packages/Aelena.FileApi.Core) is
MIT** and provides everything else in the toolkit: DOCX, images, email, hashing,
PII detection, readability, text and ZIP. It has no reference to iText. PDF was
separated into this package precisely so that the rest could stay MIT.

## What's here

Metrics, metadata, text/page/Markdown extraction, annotations and bookmarks,
merge, split, rotate, reorder, delete pages, watermark, encrypt/decrypt,
compress, page numbers, form fields, and a health check.

Table extraction and redaction are **not implemented** and answer with a clear
error rather than a wrong answer — see the changelog for why.

## Links

- [Repository](https://github.com/aelena/file-api)
- [Licensing in detail](https://github.com/aelena/file-api/blob/main/LICENSING.md)
