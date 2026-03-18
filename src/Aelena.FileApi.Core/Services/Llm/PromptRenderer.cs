using Scriban;
using Scriban.Runtime;

namespace Aelena.FileApi.Core.Services.Llm;

/// <summary>
/// Renders prompt templates from the <c>prompts/</c> directory using Scriban.
/// Templates use Liquid-like syntax with variables like <c>{{ language }}</c>,
/// conditionals like <c>{% if tone == "legal" %}</c>, etc.
/// Thread-safe — templates are parsed once and cached.
/// </summary>
public sealed class PromptRenderer
{
    private readonly string _promptsDir;
    private readonly Dictionary<string, Template> _cache = new();
    private readonly object _lock = new();

    /// <summary>
    /// Creates a new renderer that loads templates from the specified directory.
    /// </summary>
    /// <param name="promptsDir">Path to the prompts directory (default: "prompts/" relative to app root).</param>
    public PromptRenderer(string? promptsDir = null)
    {
        _promptsDir = promptsDir ?? Path.Combine(AppContext.BaseDirectory, "prompts");
    }

    /// <summary>
    /// Render a named template with the given variables.
    /// </summary>
    /// <param name="templateName">Filename of the template (e.g. "summarize.sbn").</param>
    /// <param name="variables">Key-value pairs to inject into the template.</param>
    /// <returns>The rendered prompt string, trimmed.</returns>
    public string Render(string templateName, IDictionary<string, object?> variables)
    {
        var template = GetOrParseTemplate(templateName);
        var scriptObject = new ScriptObject();

        foreach (var (key, value) in variables)
            scriptObject.Add(key, value);

        var context = new TemplateContext();
        context.PushGlobal(scriptObject);

        return template.Render(context).Trim();
    }

    /// <summary>
    /// Render a named template with anonymous object variables.
    /// </summary>
    public string Render(string templateName, object variables)
    {
        var dict = variables.GetType()
            .GetProperties()
            .ToDictionary(p => p.Name.ToLowerInvariant(), p => p.GetValue(variables));
        return Render(templateName, dict!);
    }

    private Template GetOrParseTemplate(string templateName)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(templateName, out var cached))
                return cached;

            var path = Path.Combine(_promptsDir, templateName);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Prompt template not found: {path}");

            var source = File.ReadAllText(path);
            var template = Template.Parse(source, path);

            if (template.HasErrors)
                throw new InvalidOperationException(
                    $"Template parse errors in {templateName}: {string.Join("; ", template.Messages)}");

            _cache[templateName] = template;
            return template;
        }
    }
}
