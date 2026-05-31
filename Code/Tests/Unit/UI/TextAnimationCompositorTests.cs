using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame.Data;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Renderers.Menu;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.Avalonia.Settings.Helpers;
using RPGGame.UI.TextAnimation;

namespace RPGGame.Tests.Unit.UI
{
    public static class TextAnimationCompositorTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;

            TestBaseOnlyPresetReturnsUnchangedColors(ref run, ref passed, ref failed);
            TestPathIntroParityWarmCoolWhite(ref run, ref passed, ref failed);
            TestDirectGradientStackMatchesPathIntro(ref run, ref passed, ref failed);
            TestInheritBasePreservesPerCharTemplateColors(ref run, ref passed, ref failed);
            TestClampBrightnessApplied(ref run, ref passed, ref failed);
            TestMultiLayerStackOrderMatters(ref run, ref passed, ref failed);
            TestSkipWhenTemplateUndulateDisabled(ref run, ref passed, ref failed);
            TestHueAndSaturationAdjustWithMask(ref run, ref passed, ref failed);
            TestAccentHsvLayerStackedOnPathIntro(ref run, ref passed, ref failed);
            TestAccentHsvVisibleOnPathIntroWarmWhite(ref run, ref passed, ref failed);
            TestAccentHsvMaskTimingParameters(ref run, ref passed, ref failed);
            TestPreviewReadabilityBoostOnBlack(ref run, ref passed, ref failed);

            TestBase.PrintSummary(nameof(TextAnimationCompositorTests), run, passed, failed);
        }

        private static void TestBaseOnlyPresetReturnsUnchangedColors(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestBaseOnlyPresetReturnsUnchangedColors));

            var preset = new TextAnimationPresetConfig
            {
                Layers = new List<TextAnimationLayerConfig>
                {
                    new()
                    {
                        Type = "baseColor",
                        Source = new TextAnimationColorSourceConfig { Solid = "R" }
                    }
                }
            };

            var result = TextAnimationCompositor.Compose(preset, plainText: "AB");
            TestBase.AssertEqual(2, result.Count, "Should emit one segment per character", ref run, ref passed, ref failed);
            var expectedRed = ColorCodeLoader.GetColor("R");
            TestBase.AssertTrue(
                ColorValidator.AreColorsEqual(expectedRed, result[0].Color),
                "First char should stay base red",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                ColorValidator.AreColorsEqual(expectedRed, result[1].Color),
                "Second char should stay base red",
                ref run, ref passed, ref failed);

            TestBase.ClearCurrentTestName();
        }

        private static void TestPathIntroParityWarmCoolWhite(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestPathIntroParityWarmCoolWhite));

            Color warm = PreWeaponPathIntroRenderer.GetShimmerColorForCharacter(0, -Math.PI / 2.0);
            TestBase.AssertEqual(
                PreWeaponPathIntroRenderer.WarmWhite,
                warm,
                "Low shimmer phase should be warm white",
                ref run,
                ref passed,
                ref failed);

            Color cool = PreWeaponPathIntroRenderer.GetShimmerColorForCharacter(0, Math.PI / 2.0);
            TestBase.AssertEqual(
                PreWeaponPathIntroRenderer.CoolWhite,
                cool,
                "High shimmer phase should be cool white",
                ref run,
                ref passed,
                ref failed);

            TestBase.ClearCurrentTestName();
        }

        private static void TestDirectGradientStackMatchesPathIntro(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestDirectGradientStackMatchesPathIntro));

            var segments = PreWeaponPathIntroRenderer.BuildQuestLineSegments(0.0);
            string text = string.Concat(segments.Select(s => s.Text));

            TestBase.AssertEqual(
                PreWeaponPathIntroRenderer.QuestLine,
                text,
                "Compositor segments should preserve full quote",
                ref run,
                ref passed,
                ref failed);

            bool hasMultipleColors = segments.Select(s => s.Color).Distinct().Count() > 1;
            TestBase.AssertTrue(
                hasMultipleColors,
                "Compositor segments should vary color across the line",
                ref run,
                ref passed,
                ref failed);

            TestBase.ClearCurrentTestName();
        }

        private static void TestInheritBasePreservesPerCharTemplateColors(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestInheritBasePreservesPerCharTemplateColors));

            var baseSegments = new List<ColoredText>
            {
                new ColoredText("A", Colors.Red),
                new ColoredText("B", Colors.Blue)
            };

            var preset = new TextAnimationPresetConfig
            {
                InheritBaseFromSegments = true,
                Layers = new List<TextAnimationLayerConfig>
                {
                    new()
                    {
                        Type = "baseColor",
                        Source = new TextAnimationColorSourceConfig { Inherit = true }
                    }
                }
            };

            var result = TextAnimationCompositor.Compose(preset, baseSegments: baseSegments);
            TestBase.AssertTrue(ColorValidator.AreColorsEqual(Colors.Red, result[0].Color), "First inherited color", ref run, ref passed, ref failed);
            TestBase.AssertTrue(ColorValidator.AreColorsEqual(Colors.Blue, result[1].Color), "Second inherited color", ref run, ref passed, ref failed);

            TestBase.ClearCurrentTestName();
        }

        private static void TestClampBrightnessApplied(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestClampBrightnessApplied));

            var preset = new TextAnimationPresetConfig
            {
                ClampBrightness = new TextAnimationClampConfig { Min = 200, Max = 200 },
                Layers = new List<TextAnimationLayerConfig>
                {
                    new()
                    {
                        Type = "baseColor",
                        Source = new TextAnimationColorSourceConfig { Solid = "k" }
                    }
                }
            };

            var result = TextAnimationCompositor.Compose(preset, plainText: "X");
            double value = ColorValidator.GetHsvValue255(result[0].Color);
            TestBase.AssertTrue(value >= 199 && value <= 201, "Dark base should clamp up to min brightness", ref run, ref passed, ref failed);

            TestBase.ClearCurrentTestName();
        }

        private static void TestMultiLayerStackOrderMatters(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestMultiLayerStackOrderMatters));

            var baseOnly = new TextAnimationPresetConfig
            {
                Layers = new List<TextAnimationLayerConfig>
                {
                    new() { Type = "baseColor", Source = new TextAnimationColorSourceConfig { Solid = "#808080" } }
                }
            };

            var withOverlay = new TextAnimationPresetConfig
            {
                Layers = new List<TextAnimationLayerConfig>
                {
                    new() { Type = "baseColor", Source = new TextAnimationColorSourceConfig { Solid = "#808080" } },
                    new()
                    {
                        Type = "colorOverlay",
                        Gradient = new List<string> { "#000000", "#FFFFFF" },
                        Mask = new TextAnimationMaskConfig { Type = "constant", ConstantValue = 0.5 }
                    }
                }
            };

            var gray = TextAnimationCompositor.Compose(baseOnly, plainText: "X")[0].Color;
            var blended = TextAnimationCompositor.Compose(withOverlay, plainText: "X")[0].Color;

            TestBase.AssertTrue(
                !ColorValidator.AreColorsEqual(gray, blended),
                "Overlay layer should change color relative to base-only stack",
                ref run,
                ref passed,
                ref failed);

            TestBase.ClearCurrentTestName();
        }

        private static void TestSkipWhenTemplateUndulateDisabled(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestSkipWhenTemplateUndulateDisabled));

            var preset = TextAnimationPresetLoader.GetPreset("displayLogShimmer");
            var baseSegments = new List<ColoredText>
            {
                new ColoredText("Z", Colors.White, sourceTemplate: "fiery")
            };

            var withSkip = TextAnimationCompositor.Compose(
                preset,
                baseSegments: baseSegments,
                animationState: DungeonSelectionAnimationState.Instance,
                phaseOverride: 0);

            var baseOnly = TextAnimationCompositor.Compose(
                new TextAnimationPresetConfig
                {
                    InheritBaseFromSegments = true,
                    Layers = new List<TextAnimationLayerConfig>
                    {
                        new() { Type = "baseColor", Source = new TextAnimationColorSourceConfig { Inherit = true } }
                    }
                },
                baseSegments: baseSegments);

            // fiery template has undulate=false in shipped data — undulation layer skipped; colors may still differ if brightness mask active
            TestBase.AssertTrue(
                ColorValidator.AreColorsEqual(baseOnly[0].Color, withSkip[0].Color),
                "When template undulate is disabled, undulation layer should not alter base color when mask is off",
                ref run,
                ref passed,
                ref failed);

            TestBase.ClearCurrentTestName();
        }

        private static void TestHueAndSaturationAdjustWithMask(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestHueAndSaturationAdjustWithMask));

            var preset = new TextAnimationPresetConfig
            {
                Layers = new List<TextAnimationLayerConfig>
                {
                    new() { Type = "baseColor", Source = new TextAnimationColorSourceConfig { Solid = "R" } },
                    new()
                    {
                        Type = "hsvAdjust",
                        HsvAdjust = new TextAnimationHsvAdjustConfig
                        {
                            HueShift = 120,
                            SaturationScale = 2.0
                        },
                        Mask = new TextAnimationMaskConfig { Type = "constant", ConstantValue = 1.0 }
                    }
                }
            };

            var result = TextAnimationCompositor.Compose(preset, plainText: "!");
            var (h, s, _) = ColorValidator.RgbToHsv(result[0].Color.R, result[0].Color.G, result[0].Color.B);
            TestBase.AssertTrue(h > 90 && h < 150, "Hue shift should move red toward green band", ref run, ref passed, ref failed);
            TestBase.AssertTrue(s > 0.5, "Saturation scale should increase saturation", ref run, ref passed, ref failed);

            TestBase.ClearCurrentTestName();
        }

        private static void TestAccentHsvLayerStackedOnPathIntro(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestAccentHsvLayerStackedOnPathIntro));

            var preset = TextAnimationPresetUiHelper.ClonePreset(TextAnimationPresetLoader.BuiltInDefaults["pathIntro"]);
            TextAnimationPresetUiHelper.SetAccentHsv(preset, hue: 90, sat: 1.5, phaseMs: 800, charOffset: 0.5);

            var withoutAccent = TextAnimationCompositor.Compose(
                TextAnimationPresetLoader.BuiltInDefaults["pathIntro"],
                plainText: "ABC",
                phaseOverride: 0);

            var withAccent = TextAnimationCompositor.Compose(
                preset,
                plainText: "ABC",
                phaseOverride: 0);

            TestBase.AssertTrue(
                withAccent.Any(c => !ColorValidator.AreColorsEqual(c.Color, withoutAccent[0].Color)),
                "Accent HSV layer should change preview colors when hue/sat differ from identity",
                ref run,
                ref passed,
                ref failed);

            TestBase.ClearCurrentTestName();
        }

        private static void TestAccentHsvVisibleOnPathIntroWarmWhite(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestAccentHsvVisibleOnPathIntroWarmWhite));

            var preset = TextAnimationPresetUiHelper.ClonePreset(TextAnimationPresetLoader.BuiltInDefaults["pathIntro"]);
            preset.Layers!.Add(new TextAnimationLayerConfig
            {
                Type = "hsvAdjust",
                HsvAdjust = new TextAnimationHsvAdjustConfig
                {
                    HueShift = 150,
                    SaturationScale = 2.0
                },
                Mask = new TextAnimationMaskConfig { Type = "constant", ConstantValue = 1.0 }
            });

            var withoutAccent = TextAnimationCompositor.Compose(
                TextAnimationPresetLoader.BuiltInDefaults["pathIntro"],
                plainText: "Sample",
                phaseOverride: 0);

            var withAccent = TextAnimationCompositor.Compose(
                preset,
                plainText: "Sample",
                phaseOverride: 0);

            int visiblyShifted = 0;
            for (int i = 0; i < withAccent.Count && i < withoutAccent.Count; i++)
            {
                var before = withoutAccent[i].Color;
                var after = withAccent[i].Color;
                if (Math.Abs(before.R - after.R) + Math.Abs(before.G - after.G) + Math.Abs(before.B - after.B) >= 24)
                    visiblyShifted++;
            }

            TestBase.AssertTrue(
                visiblyShifted >= 2,
                "Accent hue/sat should produce clearly visible color shifts on pale pathIntro preview text",
                ref run,
                ref passed,
                ref failed);

            TestBase.ClearCurrentTestName();
        }

        private static void TestAccentHsvMaskTimingParameters(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestAccentHsvMaskTimingParameters));

            var narrowOffset = TextAnimationPresetUiHelper.ClonePreset(TextAnimationPresetLoader.BuiltInDefaults["pathIntro"]);
            TextAnimationPresetUiHelper.SetAccentHsv(narrowOffset, hue: 120, sat: 2.0, phaseMs: 500, charOffset: 0.05);

            var wideOffset = TextAnimationPresetUiHelper.ClonePreset(TextAnimationPresetLoader.BuiltInDefaults["pathIntro"]);
            TextAnimationPresetUiHelper.SetAccentHsv(wideOffset, hue: 120, sat: 2.0, phaseMs: 500, charOffset: 2.5);

            var narrow = TextAnimationCompositor.Compose(narrowOffset, plainText: "ABCDEF", phaseOverride: 0.5);
            var wide = TextAnimationCompositor.Compose(wideOffset, plainText: "ABCDEF", phaseOverride: 0.5);

            int differentPairs = 0;
            for (int i = 0; i < narrow.Count && i < wide.Count; i++)
            {
                if (!ColorValidator.AreColorsEqual(narrow[i].Color, wide[i].Color))
                    differentPairs++;
            }

            TestBase.AssertTrue(
                differentPairs >= 2,
                "Accent char offset should change per-character mask sampling",
                ref run,
                ref passed,
                ref failed);

            TestBase.ClearCurrentTestName();
        }

        private static void TestPreviewReadabilityBoostOnBlack(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestPreviewReadabilityBoostOnBlack));

            var dark = Color.FromRgb(20, 25, 40);
            var boosted = TextAnimationPreviewHelper.EnsureReadableOnBlack(dark);
            TestBase.AssertTrue(
                boosted.R > dark.R || boosted.G > dark.G || boosted.B > dark.B,
                "Dark preview colors should be lifted for the black preview bar",
                ref run,
                ref passed,
                ref failed);

            var bright = PreWeaponPathIntroRenderer.WarmWhite;
            var unchanged = TextAnimationPreviewHelper.EnsureReadableOnBlack(bright);
            TestBase.AssertEqual(bright, unchanged, "Already-bright colors should not change", ref run, ref passed, ref failed);

            TestBase.ClearCurrentTestName();
        }
    }
}
