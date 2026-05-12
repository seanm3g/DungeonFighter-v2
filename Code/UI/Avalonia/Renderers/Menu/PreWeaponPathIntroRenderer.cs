using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia.Renderers.Menu
{
    /// <summary>
    /// Renders the short path-choice interstitial after Training Ground completion.
    /// </summary>
    public class PreWeaponPathIntroRenderer
    {
        public const string QuestLine = "The quest to find yourself begins when you choose a path...";

        internal static readonly Color WarmWhite = Color.FromRgb(255, 244, 220);
        internal static readonly Color CoolWhite = Color.FromRgb(226, 241, 255);

        private const double CharacterPhaseOffset = 0.36;
        private const double PhaseMillisecondsDivisor = 320.0;

        private readonly ICanvasTextManager textManager;

        public PreWeaponPathIntroRenderer(GameCanvasControl canvas, ICanvasTextManager textManager)
        {
            _ = canvas ?? throw new ArgumentNullException(nameof(canvas));
            this.textManager = textManager ?? throw new ArgumentNullException(nameof(textManager));
        }

        public int RenderPreWeaponPathIntroContent(int x, int y, int width, int height, Character _)
        {
            int lineY = y + Math.Max(0, height / 2);
            int lineX = MenuLayoutCalculator.CalculateCenteredTextX(x, width, QuestLine.Length);

            textManager.WriteLineColoredSegments(BuildQuestLineSegments(GetCurrentPhase()), lineX, lineY);
            return 1;
        }

        internal static List<ColoredText> BuildQuestLineSegments(double phase)
        {
            var segments = new List<ColoredText>(QuestLine.Length);
            for (int i = 0; i < QuestLine.Length; i++)
            {
                segments.Add(new ColoredText(
                    QuestLine[i].ToString(),
                    GetShimmerColorForCharacter(i, phase),
                    sourceTemplate: null,
                    colorReadyForCanvas: true));
            }

            return segments;
        }

        internal static Color GetShimmerColorForCharacter(int characterIndex, double phase)
        {
            double t = (Math.Sin(phase + characterIndex * CharacterPhaseOffset) + 1.0) / 2.0;
            return Interpolate(WarmWhite, CoolWhite, t);
        }

        internal static Color Interpolate(Color start, Color end, double t)
        {
            t = Math.Clamp(t, 0.0, 1.0);
            return Color.FromRgb(
                (byte)Math.Round(start.R + (end.R - start.R) * t),
                (byte)Math.Round(start.G + (end.G - start.G) * t),
                (byte)Math.Round(start.B + (end.B - start.B) * t));
        }

        private static double GetCurrentPhase()
            => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / PhaseMillisecondsDivisor;
    }
}
