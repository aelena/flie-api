using System.Text;
using Aelena.FileApi.Core.Models;

namespace Aelena.FileApi.Core.Services.Common;

/// <summary>
/// Plain text file analysis: metrics and search.
/// </summary>
public static class TxtService
{
    /// <summary>
    /// Compute metrics for a plain text file.
    /// </summary>
    public static TxtMetrics GetMetrics(ReadOnlySpan<byte> data, string fileName)
    {
        var text = Encoding.UTF8.GetString(data);
        var lineCount = string.IsNullOrEmpty(text) ? 0
            : text.Split('\n').Length - (text.EndsWith('\n') ? 1 : 0);

        return new TxtMetrics(
            FileName: fileName,
            FileSizeBytes: data.Length,
            WordCount: TextAnalysis.CountWords(text),
            CharCount: TextAnalysis.CountChars(text),
            TokenCount: TextAnalysis.CountTokens(text),
            Language: TextAnalysis.DetectLanguage(text),
            CreationDate: null,
            LastModifiedDate: null,
            LineCount: lineCount);
    }

    /// <summary>
    /// Search a plain text file for literal or regex matches.
    /// </summary>
    public static (string FileName, IReadOnlyList<SearchMatch> Matches) Search(
        ReadOnlySpan<byte> data, string fileName, string? query = null, string? pattern = null)
    {
        var text = Encoding.UTF8.GetString(data);
        var matches = TextSearch.Search(text, query: query, pattern: pattern);
        return (fileName, matches);
    }
}
