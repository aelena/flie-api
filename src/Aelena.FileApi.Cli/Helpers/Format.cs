using System.Globalization;

namespace Aelena.FileApi.Cli.Helpers;

/// <summary>
/// Number formatting for terminal output.
/// </summary>
/// <remarks>
/// Everything the CLI prints is read by a person, so it is formatted in the
/// operator's own locale — a German user should see <c>1.234</c>, not <c>1,234</c>.
/// That is the opposite of what Core does when it writes Markdown or a CRC, where
/// the invariant culture is required so the output does not change shape with the
/// machine it ran on. Naming the two cases apart keeps that distinction visible
/// instead of leaving it to whichever overload someone reached for.
/// </remarks>
public static class Format
{
    /// <summary>Format an integer for display in the current locale.</summary>
    public static string Display(this int value) =>
        value.ToString(CultureInfo.CurrentCulture);

    /// <summary>Format an integer for display in the current locale.</summary>
    public static string Display(this int value, string format) =>
        value.ToString(format, CultureInfo.CurrentCulture);

    /// <summary>Format a long for display in the current locale.</summary>
    public static string Display(this long value, string format) =>
        value.ToString(format, CultureInfo.CurrentCulture);
}
