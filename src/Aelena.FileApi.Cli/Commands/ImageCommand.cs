using System.CommandLine;
using Aelena.FileApi.Cli.Helpers;
using Aelena.FileApi.Core.Services.Image;

namespace Aelena.FileApi.Cli.Commands;

public static class ImageCommand
{
    public static Command Create()
    {
        var cmd = new Command("image", "Image operations — resize, rotate, crop, convert, blur, etc.");
        cmd.AddCommand(Exif());
        cmd.AddCommand(Resize());
        cmd.AddCommand(Rotate());
        cmd.AddCommand(Convert());
        cmd.AddCommand(Grayscale());
        cmd.AddCommand(Blur());
        cmd.AddCommand(Compress());
        return cmd;
    }

    private static Command Exif()
    {
        var fileArg = new Argument<FileInfo>("file", "Image file");
        var cmd = new Command("exif", "Extract EXIF metadata") { fileArg };
        cmd.SetHandler(file =>
        {
            var data = Output.ReadFileWithSpinner(file.FullName);
            var e = ImageService.GetExif(data, file.Name);
            Output.Properties($"EXIF: {file.Name}",
                ("Format", e.Format), ("Width", e.Width?.ToString()), ("Height", e.Height?.ToString()),
                ("Mode", e.Mode), ("Size", $"{e.FileSizeBytes:N0} bytes"));
            if (e.Exif is { Count: > 0 })
                Output.Properties("EXIF Tags", e.Exif.Select(kv => (kv.Key, (string?)kv.Value)).ToArray());
        }, fileArg);
        return cmd;
    }

    private static Command Resize()
    {
        var fileArg = new Argument<FileInfo>("file", "Image file");
        var wOpt = new Option<int?>("-w", "Target width");
        var hOpt = new Option<int?>("-h", "Target height");
        var outOpt = new Option<string?>("-o", "Output file");
        var cmd = new Command("resize", "Resize an image") { fileArg, wOpt, hOpt, outOpt };
        cmd.SetHandler((file, w, h, o) =>
        {
            var data = Output.ReadFileWithSpinner(file.FullName);
            var (name, bytes, _) = ImageService.Resize(data, file.Name, w, h);
            Output.WriteFile(o ?? name, bytes);
        }, fileArg, wOpt, hOpt, outOpt);
        return cmd;
    }

    private static Command Rotate()
    {
        var fileArg = new Argument<FileInfo>("file", "Image file");
        var angleOpt = new Option<float>("--angle", "Rotation angle in degrees") { IsRequired = true };
        var outOpt = new Option<string?>("-o", "Output file");
        var cmd = new Command("rotate", "Rotate an image") { fileArg, angleOpt, outOpt };
        cmd.SetHandler((file, angle, o) =>
        {
            var data = Output.ReadFileWithSpinner(file.FullName);
            var (name, bytes, _) = ImageService.Rotate(data, file.Name, angle);
            Output.WriteFile(o ?? name, bytes);
        }, fileArg, angleOpt, outOpt);
        return cmd;
    }

    private static Command Convert()
    {
        var fileArg = new Argument<FileInfo>("file", "Image file");
        var fmtOpt = new Option<string>("--format", "Target format: png, jpeg, webp, bmp, gif, tiff") { IsRequired = true };
        var outOpt = new Option<string?>("-o", "Output file");
        var cmd = new Command("convert", "Convert image format") { fileArg, fmtOpt, outOpt };
        cmd.SetHandler((file, fmt, o) =>
        {
            var data = Output.ReadFileWithSpinner(file.FullName);
            var (name, bytes, _) = ImageService.Convert(data, file.Name, fmt);
            Output.WriteFile(o ?? name, bytes);
        }, fileArg, fmtOpt, outOpt);
        return cmd;
    }

    private static Command Grayscale()
    {
        var fileArg = new Argument<FileInfo>("file", "Image file");
        var outOpt = new Option<string?>("-o", "Output file");
        var cmd = new Command("grayscale", "Convert to grayscale") { fileArg, outOpt };
        cmd.SetHandler((file, o) =>
        {
            var data = Output.ReadFileWithSpinner(file.FullName);
            var (name, bytes, _) = ImageService.Grayscale(data, file.Name);
            Output.WriteFile(o ?? name, bytes);
        }, fileArg, outOpt);
        return cmd;
    }

    private static Command Blur()
    {
        var fileArg = new Argument<FileInfo>("file", "Image file");
        var radiusOpt = new Option<float>("--radius", () => 2f, "Blur radius");
        var outOpt = new Option<string?>("-o", "Output file");
        var cmd = new Command("blur", "Apply Gaussian blur") { fileArg, radiusOpt, outOpt };
        cmd.SetHandler((file, radius, o) =>
        {
            var data = Output.ReadFileWithSpinner(file.FullName);
            var (name, bytes, _) = ImageService.Blur(data, file.Name, radius);
            Output.WriteFile(o ?? name, bytes);
        }, fileArg, radiusOpt, outOpt);
        return cmd;
    }

    private static Command Compress()
    {
        var fileArg = new Argument<FileInfo>("file", "Image file");
        var qualOpt = new Option<int>("--quality", () => 85, "JPEG quality 1-100");
        var outOpt = new Option<string?>("-o", "Output file");
        var cmd = new Command("compress", "Compress as JPEG") { fileArg, qualOpt, outOpt };
        cmd.SetHandler((file, quality, o) =>
        {
            var data = Output.ReadFileWithSpinner(file.FullName);
            var (name, bytes, _) = ImageService.Compress(data, file.Name, quality);
            Output.WriteFile(o ?? name, bytes);
        }, fileArg, qualOpt, outOpt);
        return cmd;
    }
}
