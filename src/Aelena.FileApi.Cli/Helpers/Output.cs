using Spectre.Console;

namespace Aelena.FileApi.Cli.Helpers;

/// <summary>Spectre.Console rendering helpers for rich CLI output.</summary>
public static class Output
{
    /// <summary>
    /// Diagnostics go to stderr so that <c>fileapi docx markdown x.docx &gt; out.md</c>
    /// captures the document and not the warnings printed alongside it.
    /// </summary>
    private static readonly IAnsiConsole ErrorConsole = AnsiConsole.Create(new AnsiConsoleSettings
    {
        Out = new AnsiConsoleOutput(Console.Error)
    });

    /// <summary>Render a key-value property table.</summary>
    public static void Properties(string title, params (string Key, string? Value)[] props)
    {
        var table = new Table().Border(TableBorder.Rounded).Title($"[bold]{Markup.Escape(title)}[/]");
        table.AddColumn("Property");
        table.AddColumn("Value");

        foreach (var (key, value) in props)
            table.AddRow(Markup.Escape(key), Markup.Escape(value ?? "(none)"));

        AnsiConsole.Write(table);
    }

    /// <summary>Render a simple list panel.</summary>
    public static void List(string title, IEnumerable<string> items)
    {
        var panel = new Panel(string.Join('\n', items.Select(i => $"  - {Markup.Escape(i)}")))
            .Header($"[bold]{Markup.Escape(title)}[/]")
            .Border(BoxBorder.Rounded);
        AnsiConsole.Write(panel);
    }

    /// <summary>Show a success message.</summary>
    public static void Success(string message) =>
        AnsiConsole.MarkupLine($"[green]OK[/] {Markup.Escape(message)}");

    /// <summary>Report a failure on stderr.</summary>
    public static void Error(string message) =>
        ErrorConsole.MarkupLine($"[red]error:[/] {Markup.Escape(message)}");

    /// <summary>Offer the user a next step after a failure, on stderr.</summary>
    public static void Hint(string message) =>
        ErrorConsole.MarkupLine($"[yellow]hint:[/] {Markup.Escape(message)}");

    /// <summary>Show a file written confirmation.</summary>
    public static void FileWritten(string path, long bytes) =>
        AnsiConsole.MarkupLine($"[green]Wrote[/] {Markup.Escape(path)} ({bytes:N0} bytes)");

    /// <summary>
    /// Read a file's bytes.
    /// </summary>
    /// <remarks>
    /// Existence is enforced by the parser (<c>AcceptExistingOnly</c>), so a missing
    /// file is reported as a usage error before any handler runs. Anything that goes
    /// wrong here is a genuine I/O fault and propagates to the command's error handler
    /// — this method never terminates the process itself.
    /// </remarks>
    public static byte[] ReadFile(FileInfo file) =>
        File.ReadAllBytes(file.FullName);

    /// <summary>Write file bytes and show confirmation.</summary>
    public static void WriteFile(string path, byte[] data)
    {
        File.WriteAllBytes(path, data);
        FileWritten(path, data.Length);
    }
}
