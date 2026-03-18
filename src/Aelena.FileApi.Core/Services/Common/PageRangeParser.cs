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
    /// <exception cref="ArgumentException">When a page number or range is out of bounds.</exception>
    public static IReadOnlyList<int> Parse(string pages, int maxPage)
    {
        var result = new SortedSet<int>();

        foreach (var part in pages.Split(','))
        {
            var trimmed = part.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            if (trimmed.Contains('-'))
            {
                var bounds = trimmed.Split('-', 2);
                var start = int.Parse(bounds[0].Trim());
                var end = int.Parse(bounds[1].Trim());

                if (start < 1 || end < 1 || start > maxPage || end > maxPage)
                    throw new ArgumentException($"Page range {trimmed} out of bounds (1-{maxPage})");

                for (var i = start; i <= end; i++)
                    result.Add(i - 1);
            }
            else
            {
                var page = int.Parse(trimmed);
                if (page < 1 || page > maxPage)
                    throw new ArgumentException($"Page {page} out of bounds (1-{maxPage})");

                result.Add(page - 1);
            }
        }

        return [.. result];
    }
}
