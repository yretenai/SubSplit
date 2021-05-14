using DragonLib.CLI;
using DragonLib.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

namespace SubSplit
{
    public static class Program
    {
        public static SubSplitFlags? Flags { get; private set; }

        private static async Task<int> Main(string[] args)
        {
            Flags = CommandLineFlags.ParseFlags<SubSplitFlags>(args);

            if (Flags == null) return 1;

            if (!File.Exists(Flags.InputFile))
            {
                Logger.Error("Program", "Input file does not exist");
                return 4;
            }

            Logger.Info("Program", "Downloading ffmpeg");

            await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official);

            var probe = await FFmpeg.GetMediaInfo(Flags.InputFile);
            if (probe == null)
            {
                Logger.Error("FFmpeg", "Can't probe media file");
                return 2;
            }

            if (!File.Exists(Flags.PrimarySubtitleFile))
            {
                Logger.Error("Program", "Subtitle file missing");
                return 5;
            }

            var subtitles = SubtitleFile.GetSubtitles(await File.ReadAllLinesAsync(Flags.PrimarySubtitleFile), out var subtitleLeadIn);
            if (!File.Exists(Flags.OverrideSubtitleFile))
            {
                if (!string.IsNullOrEmpty(Flags.OverrideSubtitleFile)) Logger.Warn("Program", "Override Subtitle File does not exist");
            }
            else
            {
                subtitles = SubtitleFile.GetSubtitles(await File.ReadAllLinesAsync(Flags.OverrideSubtitleFile), out subtitleLeadIn).Select((t, index) => subtitles.ElementAt(index) with
                {
                    Text = t.Text,
                    OriginalText = t.OriginalText
                }).ToList();
            }

            var audioStream = Flags.ProcessSound ? probe.AudioStreams.FirstOrDefault(stream => stream.Index == Flags.AudioIndex || Flags.AudioIndex < 0 && stream.Default == 1) : default;
            var videoStream = probe.VideoStreams.FirstOrDefault(stream => stream.Index == Flags.VideoIndex || Flags.VideoIndex < 0 && stream.Default == 1);

            if (Flags.ProcessSound && audioStream == default)
            {
                Logger.Warn("Program", "Can't find suitable audio stream.");
                Flags.ProcessSound = false;
            }

            if (videoStream == default)
            {
                Logger.Error("Program", "Can't find video stream.");
                return 3;
            }

            return await SplitSubtitles(subtitles, subtitleLeadIn, videoStream, audioStream);
        }


        // TODO: Move to DragonLib.
        public static string GetValidFilename(string filename)
        {
            var invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            var invalidReStr = $@"[{invalidChars}]+";

            var reservedWords = new[]
            {
                "CON", "PRN", "AUX", "CLOCK$", "NUL", "COM0", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT0",
                "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
            };

            var sanitisedNamePart = Regex.Replace(filename.TrimEnd('.'), invalidReStr, "_");
            return reservedWords.Select(reservedWord => $"^{reservedWord}\\.").Aggregate(sanitisedNamePart, (current, reservedWordPattern) => Regex.Replace(current, reservedWordPattern, "_reservedWord_.", RegexOptions.IgnoreCase));
        }

        private static async Task<int> SplitSubtitles(IEnumerable<Subtitle> subtitles, string leadIn, IVideoStream videoStream, IAudioStream? audioStream)
        {
            if (!Directory.Exists(Flags!.OutputDirectory)) Directory.CreateDirectory(Flags.OutputDirectory);

            var durationOffset = TimeSpan.FromMilliseconds(Flags.DurationOffset);
            var startOffset = TimeSpan.FromMilliseconds(Flags.StartOffset);
            var fixedStart = TimeSpan.FromSeconds(2);
            var subtitlesAll = subtitles.ToArray();
            for (var index = 0; index < subtitlesAll.Length; index++)
            {
                var subtitleList = new List<Subtitle>
                {
                    subtitlesAll[index]
                };

                if (Flags.Merge)
                    for (var index2 = index + 1; index2 < subtitlesAll.Length; index2++)
                    {
                        var text = subtitlesAll[index2].Text.Trim(' ', '.', '-');
                        if (text[0] == text.ToLower()[0])
                        {
                            subtitleList.Add(subtitlesAll[index2]);
                        }
                        else
                        {
                            index = index2 - 1;
                            break;
                        }
                    }

                List<IStream> streamList = new();
                var duration = (subtitleList.Last().End - subtitleList.First().Start).Add(durationOffset);
                var start = subtitleList.First().Start.Subtract(startOffset) - fixedStart;
                streamList.Add(videoStream.Split(fixedStart, duration));
                if (audioStream != null) streamList.Add(audioStream.Split(fixedStart, duration));
                var name = $"{subtitleList.First().Id}_{GetValidFilename(subtitleList.First().Text)}";
                var oldEnd = subtitleList.First().Start;
                var newStart = TimeSpan.Zero;
                var newEnd = TimeSpan.Zero;
                for (var i = 0; i < subtitleList.Count; i++)
                {
                    var subtitle = subtitleList[i];
                    newEnd += subtitle.End - oldEnd;
                    if (i == subtitleList.Count - 1) newEnd += durationOffset;

                    if (i == 0) newEnd += startOffset;

                    subtitleList[i] = subtitle with
                    {
                        Start = newStart,
                        End = newStart + newEnd
                    };
                    newStart = newEnd;
                    oldEnd = subtitle.End;
                }

                var ext = subtitleList.First() is AdvancedSubtitle ? "ass" : "srt";
                await File.WriteAllTextAsync(Path.Combine(Flags.OutputDirectory, name + "." + ext), leadIn + string.Join(subtitleList.First() is AdvancedSubtitle ? "\n" : "\n\n", subtitleList));
                try
                {
                    Logger.Info("Program", string.Join(" ", subtitleList.Select(x => x.Text)).Replace("\n", " "));
                    await FFmpeg.Conversions.New().SetOverwriteOutput(true).AddStream(streamList).SetOutput(Path.Combine(Flags.OutputDirectory, name + ".mp4")).AddParameter($"-ss {start.ToFFmpeg()}", ParameterPosition.PreInput).Start();
                }
                catch (Exception e)
                {
                    Logger.Error("FFmpeg", e);
                }
            }

            return 0;
        }
    }
}
