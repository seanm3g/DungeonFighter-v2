using System;
using System.Collections.Generic;
using Avalonia.Media;
using static RPGGame.UI.TitleScreen.TitleSceneBuilder;

namespace RPGGame.UI.TitleScreen
{
    /// <summary>
    /// The common room of the inn, shown behind the character and
    /// inventory screens: a stone hearth with a crackling fire and a shield over the mantle, candle chandeliers,
    /// a night window, a bounty board, a bar with bottles, kegs and a barkeep, tables with candles, and a cat
    /// asleep on the rug. Deterministic for a seed, grid size and time.
    /// </summary>
    public sealed class InnInteriorSceneBuilder
    {
        public const string BoardTitle = "BOUNTIES";

        private static readonly Color NightColor = Color.FromRgb(6, 4, 4);
        private static readonly Color FlameWhite = Color.FromRgb(255, 240, 180);
        private static readonly Color FlameYellow = Color.FromRgb(255, 196, 70);
        private static readonly Color FlameOrange = Color.FromRgb(240, 110, 30);
        private static readonly Color FlameRed = Color.FromRgb(170, 40, 20);
        private static readonly Color WoodDark = Color.FromRgb(30, 18, 12);
        private static readonly Color WoodLit = Color.FromRgb(160, 102, 60);
        private static readonly Color StoneDark = Color.FromRgb(28, 26, 30);
        private static readonly Color StoneLit = Color.FromRgb(176, 152, 130);
        private static readonly Color Iron = Color.FromRgb(96, 90, 96);
        private static readonly Color Parchment = Color.FromRgb(214, 198, 166);
        private static readonly Color Gold = Color.FromRgb(220, 176, 80);

        /// <summary>Light every cell gets, so the room's outline shows away from the fire and candles.</summary>
        private const double Ambient = 0.18;

        private readonly int _seed;
        private Layout? _layout;

        public InnInteriorSceneBuilder(int seed)
        {
            _seed = seed;
        }

        /// <summary>
        /// The common room for use behind menus. Colors are scaled by <paramref name="brightness"/> and cells where
        /// <paramref name="keepClear"/> returns true are left empty so menu text on top stays readable.
        /// </summary>
        public TitleSceneFrame BuildBackdrop(int width, int height, double t, double brightness, Func<int, int, bool>? keepClear)
        {
            width = Math.Max(40, width);
            height = Math.Max(24, height);
            t = Math.Max(0, t);

            if (_layout == null || _layout.W != width || _layout.H != height)
                _layout = new Layout(width, height, _seed);
            var L = _layout;

            var flicker = new double[L.Lights.Count];
            for (int i = 0; i < flicker.Length; i++)
                flicker[i] = TorchFlicker(t, i);

            var frame = new TitleSceneFrame(width, height, NightColor);
            var grid = new Cells(width, height);

            DrawCeiling(grid, L, flicker);
            DrawWall(grid, L, flicker);
            DrawPosts(grid, L, flicker);
            DrawWindow(grid, L, t);
            DrawBoard(grid, L, flicker);
            DrawHearth(grid, L, t, flicker);
            DrawBar(grid, L, t, flicker);
            DrawFloor(grid, L, flicker);
            DrawRug(grid, L, flicker);
            DrawCat(grid, L, t);
            DrawTables(grid, L, t, flicker);
            DrawChandeliers(grid, L, t);
            DrawMotes(grid, L, t);

            grid.Dim(brightness, keepClear);
            grid.EmitRuns(frame.Texts);
            return frame;
        }

        // ───────────────────────────── Lighting ─────────────────────────────

        private static double Light(Layout L, int x, int y, double[] flicker)
        {
            double light = Ambient;
            for (int i = 0; i < L.Lights.Count; i++)
            {
                var s = L.Lights[i];
                double R = s.Radius * flicker[i];
                double dx = x - s.X;
                double dy = (y - s.Y) * 2.0;
                double d = Math.Sqrt(dx * dx + dy * dy);
                if (d < R)
                    light += s.Strength * flicker[i] * Math.Pow(1 - d / R, 1.6);
            }
            return light;
        }

        /// <summary>Quantized so neighbouring cells share colors and merge into runs; above 1 warms toward firelight.</summary>
        private static Color Lit(Color dark, Color lit, double light)
        {
            double q = Math.Round(Math.Min(light, 1.3) * 8) / 8;
            var c = Lerp(dark, lit, Math.Min(1, q));
            return q > 1 ? Lerp(c, Color.FromRgb(255, 196, 120), (q - 1) / 0.3) : c;
        }

        private static Color Flame(double heat) =>
            heat > 0.75 ? FlameWhite : heat > 0.5 ? FlameYellow : heat > 0.28 ? FlameOrange : FlameRed;

        private static void SetLit(Cells grid, Layout L, int x, int y, char ch, Color dark, Color lit, double[] flicker, double scale = 1) =>
            grid.Set(x, y, ch, Lit(dark, lit, Light(L, x, y, flicker) * scale));

        private static void Blank(Cells grid, int x0, int y0, int x1, int y1)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    grid.Set(x, y, ' ', default);
        }

        // ───────────────────────────── Room shell ─────────────────────────────

        private static void DrawCeiling(Cells grid, Layout L, double[] flicker)
        {
            for (int y = 0; y < L.CeilY; y++)
            {
                for (int x = 0; x < L.W; x++)
                {
                    // Joists running back into the dark, boards between them.
                    char ch = x % 12 == 0 ? '|' : y == L.CeilY - 1 && x % 12 == 6 ? '_' : ' ';
                    if (ch != ' ')
                        SetLit(grid, L, x, y, ch, WoodDark, WoodLit, flicker, 0.6);
                }
            }
            for (int x = 0; x < L.W; x++)
                SetLit(grid, L, x, L.CeilY, '=', WoodDark, WoodLit, flicker);
        }

        private static void DrawWall(Cells grid, Layout L, double[] flicker)
        {
            for (int y = L.CeilY + 1; y < L.FloorY; y++)
            {
                for (int x = 0; x < L.W; x++)
                {
                    char ch;
                    if (y == L.FloorY - 1) ch = '=';
                    else if (y == L.RailY) ch = '-';
                    else if (x % 9 == 0) ch = '|';
                    else
                    {
                        int h = Hash(L.Seed, x, y) % 200;
                        ch = h < 3 ? '.' : h < 4 ? 'o' : ' ';
                    }
                    if (ch != ' ')
                        SetLit(grid, L, x, y, ch, WoodDark, WoodLit, flicker, ch == '-' || ch == '=' ? 1 : 0.65);
                }
            }
        }

        private static void DrawPosts(Cells grid, Layout L, double[] flicker)
        {
            foreach (int px in L.Posts)
            {
                for (int y = L.CeilY + 1; y < L.FloorY; y++)
                {
                    SetLit(grid, L, px, y, '|', WoodDark, WoodLit, flicker, 1.1);
                    SetLit(grid, L, px + 1, y, '|', WoodDark, WoodLit, flicker, 1.1);
                }
                SetLit(grid, L, px - 2, L.CeilY + 1, '\\', WoodDark, WoodLit, flicker);
                SetLit(grid, L, px - 1, L.CeilY + 2, '\\', WoodDark, WoodLit, flicker);
                SetLit(grid, L, px + 3, L.CeilY + 1, '/', WoodDark, WoodLit, flicker);
                SetLit(grid, L, px + 2, L.CeilY + 2, '/', WoodDark, WoodLit, flicker);
            }
        }

        private static void DrawWindow(Cells grid, Layout L, double t)
        {
            var w = L.Window;
            int x1 = w.X + w.W - 1, y1 = w.Y + w.H - 1;
            int mx = w.X + w.W / 2, my = w.Y + w.H / 2;
            var frameColor = Color.FromRgb(110, 72, 42);
            var curtain = Color.FromRgb(120, 34, 36);

            for (int y = w.Y; y <= y1; y++)
            {
                for (int x = w.X; x <= x1; x++)
                {
                    bool edgeY = y == w.Y || y == y1 || y == my;
                    bool edgeX = x == w.X || x == x1 || x == mx;
                    if (edgeY || edgeX)
                    {
                        grid.Set(x, y, edgeY && edgeX ? '+' : edgeY ? '-' : '|', frameColor);
                        continue;
                    }
                    // Night sky through the glass.
                    int h = Hash(L.Seed, x, y + 1200) % 100;
                    if (h < 9)
                    {
                        double tw = 0.5 + 0.5 * Math.Sin(t * (0.8 + h * 0.2) + h);
                        grid.Set(x, y, h == 0 ? '+' : '.', Scale(Color.FromRgb(190, 200, 236), Math.Round((0.35 + 0.65 * tw) * 4) / 4));
                    }
                    else
                        grid.Set(x, y, ' ', default);
                }
            }
            grid.Set(x1 - 3, w.Y + 1, 'o', Color.FromRgb(232, 226, 196));

            for (int y = w.Y - 1; y <= y1 + 1; y++)
            {
                grid.Set(w.X - 2, y, '{', curtain);
                grid.Set(w.X - 1, y, '|', curtain);
                grid.Set(x1 + 1, y, '|', curtain);
                grid.Set(x1 + 2, y, '}', curtain);
            }
            for (int x = w.X - 3; x <= x1 + 3; x++)
                grid.Set(x, w.Y - 2, x == w.X - 3 || x == x1 + 3 ? 'o' : '=', Iron);
            for (int x = w.X - 1; x <= x1 + 1; x++)
                grid.Set(x, y1 + 2, '=', frameColor);
        }

        private static void DrawBoard(Cells grid, Layout L, double[] flicker)
        {
            var b = L.Board;
            int x1 = b.X + b.W - 1, y1 = b.Y + b.H - 1;
            Blank(grid, b.X, b.Y, x1, y1);
            for (int y = b.Y; y <= y1; y++)
            {
                for (int x = b.X; x <= x1; x++)
                {
                    bool edgeY = y == b.Y || y == y1;
                    bool edgeX = x == b.X || x == x1;
                    if (edgeY || edgeX)
                        SetLit(grid, L, x, y, edgeY && edgeX ? '+' : edgeY ? '-' : '|', WoodDark, WoodLit, flicker, 1.1);
                }
            }

            int tx = b.X + (b.W - BoardTitle.Length) / 2;
            for (int i = 0; i < BoardTitle.Length; i++)
                grid.Set(tx + i, b.Y + 1, BoardTitle[i], Gold);

            // Notices pinned in two rows, nudged a little so they look hand-placed.
            int cols = Math.Max(1, (b.W - 2) / 6);
            for (int row = 0; row < 2; row++)
            {
                int py = b.Y + 3 + row * 3;
                if (py + 1 >= y1) break;
                for (int c = 0; c < cols; c++)
                {
                    if (Hash(L.Seed, c, row + 40) % 5 == 0) continue;
                    int px = b.X + 2 + c * 6 + Hash(L.Seed, c, row + 41) % 2;
                    if (px + 3 >= x1) break;
                    var paper = Hash(L.Seed, c, row + 42) % 3 == 0 ? Color.FromRgb(190, 170, 130) : Parchment;
                    grid.Set(px + 1, py, 'o', Color.FromRgb(200, 50, 40));
                    for (int k = 0; k < 4; k++)
                    {
                        if (k != 1) grid.Set(px + k, py, '_', Scale(paper, 0.8));
                        grid.Set(px + k, py + 1, k == 0 || k == 3 ? '|' : '~', Scale(paper, 0.9));
                    }
                }
            }
        }

        // ───────────────────────────── Hearth ─────────────────────────────

        private static void DrawHearth(Cells grid, Layout L, double t, double[] flicker)
        {
            int x0 = L.HearthLeft, x1 = L.HearthRight, cx = (x0 + x1) / 2;
            int top = L.OpeningTop, mantle = L.MantleY;

            // Chimney breast above the mantle, then the stone surround.
            for (int y = L.CeilY + 1; y < L.FloorY; y++)
            {
                int left = y < mantle ? x0 + 3 : x0;
                int right = y < mantle ? x1 - 3 : x1;
                for (int x = left; x <= right; x++)
                {
                    bool opening = y >= top && x > x0 + 3 && x < x1 - 3;
                    if (opening) continue;
                    int course = y / 3;
                    char ch = y % 3 == 2 ? '_' : (x + (course & 1) * 4) % 8 == 0 ? '|' : ' ';
                    grid.Set(x, y, ' ', default);
                    if (ch != ' ')
                        SetLit(grid, L, x, y, ch, StoneDark, StoneLit, flicker);
                }
            }

            // Mantle beam and what sits on it.
            for (int x = x0 - 2; x <= x1 + 2; x++)
                SetLit(grid, L, x, mantle, '=', WoodDark, WoodLit, flicker, 1.1);
            int frameIndex = (int)(t * 10);
            foreach (var (mx, i) in new[] { (x0 + 1, 0), (x1 - 1, 1) })
            {
                grid.Set(mx, mantle - 1, 'i', Parchment);
                grid.Set(mx, mantle - 2, Hash(L.Seed + i, frameIndex, 5) % 3 == 0 ? '*' : '\'', FlameYellow);
            }
            grid.Set(x0 + 5, mantle - 1, 'U', Color.FromRgb(140, 120, 100));
            grid.Set(x0 + 7, mantle - 1, '8', Color.FromRgb(90, 130, 110));
            grid.Set(x1 - 6, mantle - 1, '!', Color.FromRgb(70, 120, 60));
            grid.Set(x1 - 4, mantle - 1, 'o', Color.FromRgb(200, 196, 180));

            // Crossed swords behind a shield over the mantle.
            int sy = mantle - 7;
            if (sy > L.CeilY + 1)
            {
                Blank(grid, cx - 7, sy - 1, cx + 7, sy + 5);
                var steel = Color.FromRgb(170, 176, 190);
                var shield = Color.FromRgb(130, 36, 30);
                for (int k = 0; k < 5; k++)
                {
                    grid.Set(cx - 5 + k, sy + k, '\\', steel);
                    grid.Set(cx + 5 - k, sy + k, '/', steel);
                }
                grid.Set(cx - 6, sy - 1, '+', Gold);
                grid.Set(cx + 6, sy - 1, '+', Gold);
                string[] crest = { " ___ ", "|   |", "| + |", " \\_/ " };
                for (int r = 0; r < crest.Length; r++)
                {
                    for (int c = 0; c < crest[r].Length; c++)
                    {
                        char ch = crest[r][c];
                        int x = cx - 2 + c, y = sy + 1 + r;
                        if (ch == ' ' && r is 1 or 2 && c is > 0 and < 4)
                            grid.Set(x, y, ' ', default);
                        else if (ch != ' ')
                            grid.Set(x, y, ch, ch == '+' ? Gold : shield);
                    }
                }
            }

            // Arch and dark firebox.
            for (int x = x0 + 4; x <= x1 - 4; x++)
                SetLit(grid, L, x, top, '_', StoneDark, StoneLit, flicker);
            Blank(grid, x0 + 4, top + 1, x1 - 4, L.FloorY - 1);

            // Logs, then flames shaped by an envelope and rolling noise.
            int logHalf = Math.Max(3, (x1 - x0) / 4);
            var bark = Color.FromRgb(96, 58, 32);
            for (int x = cx - logHalf; x <= cx + logHalf; x++)
            {
                int k = (x - cx + logHalf) % 5;
                grid.Set(x, L.FloorY - 1, k == 0 ? '(' : k == 4 ? ')' : '=', bark);
            }

            int flameRows = Math.Max(3, L.FloorY - 2 - top - 1);
            double halfW = logHalf + 2;
            for (int x = cx - (int)halfW; x <= cx + (int)halfW; x++)
            {
                double env = 1 - Math.Abs(x - cx) / (halfW + 1);
                double n = Noise(x * 0.35, t * 3.2, L.Seed) * 0.6 + Noise(x * 0.9, t * 6.5, L.Seed + 3) * 0.4;
                double h = flameRows * Math.Pow(env, 0.7) * (0.45 + 0.6 * n);
                for (int k = 0; k < (int)Math.Ceiling(h); k++)
                {
                    double heat = 1 - k / Math.Max(1, h);
                    int y = L.FloorY - 2 - k;
                    if (y <= top) break;
                    char ch = heat > 0.75 ? '@' : heat > 0.5 ? (Hash(L.Seed, x, (int)(t * 12) + k) % 2 == 0 ? '(' : ')') : heat > 0.28 ? '^' : '\'';
                    grid.Set(x, y, ch, Flame(heat));
                }
            }

            // Sparks rising toward the flue.
            for (int i = 0; i < 6; i++)
            {
                double period = 1.4 + Rand(L.Seed, i, 1);
                double a = (t + Rand(L.Seed, i, 2) * period) % period;
                int cycle = (int)((t + Rand(L.Seed, i, 2) * period) / period);
                int x = cx + (int)Math.Round((Rand(L.Seed + cycle, i, 3) - 0.5) * 2 * logHalf + Math.Sin(a * 5 + i));
                int y = L.FloorY - 3 - (int)(a * (L.FloorY - top) / period);
                if (y > top)
                    grid.Set(x, y, a / period < 0.5 ? '*' : '.', Flame(1 - a / period));
            }

            // Hearthstone.
            for (int x = x0 - 2; x <= x1 + 2; x++)
                SetLit(grid, L, x, L.FloorY, '=', StoneDark, StoneLit, flicker);
        }

        // ───────────────────────────── Bar ─────────────────────────────

        private static void DrawBar(Cells grid, Layout L, double t, double[] flicker)
        {
            int left = L.BarLeft, right = L.W - 2;
            if (left >= right - 6) return;

            Color[] glass =
            {
                Color.FromRgb(70, 130, 64), Color.FromRgb(170, 110, 40), Color.FromRgb(70, 96, 170),
                Color.FromRgb(150, 40, 50), Color.FromRgb(200, 200, 190),
            };
            const string bottles = "!iI8U";
            foreach (int shelfY in L.Shelves)
            {
                for (int x = left; x <= right; x++)
                    SetLit(grid, L, x, shelfY, '=', WoodDark, WoodLit, flicker, 1.1);
                for (int x = left + 1; x < right; x += 2)
                {
                    int h = Hash(L.Seed, x, shelfY + 60);
                    if (h % 6 == 0) { grid.Set(x, shelfY - 1, ' ', default); continue; }
                    grid.Set(x, shelfY - 1, bottles[h % bottles.Length], glass[(h / 7) % glass.Length]);
                    grid.Set(x + 1, shelfY - 1, ' ', default);
                }
            }

            // Kegs racked on the back wall.
            var keg = Color.FromRgb(120, 76, 42);
            for (int x = left + 1; x + 2 < right; x += 6)
            {
                int y = L.FloorY - 3;
                Blank(grid, x, y, x + 2, y + 2);
                grid.Set(x, y, '/', keg); grid.Set(x + 1, y, '=', Iron); grid.Set(x + 2, y, '\\', keg);
                grid.Set(x, y + 1, '|', keg); grid.Set(x + 1, y + 1, 'o', Gold); grid.Set(x + 2, y + 1, '|', keg);
                grid.Set(x, y + 2, '\\', keg); grid.Set(x + 1, y + 2, '=', Iron); grid.Set(x + 2, y + 2, '/', keg);
            }

            // Barkeep polishing a mug.
            int bx = L.BarkeepX, cy = L.CounterY;
            var skin = Color.FromRgb(200, 150, 110);
            var apron = Color.FromRgb(180, 170, 150);
            Blank(grid, bx - 3, cy - 3, bx + 3, cy - 1);
            grid.Set(bx, cy - 3, 'o', skin);
            bool polish = (int)(t * 2.5) % 2 == 0;
            grid.Set(bx - 1, cy - 2, '/', apron);
            grid.Set(bx, cy - 2, '|', apron);
            grid.Set(bx + 1, cy - 2, polish ? '\\' : '_', apron);
            grid.Set(bx + 2, cy - 2, polish ? ' ' : 'U', Color.FromRgb(170, 150, 120));
            grid.Set(bx + 2, cy - 1, polish ? 'U' : ' ', Color.FromRgb(170, 150, 120));
            grid.Set(bx, cy - 1, '|', apron);

            // Counter.
            for (int x = left - 1; x < L.W; x++)
                SetLit(grid, L, x, cy, '=', WoodDark, WoodLit, flicker, 1.2);
            for (int y = cy + 1; y < L.H; y++)
            {
                for (int x = left - 1; x < L.W; x++)
                {
                    char ch = x == left - 1 || (x - left) % 5 == 4 ? '|' : y == cy + 2 ? '-' : ' ';
                    grid.Set(x, y, ' ', default);
                    if (ch != ' ')
                        SetLit(grid, L, x, y, ch, WoodDark, WoodLit, flicker);
                }
            }
            foreach (int mx in L.CounterMugs)
            {
                grid.Set(mx, cy - 1, 'U', Color.FromRgb(170, 150, 120));
                if (((int)(t * 1.5) + mx) % 3 == 0)
                    grid.Set(mx, cy - 2, '~', Color.FromRgb(230, 226, 210)); // foam
            }
        }

        // ───────────────────────────── Floor ─────────────────────────────

        private static void DrawFloor(Cells grid, Layout L, double[] flicker)
        {
            for (int y = L.FloorY + 1; y < L.H; y++)
            {
                int v = y - L.FloorY - 1;
                int offset = (v / 3 % 2) * 7;
                for (int x = 0; x < L.BarLeft - 1; x++)
                {
                    char ch = v % 3 == 2 ? '_' : (x + offset) % 14 == 0 ? '|' : ' ';
                    if (ch != ' ')
                        SetLit(grid, L, x, y, ch, WoodDark, WoodLit, flicker, 0.9);
                }
            }
        }

        private static void DrawRug(Cells grid, Layout L, double[] flicker)
        {
            var red = Color.FromRgb(120, 30, 32);
            var redLit = Color.FromRgb(210, 70, 56);
            var trim = Color.FromRgb(200, 150, 60);
            int x0 = L.RugLeft, x1 = L.RugRight, y0 = L.RugTop, y1 = L.RugBottom;
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    char ch;
                    Color c;
                    if (y == y0 || y == y1) { ch = '='; c = trim; }
                    else if (x == x0 || x == x1) { ch = '|'; c = trim; }
                    else
                    {
                        int k = (x + (y - y0) * 2) % 6;
                        ch = k == 0 ? '<' : k == 3 ? '>' : k == 1 || k == 4 ? '-' : ' ';
                        c = Lit(red, redLit, Light(L, x, y, flicker));
                    }
                    grid.Set(x, y, ch, ch == '=' || ch == '|' ? Lit(Scale(trim, 0.3), trim, Light(L, x, y, flicker)) : c);
                }
            }
            for (int x = x0; x <= x1; x += 2)
                grid.Set(x, y1 + 1, '\'', Scale(trim, 0.6)); // fringe
        }

        private static void DrawCat(Cells grid, Layout L, double t)
        {
            var fur = Color.FromRgb(200, 140, 70);
            int x = L.CatX, y = L.CatY;
            string[] cat = { " /\\_/\\ ", "( -.- )" };
            for (int r = 0; r < cat.Length; r++)
                for (int c = 0; c < cat[r].Length; c++)
                    grid.Set(x + c, y + r, cat[r][c], fur);
            bool flick = (t % 5.0) < 0.6;
            grid.Set(x + 7, y + 1, flick ? '/' : '~', fur);
            grid.Set(x + 8, y + 1, flick ? ' ' : '_', fur);

            // Zzz drifting up.
            var zColor = Color.FromRgb(180, 190, 220);
            for (int i = 0; i < 3; i++)
            {
                double a = (t + i * 1.2) % 3.6;
                int zy = y - 1 - (int)(a * 1.2);
                if (zy < L.FloorY + 1) continue;
                grid.Set(x + 6 + (int)Math.Round(a * 1.5), zy, a < 1.6 ? 'z' : 'Z', Scale(zColor, 1 - a / 4.5));
            }
        }

        private static void DrawTables(Cells grid, Layout L, double t, double[] flicker)
        {
            int frameIndex = (int)(t * 10);
            for (int i = 0; i < L.Tables.Count; i++)
            {
                int tx = L.Tables[i], ty = L.TableY;
                int half = L.TableHalf;

                Blank(grid, tx - half, ty - 2, tx + half, ty + 2);
                for (int x = tx - half; x <= tx + half; x++)
                    SetLit(grid, L, x, ty, '=', WoodDark, WoodLit, flicker, 1.2);
                for (int y = ty + 1; y <= ty + 2; y++)
                {
                    SetLit(grid, L, tx - half + 1, y, '|', WoodDark, WoodLit, flicker, 1.1);
                    SetLit(grid, L, tx + half - 1, y, '|', WoodDark, WoodLit, flicker, 1.1);
                }

                // Candle in a bottle, and drinks.
                grid.Set(tx, ty - 1, 'i', Parchment);
                int h = Hash(L.Seed + i * 17, frameIndex, 3) % 4;
                grid.Set(tx, ty - 2, h == 0 ? '\'' : '*', h == 0 ? FlameOrange : FlameYellow);
                grid.Set(tx - half + 2, ty - 1, 'U', Color.FromRgb(170, 150, 120));
                if (i % 2 == 0)
                    grid.Set(tx + half - 3, ty - 1, '!', Color.FromRgb(70, 130, 64));
                else
                    grid.Set(tx + half - 2, ty - 1, 'o', Color.FromRgb(200, 196, 180));

                // Stools.
                foreach (int sx in new[] { tx - half - 4, tx + half + 4 })
                {
                    SetLit(grid, L, sx - 1, ty + 1, '=', WoodDark, WoodLit, flicker, 1.1);
                    SetLit(grid, L, sx, ty + 1, '=', WoodDark, WoodLit, flicker, 1.1);
                    SetLit(grid, L, sx + 1, ty + 1, '=', WoodDark, WoodLit, flicker, 1.1);
                    SetLit(grid, L, sx - 1, ty + 2, '/', WoodDark, WoodLit, flicker);
                    SetLit(grid, L, sx + 1, ty + 2, '\\', WoodDark, WoodLit, flicker);
                }
            }
        }

        private static void DrawChandeliers(Cells grid, Layout L, double t)
        {
            int frameIndex = (int)(t * 10);
            for (int i = 0; i < L.Chandeliers.Count; i++)
            {
                var (cx, cy) = L.Chandeliers[i];
                for (int y = L.CeilY + 1; y < cy - 1; y++)
                    grid.Set(cx, y, '|', Iron);
                for (int x = cx - 4; x <= cx + 4; x++)
                    grid.Set(x, cy + 1, x == cx - 4 ? '\\' : x == cx + 4 ? '/' : '_', Iron);
                for (int k = -1; k <= 1; k++)
                {
                    int x = cx + k * 3;
                    grid.Set(x, cy, 'i', Parchment);
                    int h = Hash(L.Seed + i * 13 + k, frameIndex, 7) % 4;
                    grid.Set(x, cy - 1, h == 0 ? '\'' : '*', h == 0 ? FlameOrange : FlameYellow);
                }
            }
        }

        private static void DrawMotes(Cells grid, Layout L, double t)
        {
            // Dust and smoke drifting in the firelight.
            var mote = Color.FromRgb(200, 170, 130);
            for (int i = 0; i < L.MoteCount; i++)
            {
                double period = 9 + 6 * Rand(L.Seed, i, 11);
                double a = (t + Rand(L.Seed, i, 12) * period) % period;
                double life = a / period;
                int x = L.MoteOrigins[i] + (int)Math.Round(Math.Sin(a * 0.5 + i) * 6 + a * 0.8);
                int y = L.FloorY - 4 - (int)(life * (L.FloorY - L.CeilY - 6));
                double glow = Math.Sin(Math.PI * life);
                if (glow < 0.25) continue;
                grid.Set(x, y, '.', Scale(mote, Math.Round(glow * 4) / 4));
            }
        }

        // ───────────────────────────── Layout ─────────────────────────────

        private readonly record struct LightSource(int X, int Y, double Radius, double Strength);
        private readonly record struct Rect(int X, int Y, int W, int H);

        private sealed class Layout
        {
            public readonly int W, H, Seed;
            public readonly int CeilY, FloorY, RailY;
            public readonly int HearthLeft, HearthRight, OpeningTop, MantleY;
            public readonly int BarLeft, CounterY, BarkeepX;
            public readonly int RugLeft, RugRight, RugTop, RugBottom, CatX, CatY;
            public readonly int TableY, TableHalf, MoteCount;
            public readonly Rect Window, Board;
            public readonly List<int> Posts = new();
            public readonly List<int> Shelves = new();
            public readonly List<int> CounterMugs = new();
            public readonly List<int> Tables = new();
            public readonly List<(int X, int Y)> Chandeliers = new();
            public readonly List<LightSource> Lights = new();
            public readonly int[] MoteOrigins;

            public Layout(int w, int h, int seed)
            {
                W = w;
                H = h;
                Seed = seed;

                CeilY = Math.Max(2, h / 13);
                FloorY = h - Math.Max(8, (int)(h * 0.3));
                RailY = FloorY - Math.Max(3, (FloorY - CeilY) / 6);
                int wallRows = FloorY - CeilY - 1;

                // Hearth on the middle of the back wall.
                int hw = Math.Clamp(w / 5, 18, 40);
                HearthLeft = w / 2 - hw / 2;
                HearthRight = HearthLeft + hw - 1;
                OpeningTop = FloorY - Math.Clamp((int)(wallRows * 0.45), 6, 14);
                MantleY = OpeningTop - 1;
                int fireX = w / 2;
                Lights.Add(new LightSource(fireX, FloorY - 3, Math.Clamp(w / 4.5, 16, 48), 1.15));

                Posts.Add((int)(w * 0.29));
                Posts.Add((int)(w * 0.71));

                // Chandeliers between the posts and the hearth.
                int chY = CeilY + Math.Max(3, wallRows / 7);
                foreach (double f in new[] { 0.345, 0.655 })
                {
                    int cx = (int)(w * f);
                    Chandeliers.Add((cx, chY));
                    Lights.Add(new LightSource(cx, chY, Math.Clamp(w / 10.0, 8, 22), 0.7));
                }

                // Window and bounty board on the left wall.
                int winW = Math.Clamp(w / 16, 9, 15) | 1;
                int winH = Math.Clamp(wallRows / 3, 5, 11) | 1;
                Window = new Rect(Math.Max(3, (int)(w * 0.07) - winW / 2), CeilY + 4, winW, winH);
                int boardW = Math.Clamp(w / 11, 12, 22);
                int boardH = Math.Clamp(wallRows / 3, 6, 10);
                Board = new Rect((int)(w * 0.19) - boardW / 2, CeilY + 5, boardW, boardH);

                // Bar along the right.
                BarLeft = (int)(w * 0.75);
                Shelves.Add(CeilY + Math.Max(3, wallRows / 4));
                Shelves.Add(CeilY + Math.Max(6, wallRows / 2));
                CounterY = FloorY + 2;
                BarkeepX = BarLeft + (w - BarLeft) / 2;
                for (int x = BarLeft + 2; x < w - 2; x += 7)
                {
                    if (Math.Abs(x - BarkeepX) > 3 && Hash(seed, x, 77) % 3 != 0)
                        CounterMugs.Add(x);
                }

                // Rug and cat in front of the fire.
                RugLeft = HearthLeft + 2;
                RugRight = HearthRight - 2;
                RugTop = FloorY + 2;
                RugBottom = Math.Min(h - 3, RugTop + 3);
                CatX = fireX - 9;
                CatY = RugTop + 1;

                // Tables either side of the rug.
                TableHalf = Math.Clamp(w / 32, 4, 7);
                TableY = Math.Min(h - 4, FloorY + (h - FloorY) / 2 + 1);
                foreach (double f in new[] { 0.14, 0.33, 0.62 })
                {
                    int tx = (int)(w * f);
                    bool clearOfRug = tx + TableHalf + 5 < RugLeft || tx - TableHalf - 5 > RugRight;
                    bool clearOfBar = tx + TableHalf + 5 < BarLeft - 1;
                    if (clearOfRug && clearOfBar)
                    {
                        Tables.Add(tx);
                        Lights.Add(new LightSource(tx, TableY - 2, 8, 0.45));
                    }
                }

                var rng = new Random(seed);
                MoteCount = Math.Clamp(w / 10, 8, 24);
                MoteOrigins = new int[MoteCount];
                for (int i = 0; i < MoteCount; i++)
                    MoteOrigins[i] = rng.Next(HearthLeft - hw, HearthRight + hw);
            }
        }
    }
}
