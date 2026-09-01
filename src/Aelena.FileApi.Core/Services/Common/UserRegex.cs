using System.Text.RegularExpressions;
using Aelena.FileApi.Core.Errors;

namespace Aelena.FileApi.Core.Services.Common;

/// <summary>
/// Compiles caller-supplied search patterns.
/// </summary>
/// <remarks>
/// <para>
/// Two things every caller needs and none of them were doing on their own.
/// </para>
/// <para>
/// A malformed pattern is the user's typo, not a server fault. Constructing a
/// <see cref="Regex"/> directly throws <see cref="ArgumentException"/>, which the HTTP
/// host could only render as a 500; here it becomes a 400 quoting the pattern.
/// </para>
/// <para>
/// Every pattern also gets a match timeout. The pattern and the text are both supplied
/// by the caller, so a pattern like <c>(a+)+$</c> against a long line of a's is a
/// catastrophic-backtracking denial of service against a shared process. With a timeout
/// the request fails; without one the thread never returns.
/// </para>
/// </remarks>
public static class UserRegex
{
    /// <summary>
    /// How long any single caller-supplied match may run before it is abandoned.
    /// Generous for a legitimate pattern, fatal to a runaway one.
    /// </summary>
    public static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(2);

    /// <summary>Compile a caller-supplied regular expression.</summary>
    /// <param name="pattern">The pattern as typed by the caller.</param>
    /// <param name="options">Regex options to apply; the match timeout is always added.</param>
    /// <exception cref="FileApiException">Status 400 when the pattern does not compile.</exception>
    public static Regex Compile(string pattern, RegexOptions options = RegexOptions.None)
    {
        try
        {
            return new Regex(pattern, options, MatchTimeout);
        }
        catch (ArgumentException ex)
        {
            throw new FileApiException(400,
                $"'{pattern}' is not a valid regular expression: {ex.Message}",
                title: "Invalid Pattern");
        }
    }

    /// <summary>Compile a literal search string into a case-insensitive pattern.</summary>
    public static Regex Literal(string query) =>
        new(Regex.Escape(query), RegexOptions.IgnoreCase, MatchTimeout);

    /// <summary>
    /// Run <paramref name="match"/>, reporting a backtracking blow-up as a client error
    /// rather than letting <see cref="RegexMatchTimeoutException"/> reach the host as a 500.
    /// </summary>
    public static T Guard<T>(Func<T> match, string pattern)
    {
        try
        {
            return match();
        }
        catch (RegexMatchTimeoutException)
        {
            throw new FileApiException(400,
                $"Matching '{pattern}' took longer than {MatchTimeout.TotalSeconds:F0} seconds and was abandoned. "
                + "Anchor the pattern or make it less ambiguous — nested quantifiers such as (a+)+ "
                + "can take exponential time on long inputs.",
                title: "Pattern Too Slow");
        }
    }
}
