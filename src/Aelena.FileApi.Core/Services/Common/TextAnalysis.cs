using System.Collections.Frozen;
using SharpToken;

namespace Aelena.FileApi.Core.Services.Common;

/// <summary>
/// Text analysis utilities: token counting, word/character counts, and language detection.
/// Stateless and thread-safe — every member is static.
/// </summary>
public static class TextAnalysis
{
    private static readonly GptEncoding DefaultEncoding = GptEncoding.GetEncoding("cl100k_base");

    /// <summary>Shortest text worth guessing a language for.</summary>
    private const int MinimumLengthForDetection = 20;

    /// <summary>
    /// Function words whose frequency distinguishes the supported languages.
    /// </summary>
    /// <remarks>
    /// Ordered so that the first language wins a tie deterministically, rather than
    /// depending on dictionary enumeration order.
    /// </remarks>
    private static readonly FrozenDictionary<string, string[]> LanguageMarkers =
        new Dictionary<string, string[]>
        {
            ["en"] = ["the", "and", "is", "of", "to"],
            ["es"] = ["el", "de", "la", "en", "que", "los"],
            ["fr"] = ["le", "les", "des", "est", "une"],
            ["de"] = ["der", "die", "und", "das", "ist"],
        }.ToFrozenDictionary();

    /// <summary>Count tokens using the cl100k_base encoding (GPT-4 / GPT-3.5-turbo compatible).</summary>
    public static int CountTokens(string text) =>
        DefaultEncoding.Encode(text).Count;

    /// <summary>Count tokens using a specific encoding model name.</summary>
    public static int CountTokens(string text, string model) =>
        GptEncoding.GetEncoding(model).Encode(text).Count;

    /// <summary>Count whitespace-delimited words.</summary>
    public static int CountWords(string text) =>
        text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    /// <summary>Count total characters.</summary>
    public static int CountChars(string text) => text.Length;

    /// <summary>
    /// Detect the dominant language of the text.
    /// Returns an ISO 639-1 code (<c>en</c>, <c>es</c>, <c>fr</c>, <c>de</c>) or <c>null</c>
    /// if detection fails or the text is shorter than 20 characters.
    /// </summary>
    /// <remarks>
    /// A function-word frequency heuristic. It is deliberately crude; for higher
    /// accuracy, integrate NTextCat or a dedicated language identification library.
    /// </remarks>
    public static string? DetectLanguage(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Trim().Length < MinimumLengthForDetection)
            return null;

        var words = Tokenize(text);
        if (words.Count == 0) return null;

        string? best = null;
        var bestScore = 0;

        foreach (var (language, markers) in LanguageMarkers)
        {
            var score = markers.Sum(marker => words.GetValueOrDefault(marker));
            if (score > bestScore)
            {
                (best, bestScore) = (language, score);
            }
        }

        return best;
    }

    /// <summary>Count lowercased word occurrences.</summary>
    /// <remarks>
    /// This replaces a substring scan for <c>" the "</c> and friends, which required a
    /// space on both sides and so missed every marker that opened the text, ended it,
    /// or sat against a newline or punctuation — exactly the positions function words
    /// occupy most often. Those misses understated every score; because the winner is
    /// whichever language scores highest, they mattered most where two languages were
    /// close, which is precisely where the heuristic is weakest to begin with.
    /// </remarks>
    private static Dictionary<string, int> Tokenize(string text)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var range in text.AsSpan().SplitAny(" \t\r\n.,;:!?()[]{}\"'"))
        {
            var word = text.AsSpan()[range].Trim();
            if (word.IsEmpty) continue;

            var key = word.ToString();
            counts[key] = counts.GetValueOrDefault(key) + 1;
        }

        return counts;
    }
}
