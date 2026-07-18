using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Renderers.Text;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.TextAnimation;

namespace RPGGame.UI.TitleScreen
{
    /// <summary>
    /// Represents a single frame of the title screen animation
    /// Contains all the lines to be displayed at a specific moment
    /// </summary>
    public class TitleFrame
    {
        public List<ColoredText>[] Lines { get; set; }

        public TitleFrame(List<ColoredText>[] lines)
        {
            Lines = lines ?? Array.Empty<List<ColoredText>>();
        }
    }

    /// <summary>
    /// Builds title screen frames using the Builder pattern
    /// Separates frame construction logic from animation and rendering
    /// </summary>
    public class TitleFrameBuilder
    {
        /// <summary>Same preset as dungeon selection name undulation.</summary>
        public const string IdleAnimationPreset = "dungeonSelection";

        private readonly TitleAnimationConfig _config;

        public TitleFrameBuilder(TitleAnimationConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Builds a frame with both words using templates
        /// </summary>
        public TitleFrame BuildTemplateFrame(string dungeonTemplate, string fighterTemplate, int colorStartOffset = 0)
        {
            var dungeonColoredLines = ApplyTemplateToLines(TitleArtAssets.DungeonLines, dungeonTemplate, colorStartOffset);
            var fighterColoredLines = ApplyTemplateToLines(TitleArtAssets.FighterLines, fighterTemplate, colorStartOffset);
            var frameLines = BuildFrameLayout(dungeonColoredLines, fighterColoredLines);
            return new TitleFrame(frameLines);
        }

        /// <summary>
        /// Builds a static palette frame (no undulation) — used for settle / final hold.
        /// </summary>
        public TitleFrame BuildPhasedPaletteFrame(TitleIdlePalette palette, int phaseIndex = 0)
        {
            if (palette == null)
                throw new ArgumentNullException(nameof(palette));

            int offset = Math.Max(0, phaseIndex);
            var dungeonColoredLines = ColorizeSequenceLines(
                TitleArtAssets.DungeonLines,
                palette.DungeonColorCodes,
                palette.TemplateName,
                offset,
                charsPerBand: 1);
            var fighterColoredLines = ColorizeSequenceLines(
                TitleArtAssets.FighterLines,
                palette.FighterColorCodes,
                palette.TemplateName,
                offset,
                charsPerBand: 1);
            ApplyIdleSaturation(dungeonColoredLines);
            ApplyIdleSaturation(fighterColoredLines);
            var frameLines = BuildFrameLayout(dungeonColoredLines, fighterColoredLines);
            return new TitleFrame(frameLines);
        }

        /// <summary>
        /// Idle frame: palette as base colors, then dungeon-selection undulation/brightness mask.
        /// When <paramref name="blendFrom"/> is set and <paramref name="blendProgress"/> &lt; 1,
        /// DEMON/FIGHTER glyph colors lerp from that palette into <paramref name="palette"/>.
        /// Only DEMON / FIGHTER lines are composed; decorator and tagline stay solid.
        /// </summary>
        public TitleFrame BuildComposedIdleFrame(
            TitleIdlePalette palette,
            BaseAnimationState? animationState = null,
            TitleIdlePalette? blendFrom = null,
            float blendProgress = 1f)
        {
            if (palette == null)
                throw new ArgumentNullException(nameof(palette));

            var state = animationState ?? DungeonSelectionAnimationState.Instance;
            blendProgress = Math.Clamp(blendProgress, 0f, 1f);

            List<ColoredText>[] dungeonBase;
            List<ColoredText>[] fighterBase;

            if (blendFrom != null && blendProgress < 1f)
            {
                var dungeonFrom = ColorizeSequenceLines(
                    TitleArtAssets.DungeonLines,
                    blendFrom.DungeonColorCodes,
                    blendFrom.TemplateName,
                    colorStartOffset: 0,
                    charsPerBand: 1);
                var fighterFrom = ColorizeSequenceLines(
                    TitleArtAssets.FighterLines,
                    blendFrom.FighterColorCodes,
                    blendFrom.TemplateName,
                    colorStartOffset: 0,
                    charsPerBand: 1);
                var dungeonTo = ColorizeSequenceLines(
                    TitleArtAssets.DungeonLines,
                    palette.DungeonColorCodes,
                    palette.TemplateName,
                    colorStartOffset: 0,
                    charsPerBand: 1);
                var fighterTo = ColorizeSequenceLines(
                    TitleArtAssets.FighterLines,
                    palette.FighterColorCodes,
                    palette.TemplateName,
                    colorStartOffset: 0,
                    charsPerBand: 1);

                ApplyIdleSaturation(dungeonFrom);
                ApplyIdleSaturation(fighterFrom);
                ApplyIdleSaturation(dungeonTo);
                ApplyIdleSaturation(fighterTo);

                dungeonBase = LerpColoredLines(dungeonFrom, dungeonTo, blendProgress);
                fighterBase = LerpColoredLines(fighterFrom, fighterTo, blendProgress);
            }
            else
            {
                dungeonBase = ColorizeSequenceLines(
                    TitleArtAssets.DungeonLines,
                    palette.DungeonColorCodes,
                    palette.TemplateName,
                    colorStartOffset: 0,
                    charsPerBand: 1);
                fighterBase = ColorizeSequenceLines(
                    TitleArtAssets.FighterLines,
                    palette.FighterColorCodes,
                    palette.TemplateName,
                    colorStartOffset: 0,
                    charsPerBand: 1);

                ApplyIdleSaturation(dungeonBase);
                ApplyIdleSaturation(fighterBase);
            }

            var dungeonAnimated = ComposeLines(dungeonBase, lineOffsetBase: 0, state);
            var fighterAnimated = ComposeLines(fighterBase, lineOffsetBase: 100, state);

            return new TitleFrame(BuildFrameLayout(dungeonAnimated, fighterAnimated));
        }

        /// <summary>
        /// Builds a frame with both words using solid colors (for transitions)
        /// </summary>
        public TitleFrame BuildSolidColorFrame(string dungeonColor, string fighterColor)
        {
            var dungeonColoredLines = ColorizeLines(TitleArtAssets.DungeonLines, dungeonColor);
            var fighterColoredLines = ColorizeLines(TitleArtAssets.FighterLines, fighterColor);
            var frameLines = BuildFrameLayout(dungeonColoredLines, fighterColoredLines);
            return new TitleFrame(frameLines);
        }

        /// <summary>
        /// Builds a settle-frame that blends from bright hold into the idle palette.
        /// At progress &gt;= 1 uses the full palette at offset 0 (undulation starts in idle).
        /// </summary>
        public TitleFrame BuildSettleFrame(TitleIdlePalette palette, float progress)
        {
            if (palette == null)
                throw new ArgumentNullException(nameof(palette));

            progress = Math.Clamp(progress, 0f, 1f);
            if (progress >= 1.0f)
                return BuildPhasedPaletteFrame(palette, 0);

            string dungeonTarget = palette.DungeonColorCodes.Count > 0
                ? palette.DungeonColorCodes[0]
                : _config.ColorScheme.DungeonFinalColor;
            string fighterTarget = palette.FighterColorCodes.Count > 0
                ? palette.FighterColorCodes[0]
                : _config.ColorScheme.FighterFinalColor;

            var dungeonTransitionLines = ColorizeTransitionLines(
                TitleArtAssets.DungeonLines,
                _config.ColorScheme.TransitionFromColor,
                dungeonTarget,
                progress);
            var fighterTransitionLines = ColorizeTransitionLines(
                TitleArtAssets.FighterLines,
                _config.ColorScheme.TransitionFromColor,
                fighterTarget,
                progress);

            ApplyIdleSaturation(dungeonTransitionLines);
            ApplyIdleSaturation(fighterTransitionLines);

            return new TitleFrame(BuildFrameLayout(dungeonTransitionLines, fighterTransitionLines));
        }

        /// <summary>
        /// Builds a frame with transition colors (legacy path using ColorScheme finals).
        /// </summary>
        public TitleFrame BuildTransitionFrame(float progress)
        {
            var scheme = _config.ColorScheme;

            if (progress >= 1.0f)
            {
                return BuildTemplateFrame("title_dungeon_yellow_orange", "title_fighter_yellow_orange");
            }

            var dungeonTransitionLines = ColorizeTransitionLines(
                TitleArtAssets.DungeonLines,
                scheme.TransitionFromColor,
                scheme.DungeonFinalColor,
                progress);

            var fighterTransitionLines = ColorizeTransitionLines(
                TitleArtAssets.FighterLines,
                scheme.TransitionFromColor,
                scheme.FighterFinalColor,
                progress);

            var frameLines = BuildFrameLayout(dungeonTransitionLines, fighterTransitionLines);
            return new TitleFrame(frameLines);
        }

        private static List<ColoredText>[] ComposeLines(
            List<ColoredText>[] baseLines,
            int lineOffsetBase,
            BaseAnimationState animationState)
        {
            var composed = new List<ColoredText>[baseLines.Length];
            for (int i = 0; i < baseLines.Length; i++)
            {
                int lineOffset = lineOffsetBase + i;
                int elementOffset = UndulatingTextHelper.GetElementRandomOffset(lineOffset);
                composed[i] = TextAnimationCompositor.Compose(
                    IdleAnimationPreset,
                    baseSegments: baseLines[i],
                    charStartIndex: 0,
                    lineOffset: lineOffset,
                    animationState: animationState,
                    elementOffset: elementOffset);
            }
            return composed;
        }

        private List<ColoredText>[] ApplyTemplateToLines(string[] lines, string templateName, int colorStartOffset = 0)
        {
            var coloredLines = new List<ColoredText>[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                coloredLines[i] = TitleColorApplicator.ApplyTemplate(lines[i], templateName, colorStartOffset);
            }
            return coloredLines;
        }

        private List<ColoredText>[] ColorizeSequenceLines(
            string[] lines,
            IReadOnlyList<string> colorCodes,
            string sourceTemplate,
            int colorStartOffset,
            int charsPerBand)
        {
            var coloredLines = new List<ColoredText>[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                coloredLines[i] = TitleColorApplicator.ApplyColorSequence(
                    lines[i],
                    colorCodes,
                    sourceTemplate,
                    colorStartOffset,
                    charsPerBand);
            }
            return coloredLines;
        }

        /// <summary>
        /// Scales HSV saturation on DEMON / FIGHTER glyph colors only (whitespace unchanged).
        /// </summary>
        private void ApplyIdleSaturation(List<ColoredText>[] lines)
        {
            double scale = _config.IdleSaturationScale;
            if (Math.Abs(scale - 1.0) < 1e-6 || lines == null)
                return;

            for (int i = 0; i < lines.Length; i++)
            {
                var segments = lines[i];
                if (segments == null || segments.Count == 0)
                    continue;

                for (int s = 0; s < segments.Count; s++)
                {
                    var seg = segments[s];
                    if (seg == null || string.IsNullOrEmpty(seg.Text))
                        continue;

                    // Skip pure-whitespace segments; mixed segments still get scaled (glyphs dominate title art).
                    bool anyGlyph = false;
                    foreach (char c in seg.Text)
                    {
                        if (!char.IsWhiteSpace(c))
                        {
                            anyGlyph = true;
                            break;
                        }
                    }
                    if (!anyGlyph)
                        continue;

                    seg.Color = ColorValidator.ScaleSaturationHsv(seg.Color, scale);
                }
            }
        }

        /// <summary>
        /// Per-glyph RGB lerp between two identically structured ASCII lines.
        /// </summary>
        public static List<ColoredText>[] LerpColoredLines(
            List<ColoredText>[] from,
            List<ColoredText>[] to,
            float progress)
        {
            progress = Math.Clamp(progress, 0f, 1f);
            if (from == null || to == null)
                return to ?? from ?? Array.Empty<List<ColoredText>>();

            int lineCount = Math.Min(from.Length, to.Length);
            var result = new List<ColoredText>[lineCount];
            for (int i = 0; i < lineCount; i++)
                result[i] = LerpColoredLine(from[i], to[i], progress);
            return result;
        }

        private static List<ColoredText> LerpColoredLine(
            List<ColoredText>? from,
            List<ColoredText>? to,
            float progress)
        {
            if (from == null || from.Count == 0)
                return to ?? new List<ColoredText>();
            if (to == null || to.Count == 0)
                return from;

            var fromColors = ExpandPerCharColors(from);
            var toColors = ExpandPerCharColors(to);
            string text = FlattenText(from);
            if (text.Length == 0)
                return new List<ColoredText>();

            int n = Math.Min(text.Length, Math.Min(fromColors.Count, toColors.Count));
            var result = new List<ColoredText>(n);
            for (int i = 0; i < n; i++)
            {
                var color = ColorValidator.LerpRgb(fromColors[i], toColors[i], progress);
                string? source = from.Count > 0 ? from[0].SourceTemplate : to[0].SourceTemplate;
                result.Add(new ColoredText(text[i].ToString(), color, source));
            }
            return result;
        }

        private static string FlattenText(List<ColoredText> segments)
        {
            if (segments == null || segments.Count == 0)
                return string.Empty;
            var sb = new System.Text.StringBuilder();
            foreach (var seg in segments)
            {
                if (!string.IsNullOrEmpty(seg?.Text))
                    sb.Append(seg.Text);
            }
            return sb.ToString();
        }

        private static List<Color> ExpandPerCharColors(List<ColoredText> segments)
        {
            var colors = new List<Color>();
            foreach (var seg in segments)
            {
                if (string.IsNullOrEmpty(seg?.Text))
                    continue;
                for (int i = 0; i < seg.Text.Length; i++)
                    colors.Add(seg.Color);
            }
            return colors;
        }

        private List<ColoredText>[] ColorizeLines(string[] lines, string colorCode)
        {
            var coloredLines = new List<ColoredText>[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                coloredLines[i] = TitleColorApplicator.ApplySolidColor(lines[i], colorCode);
            }
            return coloredLines;
        }

        private List<ColoredText>[] ColorizeTransitionLines(
            string[] lines,
            string sourceColor,
            string targetColor,
            float progress)
        {
            var coloredLines = new List<ColoredText>[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                coloredLines[i] = TitleColorApplicator.ApplyTransitionColor(
                    lines[i],
                    sourceColor,
                    targetColor,
                    progress);
            }
            return coloredLines;
        }

        private List<ColoredText>[] BuildFrameLayout(List<ColoredText>[] dungeonLines, List<ColoredText>[] fighterLines)
        {
            var frameList = new List<List<ColoredText>>();

            for (int i = 0; i < 15; i++)
            {
                frameList.Add(new List<ColoredText>());
            }

            frameList.Add(new List<ColoredText>());
            frameList.Add(new List<ColoredText>());

            foreach (var line in dungeonLines)
            {
                frameList.Add(line ?? new List<ColoredText>());
            }

            frameList.Add(new List<ColoredText>());
            var decoratorSegments = TitleColorApplicator.ApplySolidColor(TitleArtAssets.DecoratorLine, "Y");
            frameList.Add(decoratorSegments);
            frameList.Add(new List<ColoredText>());

            foreach (var line in fighterLines)
            {
                frameList.Add(line ?? new List<ColoredText>());
            }

            frameList.Add(new List<ColoredText>());
            frameList.Add(new List<ColoredText>());
            frameList.Add(new List<ColoredText>());

            var taglineSegments = TitleColorApplicator.ApplySolidColor(TitleArtAssets.Tagline, "Y");
            frameList.Add(taglineSegments);

            frameList.Add(new List<ColoredText>());
            frameList.Add(new List<ColoredText>());
            frameList.Add(new List<ColoredText>());

            return frameList.ToArray();
        }
    }
}
