using CommandLine;
using System;
using System.Diagnostics;

namespace SdImg
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 0) args[0] = args[0].ToLowerInvariant();
            var parsedResult = Parser.Default.ParseArguments<WritePngInfoCommand, ReadPngInfoCommand>(args);
            var commonOptions = parsedResult.Tag == ParserResultType.NotParsed ? null : parsedResult.Value as CommandBase;
            if (commonOptions != null)
            {
                try
                {
                    ExitCode exitCode = commonOptions.Execute();
                    Environment.ExitCode = (int)exitCode;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"{parsedResult.TypeInfo.Current.Name} ExecuteError: {ex}");
                    Environment.ExitCode = (int)ExitCode.ExecuteError;
                }

                if (commonOptions.Pause)
                {
                    if (commonOptions.PauseMessage.Length != 0)
                    {
                        Console.Write(commonOptions.PauseMessage);
                    }
                    Console.ReadLine();
                }
            }
            else
            {
                Environment.ExitCode = (int)ExitCode.CommandParseError;
#if DEBUG
                Debugger.Break();
#endif
            }
        }

    }
}