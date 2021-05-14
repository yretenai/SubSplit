using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SubSplit
{
    public static class SubtitleFile
    {
        public static IEnumerable<Subtitle> GetSubtitles(IEnumerable<string> lines, out string leadIn)
        {
            var linesArray = lines as string[] ?? lines.ToArray();
            var subtitles = new List<Subtitle>();
            leadIn = "";
            if (linesArray.ElementAt(0) == "[Script Info]")
            {
                var state = 0;
                foreach (var line in linesArray)
                {
                    switch (state)
                    {
                        case 1:
                            leadIn += line + "\n";
                            // TODO: parse / validate indices.
                            state = 2;
                            break;
                        case 2 when line.Trim().Length == 0:
                            continue;
                        case 0:
                        case 2 when line.Trim().StartsWith("["):
                        {
                            leadIn += line + "\n";
                            state = line.Trim() == "[Events]" ? 1 : 0;
                            break;
                        }
                        case 2:
                        {
                            var layerParts = line.Split(":", 2, StringSplitOptions.TrimEntries);
                            var subtitleParts = layerParts[1].Split(",", 10, StringSplitOptions.TrimEntries);

                            var original = subtitleParts[9];
                            var stripped = "";

                            var insideTag = false;
                            for (var i = 0; i < original.Length; ++i)
                            {
                                switch (original[i])
                                {
                                    case '}':
                                        insideTag = false;
                                        continue;
                                    case '{':
                                        insideTag = true;
                                        continue;
                                    case '\\':
                                    {
                                        if (!insideTag && i + 1 < original.Length)
                                            switch (original[++i])
                                            {
                                                default:
                                                    stripped += original[i];
                                                    break;
                                                case 'T':
                                                case 't':
                                                    stripped += '\t';
                                                    break;
                                                case 'N':
                                                case 'n':
                                                    stripped += '\n';
                                                    break;
                                                case 'R':
                                                case 'r':
                                                    stripped += '\r';
                                                    break;
                                            }

                                        continue;
                                    }
                                }

                                if (!insideTag) stripped += original[i];
                            }

                            var margins = new SubtitleMargins(int.Parse(subtitleParts[5]), int.Parse(subtitleParts[6]), int.Parse(subtitleParts[7]));
                            var subtitle = new AdvancedSubtitle(subtitles.Count + 1, TimeSpan.ParseExact(subtitleParts[1], @"h\:mm\:ss\.ff", null), TimeSpan.ParseExact(subtitleParts[2], @"h\:mm\:ss\.ff", null), stripped, original, layerParts[0], int.Parse(subtitleParts[0]), subtitleParts[3], margins)
                            {
                                Name = !string.IsNullOrEmpty(subtitleParts[4]) ? subtitleParts[4] : null,
                                Effect = !string.IsNullOrEmpty(subtitleParts[8]) ? subtitleParts[8] : null
                            };
                            subtitles.Add(subtitle);
                            break;
                        }
                    }
                }
            }
            else
            {
                var id = -1;
                var start = TimeSpan.MinValue;
                var end = TimeSpan.MinValue;
                var text = new List<string>();
                foreach (var line in linesArray)
                {
                    if (line.Trim().Length == 0)
                    {
                        if (id > -1)
                        {
                            var subtitle = string.Join("\n", text);
                            subtitles.Add(new Subtitle(id, start, end, subtitle, subtitle));
                        }

                        id = -1;
                        start = TimeSpan.MinValue;
                        end = TimeSpan.MinValue;

                        continue;
                    }

                    if (id == -1)
                    {
                        id = int.Parse(line, NumberStyles.Number);
                    }
                    else if (start == TimeSpan.MinValue)
                    {
                        var parts = line.Split("-->", StringSplitOptions.TrimEntries);
                        start = TimeSpan.ParseExact(parts[0], @"hh\:mm\:ss\,fff", null);
                        end = TimeSpan.ParseExact(parts[1], @"hh\:mm\:ss\,fff", null);
                    }
                    else
                    {
                        text.Add(line);
                    }
                }
            }

            return subtitles;
        }
    }
}
