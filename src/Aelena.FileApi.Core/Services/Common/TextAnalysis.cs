using SharpToken;

namespace Aelena.FileApi.Core.Services.Common;

/// <summary>
/// Text analysis utilities: token counting, word/character counts, and language detection.
/// Stateless and thread-safe — designed for use as a singleton service.
/// </summary>
public sealed class TextAnalysis
{
    private static readonly GptEncoding Encoding = GptEncoding.GetEncoding("cl100k_base");

    /// <summary>Count tokens using the cl100k_base encoding (GPT-4 / GPT-3.5-turbo compatible).</summary>
    public static int CountTokens(string text) =>
        Encoding.Encode(text).Count;

    /// <summary>Count tokens using a specific encoding model name.</summary>
    public static int CountTokens(string text, string model)
    {
        var enc = GptEncoding.GetEncoding(model);
        return enc.Encode(text).Count;
    }

    /// <summary>Count whitespace-delimited words.</summary>
    public static int CountWords(string text) =>
        text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    /// <summary>Count total characters.</summary>
    public static int CountChars(string text) => text.Length;

    /// <summary>
    /// Detect the dominant language of the text.
    /// Returns an ISO 639-1 code (e.g. "en", "es", "fr") or <c>null</c>
    /// if detection fails or the text is too short (less than 20 characters).
    /// </summary>
    /// <remarks>
    /// Uses a simple trigram-based heuristic. For production use with higher accuracy,
    /// consider integrating NTextCat or a dedicated language detection library.
    /// </remarks>
    public static string? DetectLanguage(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Trim().Length < 20)
            return null;

        // Simple heuristic based on common word frequency.
        // This is a placeholder — Phase 1+ will integrate NTextCat for proper detection.
        var lower = text.ToLowerInvariant();
        var scores = new Dictionary<string, int>
        {
            ["en"] = CountOccurrences(lower, " the ") + CountOccurrences(lower, " and ") +
                     CountOccurrences(lower, " is ") + CountOccurrences(lower, " of ") +
                     CountOccurrences(lower, " to "),
            ["es"] = CountOccurrences(lower, " el ") + CountOccurrences(lower, " de ") +
                     CountOccurrences(lower, " la ") + CountOccurrences(lower, " en ") +
                     CountOccurrences(lower, " que ") + CountOccurrences(lower, " los "),
            ["fr"] = CountOccurrences(lower, " le ") + CountOccurrences(lower, " les ") +
                     CountOccurrences(lower, " des ") + CountOccurrences(lower, " est ") +
                     CountOccurrences(lower, " une "),
            ["de"] = CountOccurrences(lower, " der ") + CountOccurrences(lower, " die ") +
                     CountOccurrences(lower, " und ") + CountOccurrences(lower, " das ") +
                     CountOccurrences(lower, " ist ")
        };

        var best = scores.MaxBy(kv => kv.Value);
        return best.Value > 0 ? best.Key : null;
    }

    private static int CountOccurrences(string text, string pattern)
    {
        var count = 0;
        var idx = 0;
        while ((idx = text.IndexOf(pattern, idx, StringComparison.Ordinal)) != -1)
        {
            count++;
            idx += pattern.Length;
        }
        return count;
    }
}
