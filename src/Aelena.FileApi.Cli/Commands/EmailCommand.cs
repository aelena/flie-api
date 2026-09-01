using System.CommandLine;
using Aelena.FileApi.Cli.Helpers;
using Aelena.FileApi.Core.Services.Common;

namespace Aelena.FileApi.Cli.Commands;

/// <summary><c>fileapi email</c> — parse .eml files into headers, body, and attachments.</summary>
public static class EmailCommand
{
    public static Command Create()
    {
        var fileArg = CommandExtensions.FileArgument("Email file (.eml)");
        var cmd = new Command("email", "Parse email files") { fileArg };

        return cmd.WithAction(parse =>
        {
            var file = parse.GetRequiredValue(fileArg);
            var email = EmailService.Parse(Output.ReadFile(file), file.Name);

            Output.Properties($"Email: {file.Name}",
                ("Subject", email.Subject),
                ("From", email.FromAddress),
                ("To", Join(email.To)),
                ("Cc", Join(email.Cc)),
                ("Date", email.Date),
                ("Message-ID", email.MessageId),
                ("Attachments", (email.Attachments?.Count ?? 0).Display()));

            if (email.BodyText is not null)
            {
                Console.WriteLine();
                Console.WriteLine("--- Body ---");
                Console.WriteLine(email.BodyText);
            }
        });
    }

    private static string? Join(IReadOnlyList<string>? addresses) =>
        addresses is { Count: > 0 } ? string.Join(", ", addresses) : null;
}
