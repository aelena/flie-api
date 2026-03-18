using System.Text.RegularExpressions;

namespace Aelena.FileApi.Core.Services.Common;

/// <summary>
/// Search text for a plain string or regex pattern, returning matches with surrounding context.
/// </summary>
public static class TextSearch
{
    /// <summary>
    /// Search text for a literal query (case-insensitive) or a regex pattern.
    /// Provide exactly one of <paramref name="query"/> or <paramref name="pattern"/>.
    /// </summary>
    /// <param name="text">The text to search within.</param>
    /// <param name="query">Literal search string (case-insensitive). Mutually exclusive with <paramref name="pattern"/>.</param>
    /// <param name="pattern">Regex pattern. Mutually exclusive with <paramref name="query"/>.</param>
    /// <param name="contextChars">Number of context characters to include before and after each match.</param>
    /// <returns>List of matches with position and context.</returns>
    public static IReadOnlyList<Models.SearchMatch> Search(
        string text,
        string? query = null,
        string? pattern = null,
        int contextChars = 80)
    {
        if (query is not null && pattern is not null)
            throw new ArgumentException("Provide either 'query' or 'pattern', not both");
        if (query is null && pattern is null)
            throw new ArgumentException("Provide either 'query' or 'pattern'");

        var regex = query is not null
            ? new Regex(Regex.Escape(query), RegexOptions.IgnoreCase)
            : new Regex(pattern!, RegexOptions.None);

        var matches = new List<Models.SearchMatch>();

        foreach (Match m in regex.Matches(text))
        {
            var ctxStart = Math.Max(0, m.Index - contextChars);
            var ctxEnd = Math.Min(text.Length, m.Index + m.Length + contextChars);

            matches.Add(new Models.SearchMatch(
                Match: m.Value,
                Start: m.Index,
                End: m.Index + m.Length,
                Context: text[ctxStart..ctxEnd]));
        }

        return matches;
    }
}
