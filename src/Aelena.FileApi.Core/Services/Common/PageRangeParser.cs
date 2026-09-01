using System.Globalization;
using Aelena.FileApi.Core.Errors;

namespace Aelena.FileApi.Core.Services.Common;

/// <summary>
/// Parses page specification strings like "1,3,5-8,12" into sorted zero-indexed page numbers.
/// Input page numbers are 1-based (user-facing). Returned indices are 0-based.
/// </summary>
public static class PageRangeParser
{
    /// <summary>
    /// Parse a page specification string into a sorted list of zero-indexed page numbers.
    /// </summary>
    /// <param name="pages">Comma-separated page numbers and ranges (e.g. "1,3,5-8,12"). 1-based.</param>
    /// <param name="maxPage">Total number of pages in the document (1-based upper bound).</param>
    /// <returns>Sorted, deduplicated list of 0-based page indices.</returns>
    /// <exception cref="FileApiException">
    /// With status 400 when the specification is malformed, names a page outside the
    /// document, or selects nothing.
    /// </exception>
    /// <remarks>
    /// Every rejection is a <see cref="FileApiException"/> so that a caller who typed
    /// "abc" or asked for page 99 of a 3-page document gets a 400 naming the problem.
    /// This previously threw <see cref="ArgumentException"/> and raw
    /// <see cref="FormatException"/>, which the HTTP host could only report as a 500
    /// "An unexpected error occurred."
    /// </remarks>
    public static IReadOnlyList<int> Parse(string pages, int maxPage)
    {
        ArgumentNullException.ThrowIfNull(pages);

        if (maxPage < 1)
            throw new FileApiException(422, "The document has no pages to select from.");

        var result = new SortedSet<int>();

        foreach (var part in pages.Split(','))
        {
            var token = part.Trim();
            if (token.Length == 0) continue;

            if (token.Contains('-', StringComparison.Ordinal))
                AddRange(result, token, maxPage);
            else
                result.Add(ParsePage(token, token, maxPage) - 1);
        }

        // "" or "," parses cleanly but selects nothing. Silently returning an empty
        // document is worse than saying so.
        if (result.Count == 0)
            throw new FileApiException(400,
                $"Page selection '{pages}' does not name any page. "
                + $"Use page numbers or ranges between 1 and {maxPage}, for example \"1,3,5-8\".");

        return [.. result];
    }

    private static void AddRange(SortedSet<int> result, string token, int maxPage)
    {
        var bounds = token.Split('-', 2);
        var start = ParsePage(bounds[0].Trim(), token, maxPage);
        var end = ParsePage(bounds[1].Trim(), token, maxPage);

        // "8-5" used to produce nothing at all: the loop simply never ran, so the
        // caller got an empty selection with no indication anything was wrong.
        if (start > end)
            throw new FileApiException(400,
                $"Page range '{token}' runs backwards. Write it as '{end}-{start}'.");

        for (var page = start; page <= end; page++)
            result.Add(page - 1);
    }

    /// <summary>Parse one 1-based page number, reporting the whole token for context.</summary>
    private static int ParsePage(string value, string token, int maxPage)
    {
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var page))
            throw new FileApiException(400,
                $"'{token}' is not a valid page selection. "
                + "Expected page numbers and ranges, for example \"1,3,5-8,12\".");

        if (page < 1 || page > maxPage)
            throw new FileApiException(400,
                $"Page {page} is out of bounds; the document has {maxPage} "
                + (maxPage == 1 ? "page." : "pages."));

        return page;
    }
}
