using System;
using System.Collections.Generic;
using Avalonia.Media;

namespace RPGGame.UI.TitleScreen
{
    /// <summary>Solid cell rectangle (title letter pixel, bevel, or extrusion).</summary>
    public readonly record struct TitleSceneBlock(int X, int Y, int Width, int Height, Color Color);

    /// <summary>Single-color run of glyphs at a grid cell.</summary>
    public readonly record struct TitleSceneText(int X, int Y, string Text, Color Color);

    /// <summary>One full-screen title frame: blocks paint under text.</summary>
    public sealed class TitleSceneFrame
    {
        public int Width { get; }
        public int Height { get; }
        public Color Background { get; }
        public List<TitleSceneBlock> Blocks { get; } = new();
        public List<TitleSceneText> Texts { get; } = new();

        public TitleSceneFrame(int width, int height, Color background)
        {
            Width = width;
            Height = height;
            Background = background;
        }
    }

    /// <summary>
    /// Procedural title screen: a torch-lit dungeon wall with stalactites, drips, rising embers,
    /// rolling fog and eyes in the dark. DEMON / FIGHTER are forged from sparks that fly in from the
    /// torches and the fog, flash white-hot on landing, and cool to orange / red by <see cref="SettleSeconds"/>.
    /// Pure and deterministic for a given seed, grid size and time, so it is testable without a canvas.
    /// </summary>
    public sealed class TitleSceneBuilder
    {
        /// <summary>By this time every letter is in place and in its final color.</summary>
        public const double SettleSeconds = 2.0;

        public const string TopWord = "DEMON";
        public const string BottomWord = "FIGHTER";
        public const string Tagline = "Enter the depths.  Face the darkness.  Claim your glory.";
        public const string PressKeyPrompt = "Press any key to continue...";

        public static readonly Color DemonColor = Color.FromRgb(255, 132, 16);
        public static readonly Color FighterColor = Color.FromRgb(228, 26, 34);
        public static readonly Color BackgroundColor = Color.FromRgb(5, 3, 7);

        private static readonly Color WhiteHot = Color.FromRgb(255, 248, 220);
        private static readonly Color SparkYellow = Color.FromRgb(255, 214, 90);
        private static readonly Color Gold = Color.FromRgb(212, 168, 72);
        private static readonly Color Parchment = Color.FromRgb(214, 198, 166);

        private const double LandingCoolSeconds = 0.35;
        private const double ShockwaveSeconds = 0.8;

        private readonly int _seed;
        private Layout? _layout;

        public TitleSceneBuilder(int seed)
        {
            _seed = seed;
        }

        public TitleSceneFrame Build(int width, int height, double t) =>
            BuildCore(width, height, t, withTitle: true, brightness: 1, keepClear: null);

        /// <summary>
        /// The cave alone (torches, fog, embers, drips, eyes) for use behind menus: no title letters, glow, tagline
        /// or prompt. Colors are scaled by <paramref name="brightness"/> and cells where <paramref name="keepClear"/>
        /// returns true are left empty so menu text on top stays readable.
        /// </summary>
        public TitleSceneFrame BuildBackdrop(int width, int height, double t, double brightness, Func<int, int, bool>? keepClear)
        {
            // Eyes only start opening after a few seconds; skip the wait.
            return BuildCore(width, height, t + SettleSeconds + 1.5, withTitle: false, brightness, keepClear);
        }

        private TitleSceneFrame BuildCore(int width, int height, double t, bool withTitle, double brightness, Func<int, int, bool>? keepClear)
        {
            width = Math.Max(40, width);
            height = Math.Max(24, height);
            t = Math.Max(0, t);

            if (_layout == null || _layout.W != width || _layout.H != height)
                _layout = new Layout(width, height, _seed);
            var L = _layout;

            var frame = new TitleSceneFrame(width, height, BackgroundColor);
            var grid = new Cells(width, height);
            var landedMask = new bool[width * height];

            // ── Title blocks (computed first so the environment can avoid landed letters) ──
            int shakeX = 0;
            double sinceImpact = t - L.ImpactTime;
            if (sinceImpact >= 0 && sinceImpact < 0.25 && t < SettleSeconds)
                shakeX = (Hash(_seed, (int)(t * 40), 7) & 1) == 0 ? -1 : 1;

            if (withTitle)
                BuildTitle(frame, grid, landedMask, L, t, shakeX);

            // ── Environment ──
            double torchFlicker0 = TorchFlicker(t, 0);
            double torchFlicker1 = TorchFlicker(t, 1);
            double titleGlow = withTitle && t >= L.ImpactTime ? 0.55 + 0.08 * Math.Sin(t * 1.7) : 0;
            double flash = withTitle && sinceImpact >= 0 && sinceImpact < ShockwaveSeconds ? 1 - sinceImpact / ShockwaveSeconds : 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (landedMask[y * width + x])
                        continue;

                    double light = Light(L, x, y, torchFlicker0, torchFlicker1, titleGlow, flash);
                    DrawWallCell(grid, L, x, y, light);
                }
            }

            DrawCeiling(grid, L, t, torchFlicker0, torchFlicker1, titleGlow, landedMask);
            DrawEyes(grid, L, t, landedMask);
            DrawFog(grid, L, t, torchFlicker0, torchFlicker1, landedMask);
            DrawDrips(grid, L, t);
            DrawTorches(grid, L, t);
            DrawEmbers(grid, L, t);
            if (withTitle)
            {
                DrawShockwave(grid, L, t, landedMask);
                DrawDecorator(grid, L, t, shakeX);
                DrawTagline(grid, L, t);
                DrawPrompt(grid, L, t);
            }
            else
            {
                grid.Dim(brightness, keepClear);
            }

            grid.EmitRuns(frame.Texts);
            return frame;
        }

        // ───────────────────────────── Title ─────────────────────────────

        private static void BuildTitle(TitleSceneFrame frame, Cells grid, bool[] landedMask, Layout L, double t, int shakeX)
        {
            bool settled = t >= SettleSeconds;
            int pw = L.PixelW, ph = L.PixelH;

            // Shimmer sweep after settle: brightens the base color only (hue stays orange / red).
            double sweepX = double.NaN;
            if (settled)
            {
                double cycle = (t - SettleSeconds) % 6.0;
                if (cycle < 1.8)
                    sweepX = L.TitleMinX - 20 + (L.TitleMaxX - L.TitleMinX + 40) * (cycle / 1.8);
            }
            double breathe = settled ? 0.04 * Math.Sin(t * 2.3) : 0;

            // Extrusion first (draws under faces).
            for (int d = L.ExtrudeDepth; d >= 1; d--)
            {
                foreach (var p in L.Pixels)
                {
                    if (!settled && t < p.LandTime)
                        continue;
                    var baseColor = p.Word == 0 ? DemonColor : FighterColor;
                    var shade = Scale(baseColor, 0.42 - 0.1 * (d - 1));
                    AddBlock(frame, landedMask, L, p.X + d + shakeX, p.Y + d, pw, ph, shade);
                }
            }

            foreach (var p in L.Pixels)
            {
                var finalColor = p.Word == 0 ? DemonColor : FighterColor;
                // Gentle top-to-bottom gradient, still the same hue.
                finalColor = Scale(finalColor, 1.06 - 0.03 * p.Row);
                if (p.BottomEdge)
                    finalColor = Scale(finalColor, 0.88);

                if (!settled && t < p.LandTime)
                {
                    if (t < p.Delay)
                        continue;

                    // In flight: eased arc from spawn to target, glowing white-hot → yellow.
                    double prog = (t - p.Delay) / p.Flight;
                    double e = 1 - Math.Pow(1 - prog, 3);
                    double fx = p.StartX + (p.X - p.StartX) * e;
                    double fy = p.StartY + (p.Y - p.StartY) * e - Math.Sin(Math.PI * prog) * p.Arc;
                    var c = Lerp(WhiteHot, SparkYellow, prog);
                    frame.Blocks.Add(new TitleSceneBlock((int)Math.Round(fx), (int)Math.Round(fy), pw, ph, c));

                    double tp = Math.Max(0, prog - 0.12);
                    double te = 1 - Math.Pow(1 - tp, 3);
                    double tx = p.StartX + (p.X - p.StartX) * te;
                    double ty = p.StartY + (p.Y - p.StartY) * te - Math.Sin(Math.PI * tp) * p.Arc;
                    grid.Set((int)Math.Round(tx), (int)Math.Round(ty) + ph / 2, prog < 0.5 ? '*' : '.', Scale(SparkYellow, 0.7));
                    continue;
                }

                Color face = finalColor;
                if (!settled)
                {
                    // Landing heat: white-hot cooling to final color well before SettleSeconds.
                    double heat = 1 - (t - p.LandTime) / LandingCoolSeconds;
                    if (heat > 0)
                        face = Lerp(finalColor, WhiteHot, Math.Pow(heat, 1.5));
                }
                else
                {
                    double m = 1 + breathe;
                    if (!double.IsNaN(sweepX))
                    {
                        double dx = (p.X - sweepX) / (6.0 * pw);
                        m += 0.22 * Math.Exp(-dx * dx);
                    }
                    face = Scale(finalColor, m);
                }

                AddBlock(frame, landedMask, L, p.X + shakeX, p.Y, pw, ph, face);

                // Bevel highlight strip on top edges when pixels are tall enough to show it.
                if (p.TopEdge && ph >= 2)
                    frame.Blocks.Add(new TitleSceneBlock(p.X + shakeX, p.Y, pw, 1, Lerp(face, WhiteHot, 0.22)));
            }
        }

        private static void AddBlock(TitleSceneFrame frame, bool[] mask, Layout L, int x, int y, int w, int h, Color c)
        {
            frame.Blocks.Add(new TitleSceneBlock(x, y, w, h, c));
            for (int yy = y; yy < y + h; yy++)
            {
                if (yy < 0 || yy >= L.H) continue;
                for (int xx = x; xx < x + w; xx++)
                {
                    if (xx < 0 || xx >= L.W) continue;
                    mask[yy * L.W + xx] = true;
                }
            }
        }

        // ───────────────────────────── Lighting / wall ─────────────────────────────

        private static double TorchFlicker(double t, int i)
        {
            double s = i * 17.3;
            return 0.86
                + 0.07 * Math.Sin(t * 7.1 + s)
                + 0.05 * Math.Sin(t * 13.7 + s * 2.1)
                + 0.03 * Math.Sin(t * 23.3 + s * 0.7);
        }

        private static double Light(Layout L, int x, int y, double f0, double f1, double titleGlow, double flash)
        {
            double light = 0;
            for (int i = 0; i < L.Torches.Count; i++)
            {
                var (tx, ty) = L.Torches[i];
                double f = i == 0 ? f0 : f1;
                double R = L.TorchRadius * f;
                double dx = x - tx;
                double dy = (y - ty) * 2.0;
                double d = Math.Sqrt(dx * dx + dy * dy);
                if (d < R)
                    light += f * Math.Pow(1 - d / R, 1.6);
            }

            if (titleGlow > 0 || flash > 0)
            {
                double bx = x < L.TitleMinX ? L.TitleMinX - x : x > L.TitleMaxX ? x - L.TitleMaxX : 0;
                double by = (y < L.TitleMinY ? L.TitleMinY - y : y > L.TitleMaxY ? y - L.TitleMaxY : 0) * 2.0;
                double d = Math.Sqrt(bx * bx + by * by);
                const double glowR = 14;
                if (d < glowR)
                    light += titleGlow * Math.Pow(1 - d / glowR, 2);

                if (flash > 0)
                {
                    double fr = L.W * 0.55;
                    double cdx = x - L.TitleCenterX;
                    double cdy = (y - L.TitleCenterY) * 2.0;
                    double cd = Math.Sqrt(cdx * cdx + cdy * cdy);
                    if (cd < fr)
                        light += 0.7 * flash * (1 - cd / fr);
                }
            }

            return light;
        }

        private static void DrawWallCell(Cells grid, Layout L, int x, int y, double light)
        {
            // Quantize so neighbouring cells share colors and merge into runs.
            double q = Math.Round(Math.Min(light, 1.3) * 8) / 8;
            if (q < 0.125 || y < L.CeilingRows || y >= L.FogTop + 2)
                return;

            int course = (y - L.CeilingRows) / 3;
            int row = (y - L.CeilingRows) % 3;
            int offset = (course & 1) * 5;
            char ch;
            if (row == 2)
                ch = '_';
            else if ((x + offset) % 10 == 0)
                ch = '|';
            else
            {
                int h = Hash(L.Seed, x, y) % 100;
                if (h < 3) ch = '.';
                else if (h < 4) ch = ',';
                else if (h < 5) ch = '\'';
                else return;
            }

            var color = Lerp(Color.FromRgb(34, 20, 20), Color.FromRgb(196, 124, 74), Math.Min(1, q));
            if (q > 1)
                color = Lerp(color, Color.FromRgb(255, 190, 120), (q - 1) / 0.3);
            grid.Set(x, y, ch, color);
        }

        // ───────────────────────────── Ceiling / drips ─────────────────────────────

        private static void DrawCeiling(Cells grid, Layout L, double t, double f0, double f1, double glow, bool[] mask)
        {
            for (int x = 0; x < L.W; x++)
            {
                int len = L.Stalactites[x];
                for (int y = 0; y < len; y++)
                {
                    if (mask[y * L.W + x]) continue;
                    double light = Light(L, x, y, f0, f1, glow, 0);
                    double q = Math.Round(Math.Min(1, 0.22 + light) * 6) / 6;
                    char ch = y == 0 ? '#' : y == len - 1 ? 'v' : (Hash(L.Seed, x, y + 300) & 1) == 0 ? 'V' : '|';
                    grid.Set(x, y, ch, Lerp(Color.FromRgb(20, 16, 22), Color.FromRgb(150, 120, 110), q));
                }
            }
        }

        private static void DrawDrips(Cells grid, Layout L, double t)
        {
            var dripColor = Color.FromRgb(120, 170, 205);
            foreach (var drip in L.Drips)
            {
                double a = (t + drip.Phase) % drip.Period;
                int tipY = L.Stalactites[drip.X];
                if (a < 0.9)
                {
                    // Bead swelling at the tip.
                    if (a > 0.4)
                        grid.Set(drip.X, tipY, '.', Scale(dripColor, 0.5 + 0.5 * (a - 0.4) / 0.5));
                    continue;
                }

                double fall = a - 0.9;
                double y = tipY + 0.5 * 70 * fall * fall;
                int floorY = L.FogTop + 3;
                if (y < floorY)
                {
                    grid.Set(drip.X, (int)y, '\'', dripColor);
                }
                else
                {
                    double tFloor = Math.Sqrt(2 * (floorY - tipY) / 70.0);
                    double splash = fall - tFloor;
                    if (splash < 0.18)
                    {
                        var sc = Scale(dripColor, 1 - splash / 0.18);
                        grid.Set(drip.X - 1, floorY, '.', sc);
                        grid.Set(drip.X + 1, floorY, '.', sc);
                        grid.Set(drip.X, floorY - 1, '\'', sc);
                    }
                }
            }
        }

        // ───────────────────────────── Eyes ─────────────────────────────

        private static void DrawEyes(Cells grid, Layout L, double t, bool[] mask)
        {
            if (t < 3.0)
                return;

            foreach (var eye in L.Eyes)
            {
                double a = (t + eye.Phase) % eye.Period;
                if (a > eye.Open)
                    continue;

                // Fade in, blink once midway, fade out.
                double env = Math.Min(1, Math.Min(a / 0.4, (eye.Open - a) / 0.4));
                double mid = eye.Open * 0.55;
                if (Math.Abs(a - mid) < 0.07)
                    continue;

                int look = a > mid ? eye.Look : 0;
                var c = Scale(Color.FromRgb(235, 24, 18), 0.35 + 0.65 * env);
                int x = eye.X + look;
                if (!mask[eye.Y * L.W + x] && !mask[eye.Y * L.W + x + 2])
                {
                    grid.Set(x, eye.Y, 'o', c);
                    grid.Set(x + 2, eye.Y, 'o', c);
                }
            }
        }

        // ───────────────────────────── Fog ─────────────────────────────

        private static void DrawFog(Cells grid, Layout L, double t, double f0, double f1, bool[] mask)
        {
            const string ramp = " .-~=";
            for (int y = L.FogTop; y < L.H; y++)
            {
                double v = (y - L.FogTop + 1) / (double)(L.H - L.FogTop);
                for (int x = 0; x < L.W; x++)
                {
                    if (mask[y * L.W + x]) continue;
                    double n = Noise(x * 0.055 + t * 0.35, y * 0.45 - t * 0.08, L.Seed) * 0.65
                             + Noise(x * 0.14 - t * 0.55, y * 0.9 + 11, L.Seed + 1) * 0.35;
                    double density = n * 0.75 + v * 0.45 - 0.3;
                    int idx = Math.Clamp((int)(density / 0.16), 0, ramp.Length - 1);
                    if (idx == 0) continue;

                    double light = Light(L, x, y, f0, f1, 0, 0);
                    double lq = Math.Round(Math.Min(1, light) * 4) / 4;
                    var baseColor = Lerp(Color.FromRgb(40, 32, 58), Color.FromRgb(128, 112, 152), (idx - 1) / 3.0);
                    var color = Lerp(baseColor, Color.FromRgb(200, 120, 70), lq * 0.6);
                    grid.Set(x, y, ramp[idx], color);
                }
            }
        }

        // ───────────────────────────── Torches ─────────────────────────────

        private static void DrawTorches(Cells grid, Layout L, double t)
        {
            var iron = Color.FromRgb(96, 84, 86);
            var wood = Color.FromRgb(120, 72, 40);
            int frameIndex = (int)(t * 12);

            for (int i = 0; i < L.Torches.Count; i++)
            {
                var (cx, cy) = L.Torches[i];

                // Bracket and handle.
                grid.Set(cx - 1, cy + 1, '\\', iron);
                grid.Set(cx, cy + 1, '_', iron);
                grid.Set(cx + 1, cy + 1, '/', iron);
                grid.Set(cx, cy + 2, '|', wood);
                grid.Set(cx, cy + 3, '|', wood);
                grid.Set(cx - 1, cy + 4, '=', iron);
                grid.Set(cx, cy + 4, '#', iron);
                grid.Set(cx + 1, cy + 4, '=', iron);

                // Flame: base row hot, tongues above flicker per frame.
                const string tongues = "()^{}";
                int h(int k) => Hash(L.Seed + i * 31, frameIndex, k);

                grid.Set(cx - 1, cy, h(1) % 3 == 0 ? '(' : '{', Color.FromRgb(255, 120, 20));
                grid.Set(cx, cy, '@', Color.FromRgb(255, 240, 170));
                grid.Set(cx + 1, cy, h(2) % 3 == 0 ? ')' : '}', Color.FromRgb(255, 120, 20));

                grid.Set(cx - 1 + h(3) % 2, cy - 1, tongues[h(4) % tongues.Length], Color.FromRgb(255, 190, 60));
                grid.Set(cx + h(5) % 2, cy - 1, tongues[h(6) % tongues.Length], Color.FromRgb(255, 210, 90));
                if (h(7) % 4 != 0)
                    grid.Set(cx + (h(8) % 3) - 1, cy - 2, h(9) % 2 == 0 ? '^' : '(', Color.FromRgb(240, 90, 24));
                if (h(10) % 3 == 0)
                    grid.Set(cx + (h(11) % 3) - 1, cy - 3, '\'', Color.FromRgb(200, 60, 20));
            }
        }

        // ───────────────────────────── Embers ─────────────────────────────

        private static void DrawEmbers(Cells grid, Layout L, double t)
        {
            for (int i = 0; i < L.EmberCount; i++)
            {
                double r1 = Rand(L.Seed, i, 1);
                double period = 2.6 + 2.6 * r1;
                double phase = Rand(L.Seed, i, 2) * period;
                double a = (t + phase) % period;
                int cycle = (int)((t + phase) / period);

                double rOrigin = Rand(L.Seed + cycle, i, 3);
                double rx = Rand(L.Seed + cycle, i, 4);
                double rs = Rand(L.Seed + cycle, i, 5);
                double x0, y0, speed;
                if (rOrigin < 0.55 && L.Torches.Count > 0)
                {
                    var (tx, ty) = L.Torches[(int)(rx * 1000) % L.Torches.Count];
                    x0 = tx + (rx - 0.5) * 3;
                    y0 = ty - 2;
                    speed = 5 + 5 * rs;
                }
                else
                {
                    x0 = rx * L.W;
                    y0 = L.H - 2;
                    speed = 3 + 4 * rs;
                }

                double drift = (Rand(L.Seed + cycle, i, 6) - 0.5) * 5;
                double x = x0 + Math.Sin(a * (1.4 + rs) + i) * (1.2 + 2 * rs) + drift * a;
                double y = y0 - a * speed;
                double life = a / period;

                char ch = life < 0.25 ? '*' : life < 0.55 ? '+' : life < 0.8 ? '.' : '`';
                Color c = life < 0.3
                    ? Lerp(Color.FromRgb(255, 240, 170), Color.FromRgb(255, 150, 40), life / 0.3)
                    : life < 0.7
                        ? Lerp(Color.FromRgb(255, 150, 40), Color.FromRgb(200, 50, 20), (life - 0.3) / 0.4)
                        : Lerp(Color.FromRgb(200, 50, 20), Color.FromRgb(60, 14, 10), (life - 0.7) / 0.3);
                grid.Set((int)Math.Round(x), (int)Math.Round(y), ch, c);
            }
        }

        // ───────────────────────────── Impact ─────────────────────────────

        private static void DrawShockwave(Cells grid, Layout L, double t, bool[] mask)
        {
            double s = t - L.ImpactTime;
            if (s < 0 || s >= ShockwaveSeconds)
                return;

            double p = s / ShockwaveSeconds;
            double r = 6 + p * L.W * 0.6;
            int points = (int)(r * 2.2) + 12;
            char ch = p < 0.3 ? '*' : p < 0.65 ? '+' : '.';
            var c = Scale(Lerp(SparkYellow, Color.FromRgb(200, 40, 20), p), 1 - 0.6 * p);
            for (int k = 0; k < points; k++)
            {
                double ang = k * Math.PI * 2 / points;
                int x = (int)Math.Round(L.TitleCenterX + Math.Cos(ang) * r);
                int y = (int)Math.Round(L.TitleCenterY + Math.Sin(ang) * r * 0.5);
                if (x < 0 || y < 0 || x >= L.W || y >= L.H || mask[y * L.W + x])
                    continue;
                grid.Set(x, y, ch, c);
            }
        }

        // ───────────────────────────── Text overlays ─────────────────────────────

        private static void DrawDecorator(Cells grid, Layout L, double t, int shakeX)
        {
            double start = L.ImpactTime - 0.05;
            if (t < start)
                return;

            double grow = Math.Min(1, (t - start) / 0.45);
            int arm = (int)Math.Round(L.DecoratorArm * grow);
            int cx = L.TitleCenterX + shakeX;
            int y = L.DecoratorY;

            var hub = (Hash(L.Seed, (int)(t * 6), 3) % 5 == 0) ? Color.FromRgb(255, 236, 160) : Gold;
            grid.Set(cx - 2, y, '{', Gold);
            grid.Set(cx, y, '+', hub);
            grid.Set(cx + 2, y, '}', Gold);
            for (int k = 0; k < arm; k++)
            {
                var c = Scale(Gold, 1 - 0.55 * k / Math.Max(1, L.DecoratorArm));
                grid.Set(cx - 4 - k, y, '=', c);
                grid.Set(cx + 4 + k, y, '=', c);
            }
            if (grow >= 1)
            {
                grid.Set(cx - 4 - arm, y, '<', Scale(Gold, 0.5));
                grid.Set(cx + 4 + arm, y, '>', Scale(Gold, 0.5));
            }
        }

        private static void DrawTagline(Cells grid, Layout L, double t)
        {
            if (t < SettleSeconds)
                return;

            int shown = Math.Min(Tagline.Length, (int)((t - SettleSeconds) * 42));
            int x0 = L.TitleCenterX - Tagline.Length / 2;
            grid.Clear(x0 - 4, L.TaglineY, Tagline.Length + 8);
            for (int i = 0; i < shown; i++)
                grid.Set(x0 + i, L.TaglineY, Tagline[i], Parchment);

            if (shown < Tagline.Length)
            {
                grid.Set(x0 + shown, L.TaglineY, '_', Gold);
            }
            else
            {
                var flank = Scale(Gold, 0.7);
                grid.Set(x0 - 3, L.TaglineY, '~', flank);
                grid.Set(x0 + Tagline.Length + 2, L.TaglineY, '~', flank);
            }
        }

        private static void DrawPrompt(Cells grid, Layout L, double t)
        {
            const double appear = SettleSeconds + 0.3;
            if (t < appear)
                return;

            double fadeIn = Math.Min(1, (t - appear) / 0.6);
            double pulse = 0.55 + 0.45 * (0.5 + 0.5 * Math.Sin((t - appear) * 3.2));
            double q = Math.Round(fadeIn * pulse * 10) / 10;
            var c = Scale(Color.FromRgb(210, 204, 196), q);
            int x0 = L.TitleCenterX - PressKeyPrompt.Length / 2;
            grid.Clear(x0 - 2, L.PromptY, PressKeyPrompt.Length + 4);
            for (int i = 0; i < PressKeyPrompt.Length; i++)
                grid.Set(x0 + i, L.PromptY, PressKeyPrompt[i], c);
        }

        // ───────────────────────────── Layout ─────────────────────────────

        private sealed class Pixel
        {
            public int X, Y, Word, Row;
            public bool TopEdge, BottomEdge;
            public double StartX, StartY, Delay, Flight, Arc;
            public double LandTime => Delay + Flight;
        }

        private readonly record struct Drip(int X, double Period, double Phase);
        private readonly record struct Eye(int X, int Y, double Period, double Phase, double Open, int Look);

        private sealed class Layout
        {
            public readonly int W, H, Seed;
            public readonly int PixelW, PixelH, ExtrudeDepth;
            public readonly List<Pixel> Pixels = new();
            public readonly int TitleMinX, TitleMaxX, TitleMinY, TitleMaxY, TitleCenterX, TitleCenterY;
            public readonly int DecoratorY, DecoratorArm, TaglineY, PromptY, FogTop, CeilingRows;
            public readonly double ImpactTime, TorchRadius;
            public readonly List<(int X, int Y)> Torches = new();
            public readonly int[] Stalactites;
            public readonly List<Drip> Drips = new();
            public readonly List<Eye> Eyes = new();
            public readonly int EmberCount;

            public Layout(int w, int h, int seed)
            {
                W = w;
                H = h;
                Seed = seed;

                const int topGap = 3, bottomGap = 2;
                int topPx = TitlePixelFont.MeasureWord(TopWord, topGap);
                int bottomPx = TitlePixelFont.MeasureWord(BottomWord, bottomGap);
                int widestPx = Math.Max(topPx, bottomPx);

                // Largest pixel (cols × rows) that leaves room for torches and text. Cells are ~1:2,
                // so 2k × k is square; 3 × 2 is slightly tall and keeps the chunky look on 1080p grids.
                var candidates = new (int W, int H)[] { (4, 2), (3, 2), (3, 1), (2, 1), (1, 1) };
                (int W, int H) pick = candidates[^1];
                foreach (var c in candidates)
                {
                    int cols = widestPx * c.W;
                    int rows = TitlePixelFont.GlyphHeight * c.H * 2 + MidGap(c.H);
                    if (cols <= w - 30 && rows <= h - 16)
                    {
                        pick = c;
                        break;
                    }
                }
                int k = pick.H;
                PixelW = pick.W;
                PixelH = pick.H;
                ExtrudeDepth = k;

                int glyphRows = TitlePixelFont.GlyphHeight * PixelH;
                int midGap = MidGap(k);
                int totalRows = glyphRows * 2 + midGap;

                CeilingRows = 4;
                FogTop = h - 7;
                int titleTop = Math.Max(CeilingRows + 2, (int)Math.Round(h * 0.40 - totalRows / 2.0));
                int bottomTop = titleTop + glyphRows + midGap;
                DecoratorY = titleTop + glyphRows + midGap / 2;

                var rng = new Random(seed);
                int centerX = w / 2;
                AddWord(TopWord, topGap, 0, centerX, titleTop, rng);
                AddWord(BottomWord, bottomGap, 1, centerX, bottomTop, rng);

                TitleMinX = int.MaxValue; TitleMinY = int.MaxValue;
                foreach (var p in Pixels)
                {
                    TitleMinX = Math.Min(TitleMinX, p.X);
                    TitleMinY = Math.Min(TitleMinY, p.Y);
                    TitleMaxX = Math.Max(TitleMaxX, p.X + PixelW - 1 + ExtrudeDepth);
                    TitleMaxY = Math.Max(TitleMaxY, p.Y + PixelH - 1 + ExtrudeDepth);
                }
                TitleCenterX = centerX;
                TitleCenterY = DecoratorY;
                DecoratorArm = Math.Max(6, Math.Min(36, (TitleMaxX - TitleMinX) / 4));

                TaglineY = Math.Min(h - 6, TitleMaxY + Math.Max(3, k + 2));
                PromptY = Math.Max(TaglineY + 2, h - 4);
                if (PromptY > h - 2) PromptY = h - 2;

                // Torches flank the title at the decorator line.
                int leftX = Math.Max(4, TitleMinX - 14);
                int rightX = Math.Min(w - 5, TitleMaxX + 14);
                int torchY = Math.Max(CeilingRows + 4, DecoratorY - 1);
                Torches.Add((leftX, torchY));
                Torches.Add((rightX, torchY));
                TorchRadius = Math.Max(18, Math.Min(34, w / 7.0));

                // Fly-in timing: left-to-right wave, top word slightly first. All land by ImpactTime.
                double span = Math.Max(1, TitleMaxX - TitleMinX);
                double latest = 0;
                foreach (var p in Pixels)
                {
                    double col = (p.X - TitleMinX) / span;
                    p.Delay = 0.1 + 0.55 * col + 0.15 * rng.NextDouble() + 0.08 * p.Word;
                    p.Flight = 0.45 + 0.15 * rng.NextDouble();
                    p.Arc = 3 + 6 * rng.NextDouble();
                    if (rng.NextDouble() < 0.5)
                    {
                        var (tx, ty) = Torches[p.X < centerX ? 0 : 1];
                        p.StartX = tx + rng.Next(-1, 2);
                        p.StartY = ty - 1;
                    }
                    else
                    {
                        p.StartX = rng.Next(0, w);
                        p.StartY = h - 1 - rng.Next(0, 3);
                    }
                    latest = Math.Max(latest, p.LandTime);
                }
                ImpactTime = latest;

                // Ceiling stalactites from smoothed noise.
                Stalactites = new int[w];
                for (int x = 0; x < w; x++)
                {
                    double n = Noise(x * 0.33, 0.5, seed + 5);
                    int len = 1 + (int)(n * 4.2);
                    if (Hash(seed, x, 900) % 7 == 0) len += 1;
                    Stalactites[x] = Math.Min(CeilingRows, len);
                }

                for (int attempt = 0; attempt < 200 && Drips.Count < 6; attempt++)
                {
                    int x = rng.Next(2, w - 2);
                    if (Stalactites[x] < 3) continue;
                    Drips.Add(new Drip(x, 2.4 + 2.4 * rng.NextDouble(), rng.NextDouble() * 5));
                }

                // Eyes lurk in the dark beyond the torch light.
                for (int attempt = 0; attempt < 400 && Eyes.Count < 4; attempt++)
                {
                    int x = rng.Next(2, w - 5);
                    int y = rng.Next(CeilingRows + 2, FogTop - 2);
                    bool farFromTorches = true;
                    foreach (var (tx, ty) in Torches)
                    {
                        double dx = x - tx, dy = (y - ty) * 2.0;
                        if (Math.Sqrt(dx * dx + dy * dy) < TorchRadius + 4) farFromTorches = false;
                    }
                    bool clearOfTitle = x + 3 < TitleMinX - 4 || x > TitleMaxX + 4 || y < TitleMinY - 2 || y > TaglineY + 1;
                    if (!farFromTorches || !clearOfTitle) continue;
                    Eyes.Add(new Eye(x, y, 8 + 7 * rng.NextDouble(), rng.NextDouble() * 15,
                        2.2 + rng.NextDouble(), rng.Next(0, 2) == 0 ? -1 : 1));
                }

                EmberCount = Math.Clamp(w / 3, 40, 90);
            }

            private static int MidGap(int k) => Math.Max(3, 2 * k + 1);

            private void AddWord(string word, int gapPx, int wordIndex, int centerX, int top, Random rng)
            {
                int widthCols = TitlePixelFont.MeasureWord(word, gapPx) * PixelW;
                int x = centerX - widthCols / 2;
                foreach (char c in word)
                {
                    var rows = TitlePixelFont.Get(c);
                    for (int r = 0; r < rows.Length; r++)
                    {
                        for (int col = 0; col < rows[r].Length; col++)
                        {
                            if (rows[r][col] != '#') continue;
                            Pixels.Add(new Pixel
                            {
                                X = x + col * PixelW,
                                Y = top + r * PixelH,
                                Word = wordIndex,
                                Row = r,
                                TopEdge = r == 0 || rows[r - 1][col] != '#',
                                BottomEdge = r == rows.Length - 1 || rows[r + 1][col] != '#',
                            });
                        }
                    }
                    x += (rows[0].Length + gapPx) * PixelW;
                }
            }
        }

        // ───────────────────────────── Cell buffer ─────────────────────────────

        private sealed class Cells
        {
            private readonly int _w, _h;
            private readonly char[] _ch;
            private readonly Color[] _color;

            public Cells(int w, int h)
            {
                _w = w;
                _h = h;
                _ch = new char[w * h];
                _color = new Color[w * h];
                Array.Fill(_ch, ' ');
            }

            public void Set(int x, int y, char ch, Color color)
            {
                if (x < 0 || y < 0 || x >= _w || y >= _h)
                    return;
                _ch[y * _w + x] = ch;
                _color[y * _w + x] = color;
            }

            public void Clear(int x, int y, int length)
            {
                for (int i = 0; i < length; i++)
                    Set(x + i, y, ' ', default);
            }

            /// <summary>Scales every glyph's color and blanks cells where <paramref name="keepClear"/> is true.</summary>
            public void Dim(double brightness, Func<int, int, bool>? keepClear)
            {
                for (int y = 0; y < _h; y++)
                {
                    for (int x = 0; x < _w; x++)
                    {
                        int i = y * _w + x;
                        if (_ch[i] == ' ')
                            continue;
                        if (keepClear != null && keepClear(x, y))
                        {
                            _ch[i] = ' ';
                            continue;
                        }
                        // Quantize so dimmed neighbours still merge into runs.
                        var c = Scale(_color[i], brightness);
                        _color[i] = Color.FromRgb((byte)(c.R & 0xFC), (byte)(c.G & 0xFC), (byte)(c.B & 0xFC));
                    }
                }
            }

            /// <summary>Collapses each row into single-color runs of non-space glyphs.</summary>
            public void EmitRuns(List<TitleSceneText> output)
            {
                var sb = new System.Text.StringBuilder();
                for (int y = 0; y < _h; y++)
                {
                    int x = 0;
                    while (x < _w)
                    {
                        int i = y * _w + x;
                        if (_ch[i] == ' ')
                        {
                            x++;
                            continue;
                        }

                        var color = _color[i];
                        int start = x;
                        sb.Clear();
                        while (x < _w && _ch[y * _w + x] != ' ' && _color[y * _w + x] == color)
                        {
                            sb.Append(_ch[y * _w + x]);
                            x++;
                        }
                        output.Add(new TitleSceneText(start, y, sb.ToString(), color));
                    }
                }
            }
        }

        // ───────────────────────────── Math helpers ─────────────────────────────

        private static int Hash(int seed, int x, int y)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (int)(h & 0x7FFFFFFF);
            }
        }

        private static double Rand(int seed, int i, int salt) => (Hash(seed, i, salt * 7919) & 0xFFFFFF) / 16777216.0;

        private static double Noise(double x, double y, int seed)
        {
            int xi = (int)Math.Floor(x), yi = (int)Math.Floor(y);
            double fx = x - xi, fy = y - yi;
            double sx = fx * fx * (3 - 2 * fx), sy = fy * fy * (3 - 2 * fy);
            double a = Rand(seed, xi * 7349 + yi, 1), b = Rand(seed, (xi + 1) * 7349 + yi, 1);
            double c = Rand(seed, xi * 7349 + yi + 1, 1), d = Rand(seed, (xi + 1) * 7349 + yi + 1, 1);
            return (a + (b - a) * sx) + ((c + (d - c) * sx) - (a + (b - a) * sx)) * sy;
        }

        private static Color Lerp(Color a, Color b, double t)
        {
            t = Math.Clamp(t, 0, 1);
            return Color.FromRgb(
                (byte)Math.Round(a.R + (b.R - a.R) * t),
                (byte)Math.Round(a.G + (b.G - a.G) * t),
                (byte)Math.Round(a.B + (b.B - a.B) * t));
        }

        private static Color Scale(Color c, double m) =>
            Color.FromRgb(
                (byte)Math.Clamp(Math.Round(c.R * m), 0, 255),
                (byte)Math.Clamp(Math.Round(c.G * m), 0, 255),
                (byte)Math.Clamp(Math.Round(c.B * m), 0, 255));
    }
}
