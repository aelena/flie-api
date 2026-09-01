using System.Text.RegularExpressions;
using Aelena.FileApi.Core.Errors;
using Aelena.FileApi.Core.Models;

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
    /// <exception cref="FileApiException">
    /// Status 400 when neither or both of <paramref name="query"/> and <paramref name="pattern"/>
    /// are given, when the pattern does not compile, or when matching times out.
    /// </exception>
    public static IReadOnlyList<SearchMatch> Search(
        string text,
        string? query = null,
        string? pattern = null,
        int contextChars = 80)
    {
        ArgumentNullException.ThrowIfNull(text);

        // These were ArgumentException, which the HTTP host reported as a 500 even
        // though sending both parameters is squarely the caller's mistake.
        if (query is not null && pattern is not null)
            throw new FileApiException(400,
                "Provide either 'query' (literal text) or 'pattern' (a regular expression), not both.");

        if (query is null && pattern is null)
            throw new FileApiException(400,
                "Provide either 'query' (literal text) or 'pattern' (a regular expression).");

        if (contextChars < 0)
            throw new FileApiException(400, "'contextChars' cannot be negative.");

        var expression = query is not null
            ? UserRegex.Literal(query)
            : UserRegex.Compile(pattern!);

        var describedAs = query ?? pattern!;

        return UserRegex.Guard(
            () => expression.Matches(text).Select(m => ToMatch(m, text, contextChars)).ToArray(),
            describedAs);
    }

    private static SearchMatch ToMatch(Match m, string text, int contextChars)
    {
        var start = Math.Max(0, m.Index - contextChars);
        var end = Math.Min(text.Length, m.Index + m.Length + contextChars);

        return new SearchMatch(
            Match: m.Value,
            Start: m.Index,
            End: m.Index + m.Length,
            Context: text[start..end]);
    }
}
