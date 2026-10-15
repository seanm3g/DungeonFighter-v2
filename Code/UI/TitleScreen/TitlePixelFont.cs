using System.Collections.Generic;

namespace RPGGame.UI.TitleScreen
{
    /// <summary>
    /// 7-row chunky bitmap glyphs (2-pixel strokes) for the title words. Each '#' becomes a solid cell block on the canvas,
    /// so letters stay crisp and evenly spaced regardless of how the font draws box-drawing glyphs.
    /// </summary>
    public static class TitlePixelFont
    {
        public const int GlyphHeight = 7;

        private static readonly Dictionary<char, string[]> Glyphs = new()
        {
            ['D'] = new[]
            {
                "######.",
                "#######",
                "##...##",
                "##...##",
                "##...##",
                "#######",
                "######.",
            },
            ['E'] = new[]
            {
                "#######",
                "#######",
                "##.....",
                "######.",
                "##.....",
                "#######",
                "#######",
            },
            ['M'] = new[]
            {
                "##...##",
                "###.###",
                "#######",
                "##.#.##",
                "##...##",
                "##...##",
                "##...##",
            },
            ['O'] = new[]
            {
                ".#####.",
                "#######",
                "##...##",
                "##...##",
                "##...##",
                "#######",
                ".#####.",
            },
            ['N'] = new[]
            {
                "##...##",
                "###..##",
                "####.##",
                "##.####",
                "##..###",
                "##...##",
                "##...##",
            },
            ['F'] = new[]
            {
                "#######",
                "#######",
                "##.....",
                "######.",
                "##.....",
                "##.....",
                "##.....",
            },
            ['I'] = new[]
            {
                "######",
                "######",
                "..##..",
                "..##..",
                "..##..",
                "######",
                "######",
            },
            ['G'] = new[]
            {
                ".######",
                "#######",
                "##.....",
                "##.####",
                "##...##",
                "#######",
                ".#####.",
            },
            ['H'] = new[]
            {
                "##...##",
                "##...##",
                "##...##",
                "#######",
                "##...##",
                "##...##",
                "##...##",
            },
            ['T'] = new[]
            {
                "######",
                "######",
                "..##..",
                "..##..",
                "..##..",
                "..##..",
                "..##..",
            },
            ['R'] = new[]
            {
                "######.",
                "#######",
                "##...##",
                "######.",
                "##.##..",
                "##..##.",
                "##...##",
            },
        };

        /// <summary>Glyph rows for <paramref name="c"/>; blank 3-wide cell for unknown characters.</summary>
        public static string[] Get(char c) =>
            Glyphs.TryGetValue(char.ToUpperInvariant(c), out var rows)
                ? rows
                : new[] { "...", "...", "...", "...", "...", "...", "..." };

        /// <summary>Width of a word in font pixels with <paramref name="gapPx"/> blank columns between letters.</summary>
        public static int MeasureWord(string word, int gapPx)
        {
            int width = 0;
            for (int i = 0; i < word.Length; i++)
            {
                width += Get(word[i])[0].Length;
                if (i < word.Length - 1)
                    width += gapPx;
            }
            return width;
        }
    }
}
