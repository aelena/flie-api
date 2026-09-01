using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Aelena.FileApi.Core.Models;

namespace Aelena.FileApi.Core.Services.Common;

/// <summary>
/// Regex-based PII (Personally Identifiable Information) detection.
/// Scans text for emails, credit cards, IBANs, SSNs, phone numbers,
/// national IDs (US, ES, FR, DE, IT, UK, PT), and dates of birth.
/// </summary>
/// <remarks>
/// The patterns are source-generated (<see cref="GeneratedRegexAttribute"/>) rather
/// than compiled at runtime from a static constructor. They are fixed and known at
/// build time, so the matcher is emitted during compilation: no regex parsing or JIT
/// work on first use, and a malformed pattern becomes a build error rather than a
/// <see cref="TypeInitializationException"/> on the first request that touches it.
/// </remarks>
public static partial class PiiService
{
    /// <summary>How much surrounding text to keep alongside each match.</summary>
    private const int ContextChars = 60;

    private static readonly ImmutableArray<(Regex Pattern, string PiiType, string? Country)> Patterns =
    [
        (Email(), "email", null),

        (VisaCard(), "credit_card_visa", null),
        (MasterCard(), "credit_card_mastercard", null),
        (AmexCard(), "credit_card_amex", null),

        (Iban(), "iban", "EU"),

        (UsSsn(), "ssn", "US"),
        (UsPhone(), "phone", "US"),

        (SpanishDni(), "dni", "ES"),
        (SpanishNie(), "nie", "ES"),
        (SpanishPhone(), "phone", "ES"),

        (FrenchInsee(), "insee_ssn", "FR"),
        (FrenchPhone(), "phone", "FR"),

        (GermanPhone(), "phone", "DE"),

        (ItalianCodiceFiscale(), "codice_fiscale", "IT"),

        (UkNationalInsurance(), "national_insurance", "UK"),
        (UkPhone(), "phone", "UK"),

        (PortuguesePhone(), "phone", "PT"),

        (DateOfBirthDayFirst(), "date_of_birth", null),
        (DateOfBirthYearFirst(), "date_of_birth", null),
    ];

    /// <summary>Detect PII in a block of text.</summary>
    public static PiiDetectionResponse Detect(string text, string fileName)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new PiiDetectionResponse(fileName, 0, new Dictionary<string, int>(), []);

        var seen = new HashSet<(string Type, int Start, int End)>();
        var matches = new List<PiiMatch>();

        foreach (var (regex, piiType, country) in Patterns)
        {
            foreach (Match match in regex.Matches(text))
            {
                var span = (piiType, match.Index, match.Index + match.Length);
                if (!seen.Add(span)) continue;

                matches.Add(new PiiMatch(
                    piiType,
                    match.Value,
                    match.Index,
                    match.Index + match.Length,
                    Context(text, match),
                    country));
            }
        }

        // Ordered by position, then by type so that two hits at the same offset keep a
        // stable order: List.Sort is unstable, so the single-key comparison this
        // replaces could return overlapping matches in a different order run to run.
        matches.Sort(static (a, b) =>
            a.Start != b.Start
                ? a.Start.CompareTo(b.Start)
                : string.CompareOrdinal(a.PiiType, b.PiiType));

        var byType = matches
            .GroupBy(m => m.PiiType)
            .ToDictionary(g => g.Key, g => g.Count());

        return new PiiDetectionResponse(fileName, matches.Count, byType, matches);
    }

    private static string Context(string text, Match match)
    {
        var start = Math.Max(0, match.Index - ContextChars);
        var end = Math.Min(text.Length, match.Index + match.Length + ContextChars);
        return text[start..end];
    }

    // ── Patterns ─────────────────────────────────────────────────────────

    [GeneratedRegex(@"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}")]
    private static partial Regex Email();

    [GeneratedRegex(@"\b4\d{3}[\s\-]?\d{4}[\s\-]?\d{4}[\s\-]?\d{4}\b")]
    private static partial Regex VisaCard();

    [GeneratedRegex(@"\b5[1-5]\d{2}[\s\-]?\d{4}[\s\-]?\d{4}[\s\-]?\d{4}\b")]
    private static partial Regex MasterCard();

    [GeneratedRegex(@"\b3[47]\d{2}[\s\-]?\d{6}[\s\-]?\d{5}\b")]
    private static partial Regex AmexCard();

    [GeneratedRegex(@"\b[A-Z]{2}\d{2}[\s]?[\dA-Z]{4}[\s]?[\dA-Z]{4}[\s]?[\dA-Z]{4}[\s]?[\dA-Z]{4}[\s]?[\dA-Z]{0,4}\b")]
    private static partial Regex Iban();

    [GeneratedRegex(@"\b\d{3}-\d{2}-\d{4}\b")]
    private static partial Regex UsSsn();

    [GeneratedRegex(@"\b(?:\+1[\s.\-]?)?\(?\d{3}\)?[\s.\-]?\d{3}[\s.\-]?\d{4}\b")]
    private static partial Regex UsPhone();

    [GeneratedRegex(@"\b\d{8}[A-Z]\b")]
    private static partial Regex SpanishDni();

    [GeneratedRegex(@"\b[XYZ]\d{7}[A-Z]\b")]
    private static partial Regex SpanishNie();

    [GeneratedRegex(@"\b(?:\+34)[\s.\-]?\d{3}[\s.\-]?\d{3}[\s.\-]?\d{3}\b")]
    private static partial Regex SpanishPhone();

    [GeneratedRegex(@"\b[12]\s?\d{2}\s?(?:0[1-9]|1[0-2])\s?\d{2}\s?\d{3}\s?\d{3}\s?\d{2}\b")]
    private static partial Regex FrenchInsee();

    [GeneratedRegex(@"\b(?:\+33)[\s.\-]?\d[\s.\-]?\d{2}[\s.\-]?\d{2}[\s.\-]?\d{2}[\s.\-]?\d{2}\b")]
    private static partial Regex FrenchPhone();

    [GeneratedRegex(@"\b(?:\+49)[\s.\-]?\d{2,5}[\s.\-]?\d{3,8}\b")]
    private static partial Regex GermanPhone();

    [GeneratedRegex(@"\b[A-Z]{6}\d{2}[A-EHLMPR-T](?:0[1-9]|[12]\d|3[01])[A-Z]\d{3}[A-Z]\b")]
    private static partial Regex ItalianCodiceFiscale();

    [GeneratedRegex(@"\b[A-CEGHJ-PR-TW-Z]{2}\s?\d{2}\s?\d{2}\s?\d{2}\s?[A-D]\b")]
    private static partial Regex UkNationalInsurance();

    [GeneratedRegex(@"\b(?:\+44)[\s.\-]?\d{4}[\s.\-]?\d{6}\b")]
    private static partial Regex UkPhone();

    [GeneratedRegex(@"\b(?:\+351)[\s.\-]?\d{3}[\s.\-]?\d{3}[\s.\-]?\d{3}\b")]
    private static partial Regex PortuguesePhone();

    [GeneratedRegex(@"\b(?:0[1-9]|[12]\d|3[01])[/\-.](?:0[1-9]|1[0-2])[/\-.](?:19|20)\d{2}\b")]
    private static partial Regex DateOfBirthDayFirst();

    [GeneratedRegex(@"\b(?:19|20)\d{2}[/\-.](?:0[1-9]|1[0-2])[/\-.](?:0[1-9]|[12]\d|3[01])\b")]
    private static partial Regex DateOfBirthYearFirst();
}
