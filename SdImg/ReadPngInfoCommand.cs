using CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace SdImg
{
    [Verb("readpnginfo",
        aliases: new string[] { "read" },
        HelpText = "画像から PNG Info を読み取る")]
    class ReadPngInfoCommand : CommandBase
    {
        [Value(-1, Min = 1, Required = true, MetaName = "オプションなしの引数はすべて入力する画像ファイルのパスとして扱う (複数指定可、1つ以上の指定が必須)。 対応画像は PNG と JPEG、非対応の画像は無視します")]
        public IEnumerable<string> InputImagePaths { get; protected set; }

        [Option('o', "output", Required = false, HelpText = @"出力先ファイルパス。BOMなしUTF8で template に従って出力する。
特殊な値 ""?temporary-{SUFFIX}"": 一時ファイルに保存する。openオプションと組み合わせて使う。 {SUFFIX} はファイル名の末尾に付くので自由に書き換えてください")]
        public string DestTextFilePath { get; protected set; }

        [Option("overwrite", Required = false, HelpText = "指定する (値は不要) と、ファイル保存時に同名のファイルがある場合は上書きする。")]
        public bool Overwrite { get; protected set; }

        [Option('t', "template", Required = false, HelpText = @"出力テンプレートのファイルパスを指定する。
テンプレートファイルの中身は {0}=ファイルパス {1}=PNG Info をプレースホルダーとして利用可能。
既定のテンプレートは ""{0}\n---\n{1}\n""")]
        public string TemplateFilePath { get; protected set; }

        [Option("open", Required = false, HelpText = "指定する (値は不要) と、出力したファイルを実行後に開く")]
        public bool Open { get; protected set; }

        public override ExitCode Execute() => ReadPngInfo.Impl(this);
    }

    class ReadPngInfo
    {
        public static readonly SdUtil SdUtil = new SdUtil();
        public static ExitCode Impl(ReadPngInfoCommand cmd)
        {
            var template = string.IsNullOrWhiteSpace(cmd.TemplateFilePath)
                ? "{0}\n---\n{1}\n"
                : File.ReadAllText(cmd.TemplateFilePath);
            try
            {
                string.Format(template, "FileName", "PngInfo"); // 例外が発生しないかチェック
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Invalid Template: {cmd.TemplateFilePath}", ex);
            }

            var pngInfoList = new List<string>();
            
            foreach (var imgPath in cmd.InputImagePaths)
            {
                try
                {
                    var pngInfo = SdUtil.GetPngInfo(imgPath, null, null, null);
                    if (pngInfo != null)
                    {
                        pngInfoList.Add(string.Format(template, imgPath, pngInfo));
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"GetPngInfo Error: {ex}");
                }
            }

            if (pngInfoList.Count == 0)
            {
                Console.Error.WriteLine("Error: PNG Info not found.");
                return ExitCode.ReadPngInfo_NoPngInfo;
            }

            var pngInfoChunk = string.Join(Environment.NewLine, pngInfoList);

            Console.WriteLine(pngInfoChunk);

            if (cmd.DestTextFilePath != null)
            {
                var destPath = WriteFile(cmd.DestTextFilePath, pngInfoChunk, cmd.Overwrite);
                if (destPath != null)
                {
                    if (cmd.Open)
                    {
                        using (var p = Process.Start("explorer", $"\"{destPath}\""))
                        {
                            p.WaitForExit();
                        }
                    }
                }
            }

            return ExitCode.Success;
        }

        private static string WriteFile(string dest, string content, bool overwrite)
        {
            if (string.IsNullOrEmpty(dest)) throw new ArgumentException("required", nameof(dest));
            if (string.IsNullOrEmpty(content)) throw new ArgumentException("required", nameof(content));

            string ret;
            try
            {
                const string TempPrefix = "?temporary-";
                if (dest.StartsWith(TempPrefix))
                {
                    var suffix = dest == TempPrefix ? "" : dest.Substring(TempPrefix.Length);
                    using (var fs = SdUtil.GetTempFile(6, suffix))
                    using (var sw = new StreamWriter(fs, new UTF8Encoding(false)))
                    {
                        ret = fs.Name;
                        sw.Write(string.Join(Environment.NewLine, content));
                    }
                }
                else
                {
                    using (var fs = new FileStream(dest, overwrite ? FileMode.Create : FileMode.CreateNew))
                    using (var sw = new StreamWriter(fs, new UTF8Encoding(false)))
                    {
                        ret = fs.Name;
                        sw.Write(string.Join(Environment.NewLine, content));
                    }
                }
                return ret;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Failed to write PNG Info. " + ex);
                return null;
            }
        }
    }
}