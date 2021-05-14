using System;

namespace SubSplit
{
    public record Subtitle(int Id, TimeSpan Start, TimeSpan End, string Text, string OriginalText)
    {
        public override string ToString() => $"{Id}\n{Start:hh\\:mm\\:ss\\,fff} --> {End:hh\\:mm\\:ss\\,fff}\n{Text}";
    }

    public record AdvancedSubtitle(int Id, TimeSpan Start, TimeSpan End, string Text, string OriginalText, string Channel, int Layer, string Style, SubtitleMargins Margins) : Subtitle(Id, Start, End, Text, OriginalText)
    {
        public string? Name { get; init; }
        public string? Effect { get; init; }

        public override string ToString() => $"{Channel}: {Layer},{Start:h\\:mm\\:ss\\.ff},{End:h\\:mm\\:ss\\.ff},{Style},{Name ?? ""},{Margins},{Effect ?? ""},{OriginalText}";
    }

    public record SubtitleMargins(int Left, int Right, int Top)
    {
        public override string ToString() => $"{Left},{Right},{Top}";
    }
}
