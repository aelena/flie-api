using System.CommandLine;
using Aelena.FileApi.Cli.Helpers;
using Aelena.FileApi.Core.Services.Common;

namespace Aelena.FileApi.Cli.Commands;

public static class EmailCommand
{
    public static Command Create()
    {
        var fileArg = new Argument<FileInfo>("file", "Email file (.eml or .msg)");
        var cmd = new Command("email", "Parse email files") { fileArg };

        cmd.SetHandler(file =>
        {
            var data = Output.ReadFileWithSpinner(file.FullName);
            var e = EmailService.Parse(data, file.Name);

            Output.Properties($"Email: {file.Name}",
                ("Subject", e.Subject),
                ("From", e.FromAddress),
                ("To", e.To is not null ? string.Join(", ", e.To) : null),
                ("Cc", e.Cc is not null ? string.Join(", ", e.Cc) : null),
                ("Date", e.Date),
                ("Message-ID", e.MessageId),
                ("Attachments", e.Attachments?.Count.ToString() ?? "0"));

            if (e.BodyText is not null)
            {
                Console.WriteLine();
                Console.WriteLine("--- Body ---");
                Console.WriteLine(e.BodyText);
            }
        }, fileArg);

        return cmd;
    }
}
