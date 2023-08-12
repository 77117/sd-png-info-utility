using CommandLine;

namespace SdImg
{
    abstract class CommandBase
    {
        [Option("pause", Required = false, HelpText = "指定する (値は不要) と、実行後に pause-message があれば標準出力し、Enter を押さないと終了しない")]
        public bool Pause { get; protected set; }

        [Option("pause-message", Required = false, HelpText = "pause時に標準出力するテキスト")]
        public string PauseMessage { get; protected set; } = string.Empty;

        /// <summary>
        /// 実装
        /// </summary>
        /// <returns>ExitCode</returns>
        abstract public ExitCode Execute();
    }

    public enum ExitCode : int
    {
        Success = 0,

        ReadPngInfo_NoPngInfo = 100001,

        ExecuteError = 999001,
        CommandParseError = 999002
    }
}
