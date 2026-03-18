using System.CommandLine;
using Aelena.FileApi.Cli.Commands;

var root = new RootCommand("fileapi — Document processing CLI powered by Aelena.FileApi.Core")
{
    PdfCommand.Create(),
    DocxCommand.Create(),
    ImageCommand.Create(),
    HashCommand.Create(),
    TxtCommand.Create(),
    ZipCommand.Create(),
    ReadabilityCommand.Create(),
    PiiCommand.Create(),
    EmailCommand.Create()
};

return await root.InvokeAsync(args);
