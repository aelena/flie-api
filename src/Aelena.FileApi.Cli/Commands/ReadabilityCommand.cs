using System.CommandLine;
using System.Text;
using Aelena.FileApi.Cli.Helpers;
using Aelena.FileApi.Core.Services.Common;

namespace Aelena.FileApi.Cli.Commands;

/// <summary><c>fileapi readability</c> — Flesch, Gunning Fog, and SMOG scores.</summary>
public static class ReadabilityCommand
{
    public static Command Create()
    {
        var fileArg = CommandExtensions.FileArgument("Text file to analyse");
        var langOpt = new Option<string>("--lang")
        {
            Description = "Language used to phrase the interpretation",
            DefaultValueFactory = _ => "en"
        }.AcceptOnlyFromAmong("en", "es");

        var cmd = new Command(
            "readability",
            "Compute readability scores (Flesch, Gunning Fog, SMOG)") { fileArg, langOpt };

        return cmd.WithAction(parse =>
        {
            var file = parse.GetRequiredValue(fileArg);
            var text = Encoding.UTF8.GetString(Output.ReadFile(file));
            var scores = ReadabilityService.Analyse(text, file.Name, parse.GetRequiredValue(langOpt));

            Output.Properties($"Readability: {file.Name}",
                ("Words", scores.WordCount.Display("N0")),
                ("Sentences", scores.SentenceCount.Display()),
                ("Syllables", scores.SyllableCount.Display("N0")),
                ("Complex words", scores.ComplexWordCount.Display()),
                ("Flesch Reading Ease", $"{scores.FleschReadingEase:F1}"),
                ("Flesch-Kincaid Grade", $"{scores.FleschKincaidGrade:F1}"),
                ("Gunning Fog Index", $"{scores.GunningFogIndex:F1}"),
                ("SMOG Index", $"{scores.SmogIndex:F1}"),
                ("Interpretation", scores.Interpretation));
        });
    }
}
