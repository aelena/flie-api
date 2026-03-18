using System.CommandLine;
using System.Text;
using Aelena.FileApi.Cli.Helpers;
using Aelena.FileApi.Core.Services.Common;

namespace Aelena.FileApi.Cli.Commands;

public static class ReadabilityCommand
{
    public static Command Create()
    {
        var fileArg = new Argument<FileInfo>("file", "Text file to analyse");
        var langOpt = new Option<string>("--lang", () => "en", "Language for interpretation (en/es)");
        var cmd = new Command("readability", "Compute readability scores (Flesch, Gunning Fog, SMOG)") { fileArg, langOpt };

        cmd.SetHandler((file, lang) =>
        {
            var data = Output.ReadFileWithSpinner(file.FullName);
            var text = Encoding.UTF8.GetString(data);
            var r = ReadabilityService.Analyse(text, file.Name, lang);

            Output.Properties($"Readability: {file.Name}",
                ("Words", r.WordCount.ToString("N0")),
                ("Sentences", r.SentenceCount.ToString()),
                ("Syllables", r.SyllableCount.ToString("N0")),
                ("Complex words", r.ComplexWordCount.ToString()),
                ("Flesch Reading Ease", $"{r.FleschReadingEase:F1}"),
                ("Flesch-Kincaid Grade", $"{r.FleschKincaidGrade:F1}"),
                ("Gunning Fog Index", $"{r.GunningFogIndex:F1}"),
                ("SMOG Index", $"{r.SmogIndex:F1}"),
                ("Interpretation", r.Interpretation));
        }, fileArg, langOpt);

        return cmd;
    }
}
