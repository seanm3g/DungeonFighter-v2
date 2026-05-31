using System;

using System.Collections.Generic;

using Avalonia;

using Avalonia.Controls;

using Avalonia.Media;

using RPGGame.UI.Avalonia.Managers;

using RPGGame.UI.ColorSystem;

using RPGGame.UI.TextAnimation;



namespace RPGGame.UI.Avalonia.Settings.Helpers

{

    /// <summary>

    /// Renders live compositor output into a horizontal row of colored character TextBlocks.

    /// </summary>

    public static class TextAnimationPreviewHelper

    {

        public const string PreviewGlyphClass = "TextAnimationPreviewGlyph";



        /// <summary>Minimum relative luminance for glyphs on the black preview bar.</summary>

        private const double MinLuminanceOnBlack = 0.42;



        public static void RenderPreview(

            Panel previewHost,

            TextAnimationPresetConfig preset,

            string presetName,

            string sampleText,

            string? previewTemplateName = null,

            BaseAnimationState? animationState = null)

        {

            if (previewHost == null)

                return;



            previewHost.Children.Clear();

            if (string.IsNullOrEmpty(sampleText))

                return;



            var state = animationState ?? ResolveAnimationState(presetName);

            List<ColoredText>? baseSegments = null;

            string? plainText = sampleText;



            if (preset.InheritBaseFromSegments && !string.IsNullOrWhiteSpace(previewTemplateName))

            {

                baseSegments = ColorTemplateLibrary.GetTemplate(previewTemplateName, sampleText);

                plainText = null;

            }



            var segments = TextAnimationCompositor.Compose(

                preset,

                baseSegments: baseSegments,

                plainText: plainText,

                animationState: state);



            foreach (var segment in segments)

            {

                var displayColor = EnsureReadableOnBlack(segment.Color);

                var glyph = new TextBlock

                {

                    Text = segment.Text,

                    FontFamily = new FontFamily("Courier New, Consolas, monospace"),

                    FontSize = 18,

                    Margin = new Thickness(0),

                    Foreground = new SolidColorBrush(displayColor)

                };

                glyph.Classes.Add(PreviewGlyphClass);

                previewHost.Children.Add(glyph);

            }

        }



        public static Color EnsureReadableOnBlack(Color color)

        {

            if (GetRelativeLuminance(color) >= MinLuminanceOnBlack)

                return color;



            // Scale channels toward white until luminance meets the preview floor.

            Color boosted = color;

            for (int i = 0; i < 8 && GetRelativeLuminance(boosted) < MinLuminanceOnBlack; i++)

            {

                boosted = Color.FromRgb(

                    (byte)Math.Min(255, boosted.R + (255 - boosted.R) / 4),

                    (byte)Math.Min(255, boosted.G + (255 - boosted.G) / 4),

                    (byte)Math.Min(255, boosted.B + (255 - boosted.B) / 4));

            }



            return boosted;

        }



        public static BaseAnimationState ResolveAnimationState(string presetName)

        {

            return presetName.Equals("critLine", StringComparison.OrdinalIgnoreCase)

                ? CritAnimationState.Instance

                : DungeonSelectionAnimationState.Instance;

        }



        private static double GetRelativeLuminance(Color color)

        {

            static double Channel(byte c)

            {

                double s = c / 255.0;

                return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);

            }



            double r = Channel(color.R);

            double g = Channel(color.G);

            double b = Channel(color.B);

            return 0.2126 * r + 0.7152 * g + 0.0722 * b;

        }

    }

}


