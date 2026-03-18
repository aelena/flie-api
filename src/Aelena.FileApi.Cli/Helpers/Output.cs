using Spectre.Console;

namespace Aelena.FileApi.Cli.Helpers;

/// <summary>Spectre.Console rendering helpers for rich CLI output.</summary>
public static class Output
{
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
        var panel = new Panel(string.Join("\n", items.Select(i => $"  - {Markup.Escape(i)}")))
            .Header($"[bold]{Markup.Escape(title)}[/]")
            .Border(BoxBorder.Rounded);
        AnsiConsole.Write(panel);
    }

    /// <summary>Show a success message.</summary>
    public static void Success(string message) =>
        AnsiConsole.MarkupLine($"[green]OK[/] {Markup.Escape(message)}");

    /// <summary>Show an error message.</summary>
    public static void Error(string message) =>
        AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(message)}");

    /// <summary>Show a file written confirmation.</summary>
    public static void FileWritten(string path, long bytes) =>
        AnsiConsole.MarkupLine($"[green]Wrote[/] {Markup.Escape(path)} ({bytes:N0} bytes)");

    /// <summary>Read file bytes with spinner.</summary>
    public static byte[] ReadFileWithSpinner(string path)
    {
        if (!File.Exists(path))
        {
            Error($"File not found: {path}");
            Environment.Exit(1);
        }
        return File.ReadAllBytes(path);
    }

    /// <summary>Write file bytes and show confirmation.</summary>
    public static void WriteFile(string path, byte[] data)
    {
        File.WriteAllBytes(path, data);
        FileWritten(path, data.Length);
    }
}
