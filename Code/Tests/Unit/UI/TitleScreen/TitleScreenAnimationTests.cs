using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RPGGame.Data;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.TitleScreen;

namespace RPGGame.Tests.Unit.UI.TitleScreen
{
    public static class TitleScreenAnimationTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== TitleScreen Animation Tests ===\n");

            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestIntroSequenceIncludesFadePopSettle();
            TestBootPathSkipsIntroAndUsesIdlePressKey();
            TestIdleSaturationScaleReducesSaturation();
            TestComplementaryBackgroundHueIsOpposite();
            TestPaletteShiftAndBackgroundDuringIdle();
            TestPaletteCrossfadeLerpsGlyphColors();
            TestTitleNeonPalettesPreferred();
            TestPalettePickerRejectsMonoAndSolids();
            TestPalettePickerFallbackWhenEmpty();
            TestPaletteFighterColorsAreReversed();
            TestPhasedOffsetShiftsNonWhitespaceColors();
            TestComposedIdleUsesDungeonSelectionUndulation();
            TestIdleCycleExitsOnCancellation();

            TestBase.PrintSummary("TitleScreen Animation Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestIntroSequenceIncludesFadePopSettle()
        {
            Console.WriteLine("--- Testing intro sequence phase order ---");

            var config = new TitleAnimationConfig
            {
                BlackScreenFrames = 1,
                FadeInFrames = 2,
                WhiteLightHoldFrames = 1,
                PopFrames = 1,
                SettleFrames = 2,
                FinalTransitionFrames = 2,
                FinalHoldDuration = 0
            };
            var palette = TitleIdlePalettePicker.CreateFallback();
            var animation = new TitleAnimation(config, palette);
            var phases = animation.GenerateAnimationSequence().Select(s => s.Phase).ToList();

            TestBase.AssertTrue(phases.Contains(AnimationPhase.BlackScreen),
                "Sequence should include BlackScreen",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(phases.Contains(AnimationPhase.FadeIn),
                "Sequence should include FadeIn",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(phases.Contains(AnimationPhase.Pop),
                "Sequence should include Pop",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(phases.Contains(AnimationPhase.Settle),
                "Sequence should include Settle",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(phases.Contains(AnimationPhase.FinalHold),
                "Sequence should include FinalHold",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            int firstFade = phases.IndexOf(AnimationPhase.FadeIn);
            int firstPop = phases.IndexOf(AnimationPhase.Pop);
            int firstSettle = phases.IndexOf(AnimationPhase.Settle);
            TestBase.AssertTrue(firstFade >= 0 && firstPop > firstFade && firstSettle > firstPop,
                "FadeIn should precede Pop, which should precede Settle",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var finalHold = animation.GenerateAnimationSequence()
                .Last(s => s.Phase == AnimationPhase.FinalHold);
            TestBase.AssertEqual(0, finalHold.DurationMs,
                "FinalHold should be zero-duration for immediate press-any-key",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestBootPathSkipsIntroAndUsesIdlePressKey()
        {
            Console.WriteLine("--- Testing boot path skips intro ---");

            var config = new TitleAnimationConfig { FramesPerSecond = 50 };
            var palette = new TitleIdlePalette(
                "test",
                new List<string> { "R", "O" },
                new List<string> { "O", "R" });
            var renderer = new CountingTitleRenderer();
            var controller = new TitleScreenController(config, renderer, palette);

            using var cts = new CancellationTokenSource();
            bool readyFired = false;
            var bootTask = controller.ShowAnimatedTitleScreenAsync(cts.Token, () => readyFired = true);

            Thread.Sleep(80);
            cts.Cancel();

            bool completed = bootTask.Wait(TimeSpan.FromSeconds(2));
            TestBase.AssertTrue(completed,
                "Boot idle path should exit promptly after cancellation",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(readyFired,
                "onReadyForKey should fire after first idle frame",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(renderer.RenderCount > 0,
                "Boot path should render idle frames",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(renderer.RenderWithPressKeyCount > 0,
                "Boot path should paint press-key in the same RenderFrame pass",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, renderer.PressKeyCount,
                "Boot path should not call separate ShowPressKeyMessage",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestIdleSaturationScaleReducesSaturation()
        {
            Console.WriteLine("--- Testing idle saturation scale ---");

            var codes = new List<string> { "R", "C", "O" };
            var palette = new TitleIdlePalette("sat_test", codes, codes.AsEnumerable().Reverse().ToList());

            var boosted = new TitleFrameBuilder(new TitleAnimationConfig { IdleSaturationScale = 1.5 })
                .BuildPhasedPaletteFrame(palette, 0);
            var muted = new TitleFrameBuilder(new TitleAnimationConfig { IdleSaturationScale = 0.5 })
                .BuildPhasedPaletteFrame(palette, 0);

            const int dungeonStartIndex = 17;
            var boostedColor = FirstNonWhitespaceColor(boosted.Lines[dungeonStartIndex]);
            var mutedColor = FirstNonWhitespaceColor(muted.Lines[dungeonStartIndex]);

            TestBase.AssertTrue(boostedColor.HasValue && mutedColor.HasValue,
                "Phased frames should have colored glyphs",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (boostedColor.HasValue && mutedColor.HasValue)
            {
                var (_, sBoosted, _) = ColorValidator.RgbToHsv(boostedColor.Value.R, boostedColor.Value.G, boostedColor.Value.B);
                var (_, sMuted, _) = ColorValidator.RgbToHsv(mutedColor.Value.R, mutedColor.Value.G, mutedColor.Value.B);
                TestBase.AssertTrue(sBoosted > sMuted + 0.05,
                    $"IdleSaturationScale 1.5 should be more saturated than 0.5 ({sBoosted:F3} vs {sMuted:F3})",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestComplementaryBackgroundHueIsOpposite()
        {
            Console.WriteLine("--- Testing complementary vibrant background ---");

            var accent = Avalonia.Media.Color.FromRgb(0, 200, 255); // cyan-ish
            var bg = ColorValidator.ComplementaryVibrantBackground(accent);
            var (hAccent, _, _) = ColorValidator.RgbToHsv(accent.R, accent.G, accent.B);
            var (hBg, sBg, vBg) = ColorValidator.RgbToHsv(bg.R, bg.G, bg.B);

            double hueDelta = Math.Abs(hBg - hAccent);
            if (hueDelta > 180) hueDelta = 360 - hueDelta;

            TestBase.AssertTrue(Math.Abs(hueDelta - 180) < 5,
                $"Background hue should be ~180° from accent (delta={hueDelta:F1})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(vBg > 0.5,
                $"Complementary background should be bright (V={vBg:F2})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(sBg > 0.7,
                $"Complementary background should be highly saturated (S={sBg:F2})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var palette = new TitleIdlePalette(
                "cyan_test",
                new List<string> { "neon_cyan", "neon_magenta" },
                new List<string> { "neon_magenta", "neon_cyan" });
            var fromPalette = palette.ResolveComplementaryBackground();
            TestBase.AssertTrue(fromPalette.A == 255,
                "Palette complementary background should be opaque",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            var (_, sPal, vPal) = ColorValidator.RgbToHsv(fromPalette.R, fromPalette.G, fromPalette.B);
            TestBase.AssertTrue(vPal > 0.5 && sPal > 0.7,
                "Neon palette complementary bg should be vibrant and bright",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestPaletteShiftAndBackgroundDuringIdle()
        {
            Console.WriteLine("--- Testing palette shift + black backdrop ---");

            var config = new TitleAnimationConfig
            {
                FramesPerSecond = 50,
                PaletteShiftIntervalMs = 40,
                PaletteTransitionMs = 40
            };
            var palette = new TitleIdlePalette(
                "start_palette",
                new List<string> { "R", "O" },
                new List<string> { "O", "R" });
            var renderer = new CountingTitleRenderer();
            var controller = new TitleScreenController(config, renderer, palette);

            using var cts = new CancellationTokenSource();
            var idleTask = controller.RunIdleCycleAsync(cts.Token);

            Thread.Sleep(250);
            cts.Cancel();

            bool completed = idleTask.Wait(TimeSpan.FromSeconds(2));
            TestBase.AssertTrue(completed,
                "Idle with palette shift should exit after cancel",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, renderer.BackgroundPaintCount,
                "Idle should keep a black backdrop (no complementary fill)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(renderer.ResetBackgroundCount > 0,
                "Idle should reset canvas background to black",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(
                !string.Equals(controller.Palette.TemplateName, "start_palette", StringComparison.Ordinal),
                "Palette should shift away from the starting template within the short interval",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestPaletteCrossfadeLerpsGlyphColors()
        {
            Console.WriteLine("--- Testing palette crossfade lerp ---");

            var from = new TitleIdlePalette(
                "title_neon_cyber",
                new List<string> { "neon_cyan", "neon_magenta" },
                new List<string> { "neon_magenta", "neon_cyan" });
            var to = new TitleIdlePalette(
                "title_neon_acid",
                new List<string> { "neon_lime", "neon_yellow" },
                new List<string> { "neon_yellow", "neon_lime" });

            var config = new TitleAnimationConfig { IdleSaturationScale = 1.0 };
            var builder = new TitleFrameBuilder(config);

            var at0 = builder.BuildComposedIdleFrame(to, blendFrom: from, blendProgress: 0f);
            var at1 = builder.BuildComposedIdleFrame(to, blendFrom: from, blendProgress: 1f);
            var mid = builder.BuildComposedIdleFrame(to, blendFrom: from, blendProgress: 0.5f);

            const int dungeonStartIndex = 17;
            var c0 = FirstNonWhitespaceColor(at0.Lines[dungeonStartIndex]);
            var c1 = FirstNonWhitespaceColor(at1.Lines[dungeonStartIndex]);
            var cMid = FirstNonWhitespaceColor(mid.Lines[dungeonStartIndex]);

            TestBase.AssertTrue(c0.HasValue && c1.HasValue && cMid.HasValue,
                "Crossfade frames should have colored glyphs",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (c0.HasValue && c1.HasValue && cMid.HasValue)
            {
                TestBase.AssertTrue(c0.Value.R != c1.Value.R || c0.Value.G != c1.Value.G || c0.Value.B != c1.Value.B,
                    "From and to palettes should differ at progress 0 vs 1",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Midpoint should sit between endpoints on at least one channel
                bool between =
                    (cMid.Value.R >= Math.Min(c0.Value.R, c1.Value.R) && cMid.Value.R <= Math.Max(c0.Value.R, c1.Value.R)) ||
                    (cMid.Value.G >= Math.Min(c0.Value.G, c1.Value.G) && cMid.Value.G <= Math.Max(c0.Value.G, c1.Value.G)) ||
                    (cMid.Value.B >= Math.Min(c0.Value.B, c1.Value.B) && cMid.Value.B <= Math.Max(c0.Value.B, c1.Value.B));
                TestBase.AssertTrue(between,
                    "Blend progress 0.5 should interpolate between palette colors",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestTitleNeonPalettesPreferred()
        {
            Console.WriteLine("--- Testing title_neon_* palettes preferred ---");

            ColorCodeLoader.Reload();
            ColorConfigurationLoader.Reload();
            ColorTemplateLoader.Reload();
            JsonLoader.ClearCacheForFile("ColorConfiguration.json");
            JsonLoader.ClearCacheForFile("ColorCodes.json");
            JsonLoader.ClearCacheForFile("ColorTemplates.json");

            var legacy = new ColorTemplateData
            {
                Name = "fiery",
                ShaderType = "sequence",
                Colors = new List<string> { "R", "O", "W" }
            };
            var neonA = new ColorTemplateData
            {
                Name = "title_neon_cyber",
                ShaderType = "sequence",
                Colors = new List<string> { "neon_cyan", "neon_magenta", "neon_pink" }
            };
            var neonB = new ColorTemplateData
            {
                Name = "title_neon_acid",
                ShaderType = "sequence",
                Colors = new List<string> { "neon_lime", "neon_yellow", "neon_orange" }
            };

            var preferred = TitleIdlePalettePicker.PreferTitleNeon(new[] { legacy, neonA, neonB });
            TestBase.AssertEqual(2, preferred.Count,
                "When title_neon_* templates exist, only those should be preferred",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(preferred.All(TitleIdlePalettePicker.IsTitleNeonTemplate),
                "Preferred set should be only title_neon_* templates",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var picked = TitleIdlePalettePicker.PickRandom(new[] { legacy, neonA, neonB });
            TestBase.AssertTrue(picked.TemplateName.StartsWith(TitleIdlePalettePicker.TitleNeonPrefix, StringComparison.OrdinalIgnoreCase),
                "PickRandom should choose a title_neon_* palette when available",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var fromDisk = TitleIdlePalettePicker.EnumerateEligibleTemplates();
            TestBase.AssertTrue(fromDisk.Count >= 2,
                "Loaded ColorTemplates.json should expose multiple title_neon_* palettes",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(fromDisk.All(TitleIdlePalettePicker.IsTitleNeonTemplate),
                "EnumerateEligibleTemplates should return only title_neon_* when present",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(ColorCodeLoader.HasColorCode("neon_cyan"),
                "neon_cyan color code should load from ColorCodes.json",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            var cyan = ColorCodeLoader.GetColor("neon_cyan");
            TestBase.AssertTrue(cyan.G > 200 && cyan.B > 200,
                "neon_cyan should be a bright cyan RGB",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestPalettePickerRejectsMonoAndSolids()
        {
            Console.WriteLine("--- Testing palette picker eligibility ---");

            var solid = new ColorTemplateData
            {
                Name = "solid_red",
                ShaderType = "solid",
                Colors = new List<string> { "R", "O" }
            };
            var mono = new ColorTemplateData
            {
                Name = "mono_white",
                ShaderType = "sequence",
                Colors = new List<string> { "W", "W", "W" }
            };
            var multi = new ColorTemplateData
            {
                Name = "multi_fire",
                ShaderType = "sequence",
                Colors = new List<string> { "R", "O", "Y" }
            };

            TestBase.AssertTrue(!TitleIdlePalettePicker.IsEligible(solid),
                "Solid shaders should not be eligible",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!TitleIdlePalettePicker.IsEligible(mono),
                "Mono color sequences should not be eligible",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(TitleIdlePalettePicker.IsEligible(multi),
                "Multi-color sequences should be eligible",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var picked = TitleIdlePalettePicker.PickRandom(new[] { solid, mono, multi });
            TestBase.AssertEqual("multi_fire", picked.TemplateName,
                "Picker should choose the only eligible multi-color template",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestPalettePickerFallbackWhenEmpty()
        {
            Console.WriteLine("--- Testing palette picker fallback ---");

            var fallback = TitleIdlePalettePicker.PickRandom(Array.Empty<ColorTemplateData>());
            TestBase.AssertTrue(!string.IsNullOrEmpty(fallback.TemplateName),
                "Fallback should return a template name",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(fallback.DungeonColorCodes.Count >= 2,
                "Fallback dungeon colors should have at least 2 codes",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(fallback.FighterColorCodes.Count >= 2,
                "Fallback fighter colors should have at least 2 codes",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestPaletteFighterColorsAreReversed()
        {
            Console.WriteLine("--- Testing fighter palette is reversed ---");

            var template = new ColorTemplateData
            {
                Name = "test_gradient",
                ShaderType = "sequence",
                Colors = new List<string> { "R", "O", "Y" }
            };
            var palette = TitleIdlePalettePicker.FromTemplate(template);

            TestBase.AssertEqual("R", palette.DungeonColorCodes[0],
                "Dungeon palette should start with first color",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("Y", palette.FighterColorCodes[0],
                "Fighter palette should start with reversed first color",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(
                string.Join(",", palette.DungeonColorCodes.Reverse()),
                string.Join(",", palette.FighterColorCodes),
                "Fighter colors should be exact reverse of dungeon colors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestPhasedOffsetShiftsNonWhitespaceColors()
        {
            Console.WriteLine("--- Testing phased color offset shifts glyphs ---");

            const string sample = "ABCDE";
            var codes = new List<string> { "R", "G", "B" };

            var at0 = ColorTemplateLibrary.ApplyColorSequence(sample, codes, "test", 0);
            var at1 = ColorTemplateLibrary.ApplyColorSequence(sample, codes, "test", 1);

            string FlatColors(List<ColoredText> segs)
            {
                var colors = new List<string>();
                foreach (var seg in segs)
                {
                    foreach (char _ in seg.Text ?? "")
                        colors.Add($"{seg.Color.R},{seg.Color.G},{seg.Color.B}");
                }
                return string.Join("|", colors);
            }

            string c0 = FlatColors(at0);
            string c1 = FlatColors(at1);

            TestBase.AssertEqual(sample, string.Concat(at0.Select(s => s.Text)),
                "Offset 0 should preserve text",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(sample, string.Concat(at1.Select(s => s.Text)),
                "Offset 1 should preserve text",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(c0 != c1,
                "Different colorStartOffset should change glyph colors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var config = new TitleAnimationConfig();
            var builder = new TitleFrameBuilder(config);
            var palette = new TitleIdlePalette("test_gradient", codes, codes.AsEnumerable().Reverse().ToList());
            var frame0 = builder.BuildPhasedPaletteFrame(palette, 0);
            var frame1 = builder.BuildPhasedPaletteFrame(palette, 1);

            const int dungeonStartIndex = 17;
            string line0a = Flatten(frame0.Lines[dungeonStartIndex]);
            string line0b = Flatten(frame1.Lines[dungeonStartIndex]);
            TestBase.AssertEqual(line0a, line0b,
                "Phased frames should preserve ASCII characters",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var colorsA = FirstNonWhitespaceColors(frame0.Lines[dungeonStartIndex], 3);
            var colorsB = FirstNonWhitespaceColors(frame1.Lines[dungeonStartIndex], 3);
            TestBase.AssertTrue(colorsA != colorsB,
                "Phased settle frames should shift non-whitespace colors with phase index",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestComposedIdleUsesDungeonSelectionUndulation()
        {
            Console.WriteLine("--- Testing composed idle uses dungeonSelection undulation ---");

            TestBase.AssertEqual("dungeonSelection", TitleFrameBuilder.IdleAnimationPreset,
                "Title idle should use the dungeonSelection text animation preset",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var config = new TitleAnimationConfig();
            var builder = new TitleFrameBuilder(config);
            var palette = new TitleIdlePalette(
                "test_gradient",
                new List<string> { "R", "O", "Y" },
                new List<string> { "Y", "O", "R" });

            var state = DungeonSelectionAnimationState.Instance;
            state.Reset();

            var frameA = builder.BuildComposedIdleFrame(palette, state);
            state.AdvanceUndulation();
            state.AdvanceUndulation();
            state.AdvanceUndulation();
            var frameB = builder.BuildComposedIdleFrame(palette, state);

            const int dungeonStartIndex = 17;
            TestBase.AssertEqual(
                Flatten(frameA.Lines[dungeonStartIndex]),
                Flatten(frameB.Lines[dungeonStartIndex]),
                "Composed idle should preserve DEMON ASCII across undulation",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var colorsA = FirstNonWhitespaceColors(frameA.Lines[dungeonStartIndex], 8);
            var colorsB = FirstNonWhitespaceColors(frameB.Lines[dungeonStartIndex], 8);
            TestBase.AssertTrue(colorsA != colorsB,
                "Advancing dungeon-selection undulation should change DEMON glyph brightness/colors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Decorator stays solid (not undulated) — compare two composed frames' decorator line
            const int decoratorIndex = 15 + 2 + 6 + 1;
            TestBase.AssertEqual(
                Flatten(frameA.Lines[decoratorIndex]),
                TitleArtAssets.DecoratorLine,
                "Decorator should remain static (not part of undulation)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestIdleCycleExitsOnCancellation()
        {
            Console.WriteLine("--- Testing idle cycle cancels promptly ---");

            var config = new TitleAnimationConfig { FramesPerSecond = 50 };
            var palette = new TitleIdlePalette(
                "test",
                new List<string> { "R", "O" },
                new List<string> { "O", "R" });
            var renderer = new CountingTitleRenderer();
            var controller = new TitleScreenController(config, renderer, palette);

            using var cts = new CancellationTokenSource();
            var idleTask = controller.RunIdleCycleAsync(cts.Token);

            // Let a couple frames render
            Thread.Sleep(80);
            cts.Cancel();

            bool completed = idleTask.Wait(TimeSpan.FromSeconds(2));
            TestBase.AssertTrue(completed,
                "Idle cycle should exit promptly after cancellation",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(renderer.RenderCount > 0,
                "Idle cycle should have rendered at least one frame",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(renderer.RenderWithPressKeyCount > 0,
                "Idle cycle should paint press-key in the same RenderFrame pass",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, renderer.PressKeyCount,
                "Idle cycle should not call separate ShowPressKeyMessage",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static string Flatten(List<ColoredText>? segments)
        {
            if (segments == null || segments.Count == 0)
                return string.Empty;
            return string.Concat(segments.Where(s => s?.Text != null).Select(s => s.Text));
        }

        private static Avalonia.Media.Color? FirstNonWhitespaceColor(List<ColoredText>? segments)
        {
            if (segments == null)
                return null;

            foreach (var seg in segments)
            {
                if (string.IsNullOrEmpty(seg.Text))
                    continue;
                foreach (char c in seg.Text)
                {
                    if (!char.IsWhiteSpace(c))
                        return seg.Color;
                }
            }
            return null;
        }

        private static string FirstNonWhitespaceColors(List<ColoredText>? segments, int count)
        {
            if (segments == null)
                return string.Empty;

            var parts = new List<string>();
            foreach (var seg in segments)
            {
                if (string.IsNullOrEmpty(seg.Text))
                    continue;
                foreach (char c in seg.Text)
                {
                    if (char.IsWhiteSpace(c))
                        continue;
                    parts.Add($"{seg.Color.R},{seg.Color.G},{seg.Color.B}");
                    if (parts.Count >= count)
                        return string.Join("|", parts);
                }
            }
            return string.Join("|", parts);
        }

        private sealed class CountingTitleRenderer : ITitleRenderer
        {
            private readonly HashSet<string> _backgroundKeys = new(StringComparer.Ordinal);

            public int RenderCount { get; private set; }
            public int RenderWithPressKeyCount { get; private set; }
            public int PressKeyCount { get; private set; }
            public int BackgroundPaintCount { get; private set; }
            public int ResetBackgroundCount { get; private set; }
            public int DistinctBackgroundCount => _backgroundKeys.Count;

            public void RenderFrame(TitleFrame frame, bool includePressKey = false, Avalonia.Media.Color? backgroundColor = null)
            {
                RenderCount++;
                if (includePressKey)
                    RenderWithPressKeyCount++;
                if (backgroundColor.HasValue)
                {
                    BackgroundPaintCount++;
                    var c = backgroundColor.Value;
                    _backgroundKeys.Add($"{c.R},{c.G},{c.B}");
                }
            }

            public void ResetBackground() => ResetBackgroundCount++;
            public void Clear() { }
            public void Refresh() { }
            public void ShowPressKeyMessage() => PressKeyCount++;
        }
    }
}
