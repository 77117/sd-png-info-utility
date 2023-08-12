using CommandLine;
using System;
using System.IO;

namespace SdImg
{

    [Verb("writepnginfo",
        aliases: new string[] { "write" },
        HelpText = "画像にPNG Info を書き込む。画像編集は ImageMagick などと連携してください。")]
    class WritePngInfoCommand : CommandBase
    {
        [Option('i', "input", Required = true, HelpText = "入力する画像のファイルパス")]
        public string InputImagePath { get; protected set; }

        [Option('p', "pnginfo", Required = true, HelpText = @"書き込みたい PNG Info が書かれたテキストファイルのファイルパスを指定する。
特殊な値 ""?empty"" : PNG Info を空にする。")]
        public string InputPngInfoPath { get; protected set; }

        [Option('o', "output", Required = true, HelpText = "出力先ファイルパス。拡張子が png, jpg のいずれかであること。")]
        public string DestImagePath { get; protected set; }

        [Option("overwrite", Required = false, HelpText = "指定する (値は不要) と、ファイル保存時に同名のファイルがある場合は上書きする。")]
        public bool Overwrite { get; protected set; }

        [Option('q', "quality", Required = false, HelpText = "jpg 保存時の quality (1-100)。 指定しない場合は 75")]
        public int Quality { get; protected set; } = 75;

        // 未実装: resize 50% WxH
        public override ExitCode Execute() => WritePngInfo.Impl(this);
    }

    class WritePngInfo
    {
        public static ExitCode Impl(WritePngInfoCommand cmd)
        {
            var ext = Path.GetExtension(cmd.DestImagePath).ToUpperInvariant();

            if (ext == ".PNG")
            {
                WritePngInfoAsPng(cmd);
            }
            else if (ext == ".JPG" || ext == ".JPEG")
            {
                WritePngInfoAsJpeg(cmd);
            }
            else
            {
                throw new ArgumentException($"{ext} is unsupported format. output={cmd.DestImagePath}");
            }

            return ExitCode.Success;
        }
        public static void WritePngInfoAsPng(WritePngInfoCommand cmd)
        {
            // inputPath のデータが jpeg のときに、保存した PNG Info が SD で読み込めなかった
            // ため、 一度 bmp にしてから PNG Info を書き込んで回避した
            var pngInfo = File.ReadAllText(cmd.InputPngInfoPath);
            var bytes = new SdUtil().LoadImageAsBitmapBytes(cmd.InputImagePath);
            new SdUtil().SaveAsPng(bytes, cmd.DestImagePath, pngInfo, cmd.Overwrite);
        }
        public static void WritePngInfoAsJpeg(WritePngInfoCommand cmd)
        {
            if (cmd.Quality < 1 || 100 < cmd.Quality) throw new ArgumentException($"Invalid quality: {cmd.Quality}");

            var pngInfo = File.ReadAllText(cmd.InputPngInfoPath);
            using (var bitmap = System.Drawing.Bitmap.FromFile(cmd.InputImagePath) as System.Drawing.Bitmap)
            {
                new SdUtil().SaveBitmapAsJpeg(bitmap, cmd.DestImagePath, pngInfo, cmd.Quality, cmd.Overwrite);
            }
        }
    }
}