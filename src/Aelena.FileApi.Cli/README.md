# fileapi — command-line document processing

```bash
dotnet tool install -g Aelena.FileApi.Cli
```

Installs the `fileapi` command.

```bash
fileapi hash invoice.pdf                  # SHA-256, MD5, SHA-1, composite
fileapi pdf metrics report.pdf            # pages, words, OCR needs, signatures
fileapi pdf extract-text report.pdf --pages 1,3,5-8
fileapi docx markdown notes.docx          # convert to Markdown
fileapi image resize --width 800 photo.jpg
fileapi pii detect contract.txt           # emails, cards, national IDs
fileapi readability essay.txt             # Flesch, Gunning Fog, SMOG
fileapi zip archive.zip                   # list entries without extracting
```

Failures print one line on stderr with a meaningful exit code: `0` success,
`1` failure, `2` bad input or arguments, `3` unsupported operation, `4` I/O error.

## ⚠️ This tool is AGPL-3.0-or-later

It includes PDF support via `Aelena.FileApi.Core.Pdf`, which is built on
[iText 7](https://itextpdf.com/) under the AGPL. Using the tool is fine; building
it into something you distribute or serve carries the AGPL obligation.

The library without PDF,
[`Aelena.FileApi.Core`](https://www.nuget.org/packages/Aelena.FileApi.Core), is MIT.

## Links

- [Repository](https://github.com/aelena/file-api)
- [Licensing in detail](https://github.com/aelena/file-api/blob/main/LICENSING.md)
