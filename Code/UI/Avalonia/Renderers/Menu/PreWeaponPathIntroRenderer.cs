using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.TextAnimation;

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

        private const double PhaseMillisecondsDivisor = 320.0;

        private readonly GameCanvasControl canvas;
        private readonly ICanvasTextManager textManager;

        public PreWeaponPathIntroRenderer(GameCanvasControl canvas, ICanvasTextManager textManager)
        {
            this.canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            this.textManager = textManager ?? throw new ArgumentNullException(nameof(textManager));
        }

        public int RenderPreWeaponPathIntroContent(int x, int y, int width, int height, Character _)
        {
            int lineY = y + Math.Max(0, height / 2);
            int lineX = MenuLayoutCalculator.CalculateCenteredTextX(x, width, QuestLine.Length);

            textManager.WriteLineColoredSegments(BuildQuestLineSegments(GetCurrentPhase()), lineX, lineY);

            string continuePrompt = UIConstants.Messages.PressAnyKey;
            int promptY = lineY + 2;
            int promptX = MenuLayoutCalculator.CalculateCenteredTextX(x, width, continuePrompt.Length);
            canvas.AddText(promptX, promptY, continuePrompt, AsciiArtAssets.Colors.Gray);

            return 3;
        }

        internal static List<ColoredText> BuildQuestLineSegments(double phase)
        {
            return TextAnimationCompositor.Compose(
                "pathIntro",
                plainText: QuestLine,
                phaseOverride: phase);
        }

        internal static Color GetShimmerColorForCharacter(int characterIndex, double phase)
        {
            if (characterIndex < 0 || characterIndex >= QuestLine.Length)
                return WarmWhite;

            var segments = TextAnimationCompositor.Compose(
                "pathIntro",
                plainText: QuestLine[characterIndex].ToString(),
                charStartIndex: characterIndex,
                phaseOverride: phase);
            return segments.Count > 0 ? segments[0].Color : WarmWhite;
        }

        internal static Color Interpolate(Color start, Color end, double t)
        {
            return ColorValidator.LerpRgb(start, end, t);
        }

        private static double GetCurrentPhase()
            => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / PhaseMillisecondsDivisor;
    }
}
