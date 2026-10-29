using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using RPGGame.UI.Avalonia.Canvas;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.UI.Avalonia.Feedback
{
    /// <summary>
    /// Turn lighting on the combat board: whoever acts keeps their action cards at full brightness while the other
    /// side's cards sit under a dark scrim. The hero owns the bottom strip, the enemy the top strip; the side panels
    /// are never dimmed. A room turn dims both strips a little. The board also names the card to spotlight (the
    /// attacker's lined-up action), which gets a soft glow. Hovering a dimmed strip lights it back up so it can be read.
    /// Health bars are cut out of the scrim, so both stay at full brightness.
    /// The board sets this every paint; the canvas paints it over everything.
    /// </summary>
    public static class CombatTurnSpotlight
    {
        public enum Side
        {
            None,
            Hero,
            Enemy,
            Room
        }

        /// <summary>Scrim alpha over the cards of the side that is not acting.</summary>
        internal const double ScrimMaxAlpha = 0.75;

        /// <summary>Scrim alpha over both sides' cards while the room acts.</summary>
        internal const double RoomScrimAlpha = 0.3;

        private const int FadeMs = 260;

        private static Side _side = Side.None;
        private static double _fromHero, _fromEnemy;
        private static DateTimeOffset _changedUtc;
        private static (int X, int Y, int W, int H) _enemyStrip;
        private static IReadOnlyList<(int X, int Y, int W, int H)> _enemyCards = Array.Empty<(int, int, int, int)>();
        private static IReadOnlyList<(int X, int Y, int W, int H)> _heroCards = Array.Empty<(int, int, int, int)>();
        private static (int X, int Y, int W, int H)? _focus;
        private static Color _focusColor;
        private static double _focusStrength;

        public static Side ActiveSide => _side;

        /// <summary>
        /// Lights <paramref name="side"/>; <paramref name="enemyStrip"/> is the enemy's card row this paint (hover area).
        /// A dimmed strip darkens only its cards (<paramref name="enemyCards"/>, <paramref name="heroCards"/>), not the
        /// room plate between them.
        /// </summary>
        public static void Set(Side side, (int X, int Y, int W, int H) enemyStrip,
            IReadOnlyList<(int X, int Y, int W, int H)> enemyCards, IReadOnlyList<(int X, int Y, int W, int H)> heroCards)
        {
            _enemyStrip = enemyStrip;
            _enemyCards = enemyCards;
            _heroCards = heroCards;
            if (side == _side)
                return;
            (_fromHero, _fromEnemy) = Current();
            _side = side;
            _changedUtc = DateTimeOffset.UtcNow;
        }

        /// <summary>Card to glow (grid cells), or null. <paramref name="strength"/> 0..1 scales the glow.</summary>
        public static void SetFocus((int X, int Y, int W, int H)? card, Color color, double strength)
        {
            _focus = card;
            _focusColor = color;
            _focusStrength = Math.Clamp(strength, 0.0, 1.0);
        }

        public static void Clear()
        {
            _side = Side.None;
            _fromHero = _fromEnemy = 0.0;
            _focus = null;
        }

        private static (double Hero, double Enemy) Target(Side side) => side switch
        {
            Side.Hero => (0.0, ScrimMaxAlpha),
            Side.Enemy => (ScrimMaxAlpha, 0.0),
            Side.Room => (RoomScrimAlpha, RoomScrimAlpha),
            _ => (0.0, 0.0)
        };

        private static (double Hero, double Enemy) Current()
        {
            var (hero, enemy) = Target(_side);
            double t = CombatBoardAnimation.EaseOut(CombatBoardAnimation.Progress(_changedUtc, FadeMs));
            return (CombatBoardAnimation.Lerp(_fromHero, hero, t), CombatBoardAnimation.Lerp(_fromEnemy, enemy, t));
        }

        public static void Paint(DrawingContext context, double contentWidth, double contentHeight, double charWidth, double charHeight,
            IEnumerable<CanvasProgressBar> healthBars)
        {
            if (!CombatBoardLayout.IsActive && !CombatBoardLayout.IsArena)
                return;

            if (CombatBoardAnimation.Progress(_changedUtc, FadeMs) < 1.0)
                CombatBoardAnimation.RequestFrame();
            var (hero, enemy) = Current();

            Rect Cells(double x, double y, double w, double h) => new(x * charWidth, y * charHeight, w * charWidth, h * charHeight);
            double lift = CombatBoardAnimation.EaseOut(CombatBoardAnimation.Progress(_hoverChangedUtc, HoverFadeMs));
            if (lift < 1.0)
                CombatBoardAnimation.RequestFrame();
            double cols = contentWidth / charWidth, rows = contentHeight / charHeight;
            // Pixel rects of the health bars, as the primitives renderer draws them.
            var holes = healthBars.Select(b => new Rect(b.X * charWidth, (b.Y + b.VerticalOffsetScale) * charHeight,
                b.Width * charWidth, b.HeightScale * charHeight)).ToList();
            foreach (Region region in Enum.GetValues<Region>())
            {
                double alpha = IsHeroRegion(region) ? hero : enemy;
                // The region under the pointer lights up so it can be read; the one it just left dims back.
                if (region == _hovered)
                    alpha *= 1.0 - lift;
                else if (region == _unhovered)
                    alpha *= lift;
                if (alpha <= 0.001)
                    continue;
                var scrim = Scrim(alpha);
                var cards = region switch
                {
                    Region.HeroStrip => _heroCards,
                    Region.EnemyStrip => _enemyCards,
                    _ => null
                };
                if (cards != null)
                {
                    foreach (var c in cards)
                        if (c.W > 0 && c.H > 0)
                            FillAround(context, scrim, Cells(c.X, c.Y, c.W, c.H), holes);
                    continue;
                }
                var (x, y, w, h) = Bounds(region, cols, rows);
                if (w > 0 && h > 0)
                    FillAround(context, scrim, Cells(x, y, w, h), holes);
            }

            if (_focus is { } f && _focusStrength > 0.0)
            {
                // Three widening strokes, fading outward: a glow around the card's border.
                var rect = Cells(f.X, f.Y, f.W, f.H);
                for (int i = 1; i <= 3; i++)
                {
                    byte a = (byte)Math.Round(255 * _focusStrength * (0.75 - i * 0.18));
                    var pen = new Pen(new SolidColorBrush(Color.FromArgb(a, _focusColor.R, _focusColor.G, _focusColor.B)), 2.0);
                    context.DrawRectangle(null, pen, rect.Inflate(i * 2.0));
                }
            }
        }

        /// <summary>The dimmable areas: each side's card strip (the side panels stay lit).</summary>
        private enum Region
        {
            None,
            HeroStrip,
            EnemyStrip
        }

        private const int HoverFadeMs = 140;
        private static Region _hovered = Region.None, _unhovered = Region.None;
        private static DateTimeOffset _hoverChangedUtc;

        private static bool IsHeroRegion(Region r) => r is Region.HeroStrip;

        /// <summary>Grid rect of <paramref name="region"/> (cells); the hero strip runs to the content bottom.</summary>
        private static (double X, double Y, double W, double H) Bounds(Region region, double cols, double rows)
        {
            int stripY = LayoutConstants.ACTION_INFO_Y;
            return region switch
            {
                Region.HeroStrip => (LayoutConstants.ACTION_INFO_X, stripY, LayoutConstants.ACTION_INFO_WIDTH,
                    CombatBoardLayout.IsArena ? LayoutConstants.ACTION_INFO_HEIGHT : rows - stripY),
                Region.EnemyStrip => (_enemyStrip.X, _enemyStrip.Y, _enemyStrip.W, _enemyStrip.H),
                _ => (0, 0, 0, 0)
            };
        }

        /// <summary>
        /// Pointer moved to grid cell (<paramref name="gridX"/>, <paramref name="gridY"/>). Returns true when it crossed
        /// into or out of a dimmable region, so the canvas repaints and that region fades up (or back down).
        /// </summary>
        public static bool TrackPointer(double gridX, double gridY)
        {
            var next = Region.None;
            if (CombatBoardLayout.IsActive || CombatBoardLayout.IsArena)
            {
                foreach (Region region in Enum.GetValues<Region>())
                {
                    var (x, y, w, h) = Bounds(region, double.MaxValue, double.MaxValue);
                    if (region != Region.None && gridX >= x && gridX < x + w && gridY >= y && gridY < y + h)
                    {
                        next = region;
                        break;
                    }
                }
            }
            if (next == _hovered)
                return false;
            _unhovered = _hovered;
            _hovered = next;
            _hoverChangedUtc = DateTimeOffset.UtcNow;
            return true;
        }

        /// <summary>Fills <paramref name="area"/> except where it overlaps <paramref name="holes"/> (even-odd cut-outs).</summary>
        private static void FillAround(DrawingContext context, IBrush brush, Rect area, List<Rect> holes)
        {
            GeometryGroup? group = null;
            foreach (var hole in holes)
            {
                var cut = hole.Intersect(area);
                if (cut.Width <= 0 || cut.Height <= 0)
                    continue;
                group ??= new GeometryGroup { FillRule = FillRule.EvenOdd, Children = { new RectangleGeometry(area) } };
                group.Children.Add(new RectangleGeometry(cut));
            }
            if (group == null)
                context.FillRectangle(brush, area);
            else
                context.DrawGeometry(brush, null, group);
        }

        private static SolidColorBrush Scrim(double alpha) =>
            new(Color.FromArgb((byte)Math.Round(255 * Math.Clamp(alpha, 0.0, 1.0)), 0, 0, 0));
    }
}
