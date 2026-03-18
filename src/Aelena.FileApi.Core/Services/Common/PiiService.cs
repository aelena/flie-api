using System.Text.RegularExpressions;
using Aelena.FileApi.Core.Models;

namespace Aelena.FileApi.Core.Services.Common;

/// <summary>
/// Regex-based PII (Personally Identifiable Information) detection.
/// Scans text for emails, credit cards, IBANs, SSNs, phone numbers,
/// national IDs (US, ES, FR, DE, IT, UK, PT), and dates of birth.
/// </summary>
public static class PiiService
{
    private static readonly List<(Regex Pattern, string PiiType, string? Country)> Patterns = [];

    static PiiService()
    {
        // Email
        P(@"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}", "email");
        // Credit cards
        P(@"\b4\d{3}[\s\-]?\d{4}[\s\-]?\d{4}[\s\-]?\d{4}\b", "credit_card_visa");
        P(@"\b5[1-5]\d{2}[\s\-]?\d{4}[\s\-]?\d{4}[\s\-]?\d{4}\b", "credit_card_mastercard");
        P(@"\b3[47]\d{2}[\s\-]?\d{6}[\s\-]?\d{5}\b", "credit_card_amex");
        // IBAN
        P(@"\b[A-Z]{2}\d{2}[\s]?[\dA-Z]{4}[\s]?[\dA-Z]{4}[\s]?[\dA-Z]{4}[\s]?[\dA-Z]{4}[\s]?[\dA-Z]{0,4}\b", "iban", "EU");
        // US
        P(@"\b\d{3}-\d{2}-\d{4}\b", "ssn", "US");
        P(@"\b(?:\+1[\s.\-]?)?\(?\d{3}\)?[\s.\-]?\d{3}[\s.\-]?\d{4}\b", "phone", "US");
        // Spain
        P(@"\b\d{8}[A-Z]\b", "dni", "ES");
        P(@"\b[XYZ]\d{7}[A-Z]\b", "nie", "ES");
        P(@"\b(?:\+34)[\s.\-]?\d{3}[\s.\-]?\d{3}[\s.\-]?\d{3}\b", "phone", "ES");
        // France
        P(@"\b[12]\s?\d{2}\s?(?:0[1-9]|1[0-2])\s?\d{2}\s?\d{3}\s?\d{3}\s?\d{2}\b", "insee_ssn", "FR");
        P(@"\b(?:\+33)[\s.\-]?\d[\s.\-]?\d{2}[\s.\-]?\d{2}[\s.\-]?\d{2}[\s.\-]?\d{2}\b", "phone", "FR");
        // Germany
        P(@"\b(?:\+49)[\s.\-]?\d{2,5}[\s.\-]?\d{3,8}\b", "phone", "DE");
        // Italy
        P(@"\b[A-Z]{6}\d{2}[A-EHLMPR-T](?:0[1-9]|[12]\d|3[01])[A-Z]\d{3}[A-Z]\b", "codice_fiscale", "IT");
        // UK
        P(@"\b[A-CEGHJ-PR-TW-Z]{2}\s?\d{2}\s?\d{2}\s?\d{2}\s?[A-D]\b", "national_insurance", "UK");
        P(@"\b(?:\+44)[\s.\-]?\d{4}[\s.\-]?\d{6}\b", "phone", "UK");
        // Portugal
        P(@"\b(?:\+351)[\s.\-]?\d{3}[\s.\-]?\d{3}[\s.\-]?\d{3}\b", "phone", "PT");
        // Dates of birth
        P(@"\b(?:0[1-9]|[12]\d|3[01])[/\-.](?:0[1-9]|1[0-2])[/\-.](?:19|20)\d{2}\b", "date_of_birth");
        P(@"\b(?:19|20)\d{2}[/\-.](?:0[1-9]|1[0-2])[/\-.](?:0[1-9]|[12]\d|3[01])\b", "date_of_birth");
    }

    private static void P(string pattern, string piiType, string? country = null) =>
        Patterns.Add((new Regex(pattern, RegexOptions.Compiled), piiType, country));

    /// <summary>Detect PII in a block of text.</summary>
    public static PiiDetectionResponse Detect(string text, string fileName)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new PiiDetectionResponse(fileName, 0, new Dictionary<string, int>(), []);

        var matches = new List<PiiMatch>();
        var seen = new HashSet<(string, int, int)>();

        foreach (var (regex, piiType, country) in Patterns)
        {
            foreach (Match m in regex.Matches(text))
            {
                var key = (piiType, m.Index, m.Index + m.Length);
                if (!seen.Add(key)) continue;

                var ctxStart = Math.Max(0, m.Index - 60);
                var ctxEnd = Math.Min(text.Length, m.Index + m.Length + 60);

                matches.Add(new PiiMatch(piiType, m.Value, m.Index, m.Index + m.Length,
                    text[ctxStart..ctxEnd], country));
            }
        }

        matches.Sort((a, b) => a.Start.CompareTo(b.Start));

        var byType = matches.GroupBy(m => m.PiiType)
            .ToDictionary(g => g.Key, g => g.Count());

        return new PiiDetectionResponse(fileName, matches.Count, byType, matches);
    }
}
