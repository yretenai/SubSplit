using DragonLib.CLI;

namespace SubSplit
{
    public class SubSplitFlags : ICLIFlags
    {
        [CLIFlag("input", Help = "Input video file", IsRequired = true, Positional = 0)]
        public string InputFile { get; set; } = null!;

        [CLIFlag("output", Help = "Output directory to save split files into", IsRequired = true, Positional = 1)]
        public string OutputDirectory { get; set; } = null!;

        [CLIFlag("subtitle", Aliases = new[] { "s" }, Help = "Primary subtitle file to use", IsRequired = true)]
        public string PrimarySubtitleFile { get; set; } = null!;

        [CLIFlag("text-override", Aliases = new[] { "t" }, Help = "Override subtitle file to use")]
        public string? OverrideSubtitleFile { get; set; }

        [CLIFlag("sound", Aliases = new[] { "S" }, Help = "Split with sounds")]
        public bool ProcessSound { get; set; }

        [CLIFlag("merge", Aliases = new[] { "c" }, Help = "Try to merge lingering subtitles")]
        public bool Merge { get; set; }

        [CLIFlag("video-stream", Aliases = new[] { "v" }, Default = -1, Help = "Video Stream Index")]
        public int VideoIndex { get; set; }

        [CLIFlag("audio-stream", Aliases = new[] { "a" }, Default = -1, Help = "Audio Stream Index")]
        public int AudioIndex { get; set; }

        [CLIFlag("offset", Default = 0, Help = "Start offset in ms")]
        public int StartOffset { get; set; }

        [CLIFlag("duration", Default = 0, Help = "Duration offset in ms")]
        public int DurationOffset { get; set; }
    }
}
