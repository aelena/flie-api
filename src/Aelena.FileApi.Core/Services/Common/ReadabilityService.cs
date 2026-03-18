using System.Text.RegularExpressions;
using Aelena.FileApi.Core.Models;

namespace Aelena.FileApi.Core.Services.Common;

/// <summary>
/// Computes readability scores: Flesch Reading Ease, Flesch-Kincaid Grade Level,
/// Gunning Fog Index, and SMOG Index.
/// </summary>
public static partial class ReadabilityService
{
    /// <summary>
    /// Analyse text and return readability metrics.
    /// </summary>
    /// <param name="text">Input text to analyse.</param>
    /// <param name="fileName">Source file name.</param>
    /// <param name="language">Language for interpretation text ("en" or "es").</param>
    public static ReadabilityResponse Analyse(string text, string fileName, string language = "en")
    {
        var words = WordRegex().Matches(text).Select(m => m.Value).ToList();
        var sentences = SentenceRegex().Split(text).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

        var wordCount = words.Count;
        var sentenceCount = Math.Max(sentences.Count, 1);

        if (wordCount == 0)
        {
            var msg = language == "es" ? "Documento sin texto analizable" : "Document contains no analysable text";
            return new ReadabilityResponse(fileName, 0, 0, 0, 0, 0, 0, 0, 0, msg);
        }

        var syllableCount = words.Sum(CountSyllables);
        var complexWordCount = words.Count(w => CountSyllables(w) >= 3);

        var avgSentenceLen = (double)wordCount / sentenceCount;
        var avgSyllablesPerWord = (double)syllableCount / wordCount;

        var fleschEase = 206.835 - 1.015 * avgSentenceLen - 84.6 * avgSyllablesPerWord;
        var fkGrade = 0.39 * avgSentenceLen + 11.8 * avgSyllablesPerWord - 15.59;
        var fog = 0.4 * (avgSentenceLen + 100.0 * complexWordCount / wordCount);
        var smog = 1.0430 * Math.Sqrt(complexWordCount * (30.0 / sentenceCount)) + 3.1291;

        return new ReadabilityResponse(
            FileName: fileName,
            WordCount: wordCount,
            SentenceCount: sentenceCount,
            SyllableCount: syllableCount,
            ComplexWordCount: complexWordCount,
            FleschReadingEase: Math.Round(fleschEase, 2),
            FleschKincaidGrade: Math.Round(fkGrade, 2),
            GunningFogIndex: Math.Round(fog, 2),
            SmogIndex: Math.Round(smog, 2),
            Interpretation: InterpretFlesch(fleschEase, language));
    }

    /// <summary>Estimate syllable count for an English word.</summary>
    public static int CountSyllables(string word)
    {
        word = word.ToLowerInvariant().Trim();
        if (string.IsNullOrEmpty(word)) return 0;
        if (word.Length <= 3) return 1;

        // Remove trailing silent-e
        if (word.EndsWith('e'))
            word = word[..^1];

        var vowelGroups = VowelGroupRegex().Matches(word);
        return Math.Max(vowelGroups.Count, 1);
    }

    private static string InterpretFlesch(double score, string language) => language switch
    {
        "es" => score switch
        {
            >= 90 => "Muy fácil de leer (5.º grado)",
            >= 80 => "Fácil de leer (6.º grado)",
            >= 70 => "Bastante fácil de leer (7.º grado)",
            >= 60 => "Inglés estándar / sencillo (8.º-9.º grado)",
            >= 50 => "Bastante difícil de leer (10.º-12.º grado)",
            >= 30 => "Difícil de leer (nivel universitario)",
            _ => "Muy difícil de leer (nivel posgrado)"
        },
        _ => score switch
        {
            >= 90 => "Very easy to read (5th grade)",
            >= 80 => "Easy to read (6th grade)",
            >= 70 => "Fairly easy to read (7th grade)",
            >= 60 => "Standard / plain English (8th-9th grade)",
            >= 50 => "Fairly difficult to read (10th-12th grade)",
            >= 30 => "Difficult to read (college level)",
            _ => "Very difficult to read (college graduate level)"
        }
    };

    [GeneratedRegex(@"[a-zA-Z]+")]
    private static partial Regex WordRegex();

    [GeneratedRegex(@"[.!?]+")]
    private static partial Regex SentenceRegex();

    [GeneratedRegex(@"[aeiouy]+")]
    private static partial Regex VowelGroupRegex();
}
