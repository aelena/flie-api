using System.CommandLine;
using Aelena.FileApi.Cli.Helpers;
using Aelena.FileApi.Core.Services.Image;

namespace Aelena.FileApi.Cli.Commands;

/// <summary><c>fileapi image</c> — resize, rotate, convert, and filter images.</summary>
public static class ImageCommand
{
    public static Command Create()
    {
        return new Command("image", "Image operations — resize, rotate, crop, convert, blur, etc.")
        {
            Exif(), Resize(), Rotate(), Convert(), Grayscale(), Blur(), Compress()
        };
    }

    private static Command Exif()
    {
        var fileArg = CommandExtensions.FileArgument("Image file");
        var cmd = new Command("exif", "Extract EXIF metadata") { fileArg };

        return cmd.WithAction(parse =>
        {
            var file = parse.GetRequiredValue(fileArg);
            var exif = ImageService.GetExif(Output.ReadFile(file), file.Name);

            Output.Properties($"EXIF: {file.Name}",
                ("Format", exif.Format),
                ("Width", exif.Width?.Display()),
                ("Height", exif.Height?.Display()),
                ("Mode", exif.Mode),
                ("Size", $"{exif.FileSizeBytes:N0} bytes"));

            if (exif.Exif is { Count: > 0 })
                Output.Properties("EXIF Tags", [.. exif.Exif.Select(kv => (kv.Key, (string?)kv.Value))]);
        });
    }

    private static Command Resize()
    {
        var fileArg = CommandExtensions.FileArgument("Image file");

        // "-h" is reserved for --help, so height takes the long form only. The old
        // "-h 100" was silently swallowed by the help option instead of resizing.
        var widthOpt = new Option<int?>("--width", "-w") { Description = "Target width in pixels" };
        var heightOpt = new Option<int?>("--height") { Description = "Target height in pixels" };
        var outOpt = CommandExtensions.OutputOption();

        var cmd = new Command("resize", "Resize an image, preserving aspect ratio when only one side is given")
            { fileArg, widthOpt, heightOpt, outOpt };

        return cmd.WithAction(parse =>
        {
            var file = parse.GetRequiredValue(fileArg);
            var (name, bytes, _) = ImageService.Resize(
                Output.ReadFile(file), file.Name,
                parse.GetValue(widthOpt), parse.GetValue(heightOpt));

            Output.WriteFile(parse.GetValue(outOpt) ?? name, bytes);
        });
    }

    private static Command Rotate()
    {
        var fileArg = CommandExtensions.FileArgument("Image file");
        var angleOpt = new Option<float>("--angle")
        {
            Description = "Rotation angle in degrees, clockwise",
            Required = true
        };
        var outOpt = CommandExtensions.OutputOption();
        var cmd = new Command("rotate", "Rotate an image") { fileArg, angleOpt, outOpt };

        return cmd.WithAction(parse =>
        {
            var file = parse.GetRequiredValue(fileArg);
            var (name, bytes, _) = ImageService.Rotate(
                Output.ReadFile(file), file.Name, parse.GetRequiredValue(angleOpt));

            Output.WriteFile(parse.GetValue(outOpt) ?? name, bytes);
        });
    }

    private static Command Convert()
    {
        var fileArg = CommandExtensions.FileArgument("Image file");
        var formatOpt = new Option<string>("--format")
        {
            Description = "Target format",
            Required = true
        }.AcceptOnlyFromAmong("png", "jpeg", "jpg", "webp", "bmp", "gif", "tiff");
        var outOpt = CommandExtensions.OutputOption();
        var cmd = new Command("convert", "Convert image format") { fileArg, formatOpt, outOpt };

        return cmd.WithAction(parse =>
        {
            var file = parse.GetRequiredValue(fileArg);
            var (name, bytes, _) = ImageService.Convert(
                Output.ReadFile(file), file.Name, parse.GetRequiredValue(formatOpt));

            Output.WriteFile(parse.GetValue(outOpt) ?? name, bytes);
        });
    }

    private static Command Grayscale()
    {
        var fileArg = CommandExtensions.FileArgument("Image file");
        var outOpt = CommandExtensions.OutputOption();
        var cmd = new Command("grayscale", "Convert to grayscale") { fileArg, outOpt };

        return cmd.WithAction(parse =>
        {
            var file = parse.GetRequiredValue(fileArg);
            var (name, bytes, _) = ImageService.Grayscale(Output.ReadFile(file), file.Name);
            Output.WriteFile(parse.GetValue(outOpt) ?? name, bytes);
        });
    }

    private static Command Blur()
    {
        var fileArg = CommandExtensions.FileArgument("Image file");
        var radiusOpt = new Option<float>("--radius")
        {
            Description = "Blur radius in pixels",
            DefaultValueFactory = _ => 2f
        };
        var outOpt = CommandExtensions.OutputOption();
        var cmd = new Command("blur", "Apply Gaussian blur") { fileArg, radiusOpt, outOpt };

        return cmd.WithAction(parse =>
        {
            var file = parse.GetRequiredValue(fileArg);
            var (name, bytes, _) = ImageService.Blur(
                Output.ReadFile(file), file.Name, parse.GetRequiredValue(radiusOpt));

            Output.WriteFile(parse.GetValue(outOpt) ?? name, bytes);
        });
    }

    private static Command Compress()
    {
        var fileArg = CommandExtensions.FileArgument("Image file");
        var qualityOpt = new Option<int>("--quality")
        {
            Description = "JPEG quality, 1 (smallest) to 100 (best)",
            DefaultValueFactory = _ => 85
        };
        var outOpt = CommandExtensions.OutputOption();
        var cmd = new Command("compress", "Compress as JPEG") { fileArg, qualityOpt, outOpt };

        return cmd.WithAction(parse =>
        {
            var file = parse.GetRequiredValue(fileArg);
            var (name, bytes, _) = ImageService.Compress(
                Output.ReadFile(file), file.Name, parse.GetRequiredValue(qualityOpt));

            Output.WriteFile(parse.GetValue(outOpt) ?? name, bytes);
        });
    }
}
