using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.UI.Avalonia.Feedback
{
    /// <summary>
    /// Paints <see cref="CombatTurnAnnouncement"/> as a box centered on the center panel, on the character grid: a
    /// double-line frame holds the actor's name in block letters ("PIP'S TURN", "THE ROOM ACTS") with the turn number
    /// under it. The box unrolls from its center line as the turn changes hands and rolls back up just before the
    /// announcement ends. A name too wide for block
    /// letters is written in plain text instead. On the combat board the box instead grows out of the NOW card to cover
    /// the LAST / NOW / NEXT cards, then shrinks back onto NOW. A kill callout reads VICTORY over the fallen enemy's name.
    /// The canvas paints it after everything else.
    /// </summary>
    public static class CombatTurnBanner
    {
        /// <summary>Time the box takes to open, and to close at the end.</summary>
        private const int SnapMs = 140;

        /// <summary>On the board: time the box takes to grow out of NOW over the cards, and to shrink back at the end.</summary>
        private const int SpreadMs = 260;

        private static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(16);
        private static DispatcherTimer? _timer;

        private static readonly Color BoxFill = Color.FromRgb(10, 10, 10);
        private static readonly Color HeroColor = Color.FromRgb(255, 200, 40);
        private static readonly Color EnemyColor = Color.FromRgb(255, 60, 60);
        private static readonly Color RoomColor = Color.FromRgb(200, 140, 255);

        private static System.Action? _refresh;

        /// <summary>Repaints the canvas; called on the UI thread.</summary>
        public static void SetRefresh(System.Action? refresh)
        {
            _refresh = refresh;
            CombatTurnAnnouncement.SetRequestInvalidate(refresh == null ? null : () => Dispatcher.UIThread.Post(() => _refresh?.Invoke()));
        }

        /// <summary>0..1: opens over the first <see cref="SnapMs"/>, holds, closes over the last.</summary>
        private static double Openness(CombatTurnAnnouncement.Callout call)
        {
            double elapsed = (DateTimeOffset.UtcNow - call.StartedUtc).TotalMilliseconds;
            double opening = elapsed / SnapMs;
            double closing = (call.DurationMs - elapsed) / SnapMs;
            return CombatBoardAnimation.EaseOut(Math.Clamp(Math.Min(opening, closing), 0.0, 1.0));
        }

        /// <summary>Repaints every frame while the callout is up; stops once it is gone.</summary>
        private static void EnsureTimer()
        {
            if (_refresh == null)
                return;
            if (_timer == null)
            {
                _timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = FrameInterval };
                _timer.Tick += (_, _) =>
                {
                    _refresh?.Invoke();
                    // Runs through the hold too, so the close starts on time.
                    if (CombatTurnAnnouncement.Current == null)
                        _timer!.Stop();
                };
            }
            if (!_timer.IsEnabled)
                _timer.Start();
        }

        internal static string Headline(CombatTurnAnnouncement.Callout call) =>
            call.Defeated ? "VICTORY" : Headline(call.Actor);

        internal static string Headline(Actor actor)
        {
            if (actor is Environment)
                return "THE ROOM ACTS";
            string name = actor.Name.ToUpperInvariant();
            return name.EndsWith('S') ? $"{name}' TURN" : $"{name}'S TURN";
        }

        private static Color ActorColor(Actor actor) => actor switch
        {
            Environment => RoomColor,
            Enemy => EnemyColor,
            _ => HeroColor
        };

        /// <summary>Paints over the center panel: the combat board above the hero strip (the whole column off the board).</summary>
        public static void Paint(DrawingContext context, double contentHeight, double charWidth, double charHeight, double fontSize)
        {
            if (CombatTurnAnnouncement.Current is not { } call || charWidth <= 0 || charHeight <= 0)
                return;
            int panelX = LayoutConstants.CENTER_PANEL_X;
            int panelW = LayoutConstants.CENTER_PANEL_WIDTH;
            int panelH = LayoutConstants.ACTION_INFO_Y > 0 ? LayoutConstants.ACTION_INFO_Y : (int)(contentHeight / charHeight);
            if (panelW < 12 || panelH < 5)
                return;

            var grid = new Grid(context, charWidth, charHeight, fontSize);
            Color color = call.Defeated ? HeroColor : ActorColor(call.Actor);
            string headline = Headline(call);
            string turnLine = call.Defeated
                ? $"-  {call.Actor.Name.ToUpperInvariant()} DEFEATED  -"
                : $"-  TURN {call.TurnNumber}  -";
            var block = BlockFont.Render(headline);
            int blockW = block.Count > 0 ? block[0].Length : 0;

            // On the board the box grows out of the NOW card until it covers LAST, NOW and NEXT, holds there while the
            // turn is announced, then shrinks back onto NOW.
            if (CombatBoardLayout.IsActive && CombatBoardLayout.NowRect is { } now && CombatBoardLayout.FlowCardsRect is { } flow)
            {
                double spread = Spread(call);
                if (spread < 1.0)
                    EnsureTimer();
                int Mix(int a, int b) => CombatBoardAnimation.Lerp(a, b, spread);
                int fx = Mix(now.X, flow.X), fy = Mix(now.Y, flow.Y);
                int fw = Mix(now.X + now.W, flow.X + flow.W) - fx, fh = Mix(now.Y + now.H, flow.Y + flow.H) - fy;
                if (fw >= 4 && fh >= 3)
                    DrawBox(grid, fx, fy, fw, fh, color, call, headline, turnLine);
                return;
            }

            double open = Openness(call);
            if (open < 1.0)
                EnsureTimer();
            if (open <= 0.0)
                return;

            bool big = blockW + 8 <= panelW && BlockFont.Height + 8 <= panelH;
            int innerW = Math.Min(panelW - 4, Math.Max(big ? blockW + 2 : headline.Length, turnLine.Length) + 6);
            int innerH = big ? BlockFont.Height + 4 : 5;
            int boxW = innerW + 2, boxH = innerH + 2;
            int bx = panelX + (panelW - boxW) / 2;
            int by = Math.Max(0, (panelH - boxH) / 2);

            // Opening and closing, only the middle rows show, whole rows at a time, as if the box unrolls from its
            // center line; a one-row slit in the actor's color stands in for the box before it has room.
            int shown = Math.Max(1, (int)Math.Round(boxH * open));
            if (shown < 3)
            {
                grid.Text(bx, by + boxH / 2, new string('═', boxW), color);
                return;
            }
            int top = by + (boxH - shown) / 2;
            using var clip = context.PushClip(new Rect(bx * charWidth, top * charHeight, boxW * charWidth, shown * charHeight));
            DrawBox(grid, bx, by, boxW, boxH, color, call, headline, turnLine);
        }

        /// <summary>
        /// 0..1 on the board: grows from NOW (0) to the full card area (1) over the first <see cref="SpreadMs"/>, holds,
        /// shrinks back over the last.
        /// </summary>
        private static double Spread(CombatTurnAnnouncement.Callout call)
        {
            double elapsed = (DateTimeOffset.UtcNow - call.StartedUtc).TotalMilliseconds;
            int ms = Math.Min(SpreadMs, call.DurationMs / 3);
            double growing = elapsed / ms;
            double shrinking = (call.DurationMs - elapsed) / ms;
            return CombatBoardAnimation.EaseOut(Math.Clamp(Math.Min(growing, shrinking), 0.0, 1.0));
        }

        /// <summary>A turn callout on the board has finished growing, so LAST / NOW / NEXT are hidden behind it.</summary>
        internal static bool CoversBoard(CombatTurnAnnouncement.Callout call) =>
            !call.Defeated && (DateTimeOffset.UtcNow - call.StartedUtc).TotalMilliseconds >= Math.Min(SpreadMs, call.DurationMs / 3);

        /// <summary>
        /// Double-line frame at (<paramref name="bx"/>, <paramref name="by"/>) with the headline centered in it: block
        /// letters when they fit, plain text when only that fits, and the turn line under it when there is room.
        /// </summary>
        private static void DrawBox(Grid grid, int bx, int by, int boxW, int boxH, Color color, CombatTurnAnnouncement.Callout call,
            string headline, string turnLine)
        {
            int innerW = boxW - 2, innerH = boxH - 2;
            grid.Fill(bx, by, boxW, boxH, BoxFill);
            grid.Text(bx, by, "╔" + new string('═', innerW) + "╗", color);
            for (int r = 1; r <= innerH; r++)
            {
                grid.Text(bx, by + r, "║", color);
                grid.Text(bx + boxW - 1, by + r, "║", color);
            }
            grid.Text(bx, by + boxH - 1, "╚" + new string('═', innerW) + "╝", color);

            bool withTurnLine = turnLine.Length <= innerW && innerH >= 3;
            int turnRows = withTurnLine ? 2 : 0;
            // Block letters scaled as large as the box allows, the headline wrapped onto up to three lines when that
            // lets them grow; one pad row and column inside the frame, and the turn line kept under them.
            var (lines, scale) = FitBlockLines(headline, innerW - 2, innerH - 2 - turnRows);
            bool big = scale * BlockFont.Height >= MinBlockRows;
            bool plain = !big && headline.Length <= innerW - 2 && innerH >= 1;
            if (!big && !plain)
                return;

            double blockRows = big ? (lines.Count * (BlockFont.Height + LineGap) - LineGap) * scale : 1;
            double cy = by + 1 + Math.Max(0.0, (innerH - blockRows - turnRows) / 2.0);
            if (big)
            {
                Color shadow = Color.FromRgb((byte)(color.R / 4), (byte)(color.G / 4), (byte)(color.B / 4));
                foreach (var line in lines)
                {
                    int lineW = line.Count > 0 ? line[0].Length : 0;
                    double lx = bx + (boxW - lineW * scale) / 2.0;
                    // Shadow first, offset down and right by a third of a letter cell, then the letters over it.
                    double off = Math.Max(0.15, scale / 3.0);
                    for (int pass = 0; pass < 2; pass++)
                    {
                        double d = pass == 0 ? off : 0.0;
                        for (int r = 0; r < line.Count; r++)
                        {
                            // One fill per horizontal run of '#' keeps the letters seamless at fractional sizes.
                            for (int c = 0; c < line[r].Length; c++)
                            {
                                if (line[r][c] != '#')
                                    continue;
                                int end = c;
                                while (end + 1 < line[r].Length && line[r][end + 1] == '#')
                                    end++;
                                grid.FillCells(lx + c * scale + d, cy + r * scale + d, (end - c + 1) * scale, scale,
                                    pass == 0 ? shadow : color);
                                c = end;
                            }
                        }
                    }
                    cy += (BlockFont.Height + LineGap) * scale;
                }
                cy -= LineGap * scale;
            }
            else
            {
                grid.Text(bx + (boxW - headline.Length) / 2, (int)Math.Round(cy), headline, color);
                cy += 1;
            }

            if (withTurnLine)
                grid.Text(bx + (boxW - turnLine.Length) / 2, Math.Min(by + boxH - 2, (int)Math.Ceiling(cy) + 1), turnLine,
                    call.Defeated ? EnemyColor : AsciiArtAssets.Colors.Gray);
        }

        /// <summary>Block letters shorter than this many text rows read worse than plain text.</summary>
        private const double MinBlockRows = 1.5;

        /// <summary>Blank letter-cell rows between wrapped headline lines.</summary>
        private const int LineGap = 1;

        /// <summary>
        /// Splits <paramref name="headline"/> at spaces into one to three lines and picks the split whose block letters
        /// can be drawn largest in <paramref name="cols"/> x <paramref name="rows"/> text cells. Returns each line's
        /// block rows and the letter-cell size in text cells (letter cells keep the text cell's aspect).
        /// </summary>
        internal static (List<List<string>> Lines, double Scale) FitBlockLines(string headline, int cols, int rows)
        {
            var best = (Lines: new List<List<string>> { BlockFont.Render(headline) }, Scale: 0.0);
            if (cols <= 0 || rows <= 0)
                return best;
            string[] words = headline.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var split in Splits(words, Math.Min(3, words.Length)))
            {
                var lines = new List<List<string>>(split.Count);
                int widest = 0;
                foreach (var text in split)
                {
                    var block = BlockFont.Render(text);
                    lines.Add(block);
                    widest = Math.Max(widest, block.Count > 0 ? block[0].Length : 0);
                }
                if (widest == 0)
                    continue;
                int height = lines.Count * (BlockFont.Height + LineGap) - LineGap;
                double scale = Math.Min((double)cols / widest, (double)rows / height);
                if (scale > best.Scale)
                    best = (lines, scale);
            }
            return best;
        }

        /// <summary>Every way to cut <paramref name="words"/> into 1..<paramref name="maxLines"/> lines, in order.</summary>
        private static IEnumerable<List<string>> Splits(string[] words, int maxLines)
        {
            if (words.Length == 0)
                yield break;
            for (int lines = 1; lines <= maxLines; lines++)
                foreach (var cuts in Cuts(1, words.Length, lines - 1))
                {
                    var result = new List<string>(lines);
                    int start = 0;
                    foreach (int cut in cuts)
                    {
                        result.Add(string.Join(' ', words[start..cut]));
                        start = cut;
                    }
                    result.Add(string.Join(' ', words[start..]));
                    yield return result;
                }
        }

        private static IEnumerable<List<int>> Cuts(int from, int count, int needed)
        {
            if (needed == 0)
            {
                yield return new List<int>();
                yield break;
            }
            for (int i = from; i <= count - needed; i++)
                foreach (var rest in Cuts(i + 1, count, needed - 1))
                {
                    rest.Insert(0, i);
                    yield return rest;
                }
        }

        /// <summary>Draws on the canvas character grid: cell fills, and text one glyph per cell.</summary>
        private readonly struct Grid
        {
            private readonly DrawingContext _context;
            private readonly double _cw, _ch, _fontSize;

            public Grid(DrawingContext context, double cw, double ch, double fontSize)
            {
                _context = context;
                _cw = cw;
                _ch = ch;
                _fontSize = fontSize;
            }

            public void Fill(int x, int y, int w, int h, Color color) =>
                _context.FillRectangle(new SolidColorBrush(color), new Rect(x * _cw, y * _ch, w * _cw, h * _ch));

            /// <summary>Fills a fractional cell rect, edges snapped to whole pixels so neighbouring fills meet without seams.</summary>
            public void FillCells(double x, double y, double w, double h, Color color)
            {
                double x0 = Math.Round(x * _cw), y0 = Math.Round(y * _ch);
                double x1 = Math.Round((x + w) * _cw), y1 = Math.Round((y + h) * _ch);
                if (x1 > x0 && y1 > y0)
                    _context.FillRectangle(new SolidColorBrush(color), new Rect(x0, y0, x1 - x0, y1 - y0));
            }

            public void Text(int x, int y, string text, Color color)
            {
                var brush = new SolidColorBrush(color);
                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i] == ' ')
                        continue;
                    var glyph = new FormattedText(text[i].ToString(), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                        GameFonts.ActiveTypeface, _fontSize, brush);
                    _context.DrawText(glyph, new Point((x + i) * _cw, y * _ch));
                }
            }
        }

        /// <summary>Five-row block letters ('#' = filled cell), one blank column between letters.</summary>
        internal static class BlockFont
        {
            public const int Height = 5;

            private static readonly Dictionary<char, string[]> Glyphs = new()
            {
                ['A'] = new[] { ".###.", "#...#", "#####", "#...#", "#...#" },
                ['B'] = new[] { "####.", "#...#", "####.", "#...#", "####." },
                ['C'] = new[] { ".####", "#....", "#....", "#....", ".####" },
                ['D'] = new[] { "####.", "#...#", "#...#", "#...#", "####." },
                ['E'] = new[] { "#####", "#....", "####.", "#....", "#####" },
                ['F'] = new[] { "#####", "#....", "####.", "#....", "#...." },
                ['G'] = new[] { ".####", "#....", "#..##", "#...#", ".###." },
                ['H'] = new[] { "#...#", "#...#", "#####", "#...#", "#...#" },
                ['I'] = new[] { "#####", "..#..", "..#..", "..#..", "#####" },
                ['J'] = new[] { "..###", "...#.", "...#.", "#..#.", ".##.." },
                ['K'] = new[] { "#...#", "#..#.", "###..", "#..#.", "#...#" },
                ['L'] = new[] { "#....", "#....", "#....", "#....", "#####" },
                ['M'] = new[] { "#...#", "##.##", "#.#.#", "#...#", "#...#" },
                ['N'] = new[] { "#...#", "##..#", "#.#.#", "#..##", "#...#" },
                ['O'] = new[] { ".###.", "#...#", "#...#", "#...#", ".###." },
                ['P'] = new[] { "####.", "#...#", "####.", "#....", "#...." },
                ['Q'] = new[] { ".###.", "#...#", "#.#.#", "#..#.", ".##.#" },
                ['R'] = new[] { "####.", "#...#", "####.", "#..#.", "#...#" },
                ['S'] = new[] { ".####", "#....", ".###.", "....#", "####." },
                ['T'] = new[] { "#####", "..#..", "..#..", "..#..", "..#.." },
                ['U'] = new[] { "#...#", "#...#", "#...#", "#...#", ".###." },
                ['V'] = new[] { "#...#", "#...#", "#...#", ".#.#.", "..#.." },
                ['W'] = new[] { "#...#", "#...#", "#.#.#", "##.##", "#...#" },
                ['X'] = new[] { "#...#", ".#.#.", "..#..", ".#.#.", "#...#" },
                ['Y'] = new[] { "#...#", ".#.#.", "..#..", "..#..", "..#.." },
                ['Z'] = new[] { "#####", "...#.", "..#..", ".#...", "#####" },
                ['0'] = new[] { ".###.", "#..##", "#.#.#", "##..#", ".###." },
                ['1'] = new[] { "..#..", ".##..", "..#..", "..#..", ".###." },
                ['2'] = new[] { "####.", "....#", ".###.", "#....", "#####" },
                ['3'] = new[] { "####.", "....#", ".###.", "....#", "####." },
                ['4'] = new[] { "#...#", "#...#", "#####", "....#", "....#" },
                ['5'] = new[] { "#####", "#....", "####.", "....#", "####." },
                ['6'] = new[] { ".###.", "#....", "####.", "#...#", ".###." },
                ['7'] = new[] { "#####", "....#", "...#.", "..#..", "..#.." },
                ['8'] = new[] { ".###.", "#...#", ".###.", "#...#", ".###." },
                ['9'] = new[] { ".###.", "#...#", ".####", "....#", ".###." },
                ['\''] = new[] { "#", "#", ".", ".", "." },
                ['-'] = new[] { "...", "...", "###", "...", "..." },
                [' '] = new[] { "...", "...", "...", "...", "..." },
            };

            /// <summary><paramref name="text"/> as <see cref="Height"/> equal-length rows; characters it has no glyph for are skipped.</summary>
            public static List<string> Render(string text)
            {
                var rows = new StringBuilder[Height];
                for (int r = 0; r < Height; r++)
                    rows[r] = new StringBuilder();
                bool first = true;
                foreach (char ch in text)
                {
                    if (!Glyphs.TryGetValue(ch, out var g))
                        continue;
                    for (int r = 0; r < Height; r++)
                    {
                        if (!first)
                            rows[r].Append('.');
                        rows[r].Append(g[r]);
                    }
                    first = false;
                }
                var result = new List<string>(Height);
                foreach (var row in rows)
                    result.Add(row.ToString());
                return result;
            }
        }
    }
}
