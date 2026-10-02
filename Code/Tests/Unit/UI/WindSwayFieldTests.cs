using System;
using System.Threading;
using Avalonia;
using RPGGame;
using RPGGame.Tests;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Canvas;
using RPGGame.UI.Avalonia.Effects;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Unit tests for near→far gradient wake + rear bias + trail (<see cref="WindSwayField"/>).
    /// </summary>
    public static class WindSwayFieldTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== WindSwayField Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            // Distortion defaults on (F6); pin enabled for physics tests, restore after.
            bool prevDistortion = DeveloperModeState.AreDistortionEffectsEnabled;
            DeveloperModeState.SetDistortionEffectsEnabled(true);
            try
            {
                TestImpulseBuildsWind();
                TestDisabledIgnoresImpulse();
                TestRuntimeDistortionOffIgnoresImpulse();
                TestDecaySettlesWind();
                TestSampleOffsetZeroWhenIdle();
                TestSampleOffsetNonZeroNearMouse();
                TestIdleSitStillClearsOffsetNearMouse();
                TestIdleVisualRestClearsOffsetBeforeHardZero();
                TestMotionDeadzoneIgnoresJitter();
                TestWakeFalloffFarIsZero();
                TestFasterMouseLargerNearOffset();
                TestTrailLingersAfterMouseLeaves();
                TestTrailExpires();
                TestNearStrongerThanFar();
                TestRearStrongerThanFront();
                TestSidePanelMembership();
                TestSectionHeaderDistortionUnitDetection();
                TestMaxOffsetRespectsConfigNearMouse();
                TestWakeDebugEllipseUsesRadius();
                TestWakeEllipseRotatesWithMouseMotion();
                TestTrackMouseUpdatesFacingWithoutWind();
                TestWakeDebugTransformKeepsCenter();
                TestWakeDebugToggleDoesNotAffectSample();
                TestChromaticFringeZeroWhenIdle();
                TestChromaticFringeAlongOffset();
                TestChromaticFringeZeroSpread();
                TestChromaticOpacityToAlpha();
                TestChromaticConfigDefaults();
                TestMayAffectTextUsesWakeBounds();
                TestFrameActiveExposesBeginFrameSnapshot();
                TestPerfDefaultsAreLeaner();
            }
            finally
            {
                DeveloperModeState.SetDistortionEffectsEnabled(prevDistortion);
            }

            TestBase.PrintSummary("WindSwayField Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static WindSwayConfig BaseCfg() => new WindSwayConfig
        {
            Enabled = true,
            ImpulseGain = 1.0,
            MaxWind = 5,
            DecayPerSecond = 0,
            MaxOffsetFraction = 0.3,
            VerticalScale = 1.0,
            WakeRadiusCells = 20,
            NearInfluence = 1.0,
            FarInfluence = 0.25,
            WakeRearBias = 0,
            WakeFalloffPower = 1,
            SpeedReferenceCellsPerSec = 20,
            TrailLifetimeSeconds = 2,
            TrailMinSpacingCells = 0.5,
            WaveLength = 1000
        };

        private static void TestImpulseBuildsWind()
        {
            Console.WriteLine("--- Impulse builds wind ---");
            var field = new WindSwayField();
            field.ApplyConfig(new WindSwayConfig
            {
                Enabled = true,
                ImpulseGain = 1.0,
                MaxWind = 10,
                DecayPerSecond = 0
            });

            field.PushFromMouseDelta(0, 0, 10, 16);
            field.PushFromMouseDelta(50, 0, 10, 16, dtOverrideSeconds: 0.1);

            var (wx, wy) = field.GetWindForTests();
            TestBase.AssertTrue(wx > 1.0,
                $"wind X should grow from mouse delta (got {wx})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(Math.Abs(wy) < 0.01,
                $"wind Y should stay ~0 for horizontal move (got {wy})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(field.IsActive,
                "field should be active after impulse",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestDisabledIgnoresImpulse()
        {
            Console.WriteLine("\n--- Disabled ignores impulse ---");
            var field = new WindSwayField();
            field.ApplyConfig(new WindSwayConfig { Enabled = false, ImpulseGain = 1.0 });
            field.PushFromMouseDelta(0, 0, 10, 16);
            field.PushFromMouseDelta(100, 0, 10, 16);
            var (wx, _) = field.GetWindForTests();
            TestBase.AssertTrue(Math.Abs(wx) < 1e-6,
                "disabled field should not accumulate wind",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!field.IsActive,
                "disabled field should not be active",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRuntimeDistortionOffIgnoresImpulse()
        {
            Console.WriteLine("\n--- F6 runtime distortion off ignores impulse ---");
            bool prev = DeveloperModeState.AreDistortionEffectsEnabled;
            try
            {
                DeveloperModeState.SetDistortionEffectsEnabled(false);
                var field = new WindSwayField();
                field.ApplyConfig(new WindSwayConfig
                {
                    Enabled = true,
                    ImpulseGain = 1.0,
                    MaxWind = 10,
                    DecayPerSecond = 0
                });
                field.PushFromMouseDelta(0, 0, 10, 16);
                field.PushFromMouseDelta(100, 0, 10, 16, dtOverrideSeconds: 0.1);
                var (wx, _) = field.GetWindForTests();
                TestBase.AssertTrue(Math.Abs(wx) < 1e-6,
                    "runtime-off field should not accumulate wind",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(!field.IsActive,
                    "runtime-off field should not be active",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                DeveloperModeState.SetDistortionEffectsEnabled(prev);
            }
        }

        private static void TestDecaySettlesWind()
        {
            Console.WriteLine("\n--- Decay settles wind ---");
            var field = new WindSwayField();
            field.ApplyConfig(new WindSwayConfig
            {
                Enabled = true,
                ImpulseGain = 1.0,
                MaxWind = 10,
                DecayPerSecond = 20,
                TrailLifetimeSeconds = 0.01
            });
            field.PushFromMouseDelta(0, 0, 10, 16);
            field.PushFromMouseDelta(40, 0, 10, 16, dtOverrideSeconds: 0.05);
            var (before, _) = field.GetWindForTests();
            Thread.Sleep(200);
            var (after, _) = field.GetWindForTests();
            TestBase.AssertTrue(before > 0.5,
                $"expected strong wind before decay (got {before})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(after < before * 0.5,
                $"wind should decay substantially ({before} → {after})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSampleOffsetZeroWhenIdle()
        {
            Console.WriteLine("\n--- Sample offset zero when idle ---");
            var field = new WindSwayField();
            field.Reset();
            var (ox, oy) = field.SampleOffset(2, 3, 0, 10, 16);
            TestBase.AssertTrue(Math.Abs(ox) < 1e-6 && Math.Abs(oy) < 1e-6,
                "idle sample should be zero",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSampleOffsetNonZeroNearMouse()
        {
            Console.WriteLine("\n--- Sample offset non-zero near mouse ---");
            var field = new WindSwayField();
            var cfg = BaseCfg();
            cfg.WakeRadiusCells = 6;
            cfg.MaxOffsetFraction = 0.2;
            cfg.SpeedReferenceCellsPerSec = 40;
            field.ApplyConfig(cfg);
            field.PushFromMouseDelta(0, 0, 10, 16);
            field.PushFromMouseDelta(30, 16, 10, 16, dtOverrideSeconds: 0.05);
            var (ox, oy) = field.SampleOffset(2, 1, 0, 10, 16);
            TestBase.AssertTrue(Math.Abs(ox) + Math.Abs(oy) > 0.05,
                $"expected non-zero sway near mouse (ox={ox}, oy={oy})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(field.GetSpeedForTests() > 1.0,
                $"expected measurable speed (got {field.GetSpeedForTests()})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestIdleSitStillClearsOffsetNearMouse()
        {
            Console.WriteLine("\n--- Idle sit-still clears offset near mouse ---");
            const double charW = 10;
            const double charH = 16;
            var field = new WindSwayField();
            field.ApplyConfig(new WindSwayConfig
            {
                Enabled = true,
                ImpulseGain = 1.0,
                MaxWind = 10,
                DecayPerSecond = 3.0,
                MaxOffsetFraction = 0.55,
                VerticalScale = 1.0,
                WakeRadiusCells = 12,
                NearInfluence = 1.0,
                FarInfluence = 0.35,
                WakeRearBias = 0,
                SpeedReferenceCellsPerSec = 20,
                // Long trail so residual would linger without idle snap/clear.
                TrailLifetimeSeconds = 2.0,
                TrailMaxPoints = 12,
                TrailMinSpacingCells = 1.0,
                ChromaticAberrationEnabled = true,
                ChromaticSpreadFraction = 0.29,
                ChromaticOpacity = 0.58
            });

            field.PushFromMouseDelta(100, 160, charW, charH);
            field.PushFromMouseDelta(140, 160, charW, charH, dtOverrideSeconds: 0.05);
            var (beforeOx, beforeOy) = field.SampleOffset(12, 10, 0, charW, charH);
            TestBase.AssertTrue(Math.Abs(beforeOx) + Math.Abs(beforeOy) > 0.05,
                $"expected sway right after motion (ox={beforeOx}, oy={beforeOy})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Past idle grace → hard rest under the still cursor (no leftover CA smear).
            Thread.Sleep(250);
            TestBase.AssertTrue(field.GetIdleSecondsForTests() >= WindSwayField.IdleGraceSeconds,
                $"expected idle past grace after sleep (idleFor={field.GetIdleSecondsForTests()}, grace={WindSwayField.IdleGraceSeconds})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(!field.IsActive,
                "field should be inactive after sitting still",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(field.GetTrailCountForTests() == 0,
                $"trail should clear when idle at rest (got {field.GetTrailCountForTests()})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            var (wx, wy) = field.GetWindForTests();
            TestBase.AssertTrue(Math.Abs(wx) < 1e-6 && Math.Abs(wy) < 1e-6,
                $"wind should snap to zero when idle (wx={wx}, wy={wy})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            var (ox, oy) = field.SampleOffset(12, 10, 0, charW, charH);
            TestBase.AssertTrue(Math.Abs(ox) < 1e-6 && Math.Abs(oy) < 1e-6,
                $"sit-still near mouse should fully rest (ox={ox}, oy={oy})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestIdleVisualRestClearsOffsetBeforeHardZero()
        {
            Console.WriteLine("\n--- Idle visual rest clears offset before hard-zero ---");
            const double charW = 10;
            const double charH = 16;
            var field = new WindSwayField();
            field.ApplyConfig(new WindSwayConfig
            {
                Enabled = true,
                ImpulseGain = 1.0,
                MaxWind = 10,
                DecayPerSecond = 0.01, // barely decays — visual rest must zero without hard-clear
                MaxOffsetFraction = 0.55,
                VerticalScale = 1.0,
                WakeRadiusCells = 12,
                NearInfluence = 1.0,
                FarInfluence = 0.35,
                WakeRearBias = 0,
                SpeedReferenceCellsPerSec = 20,
                TrailLifetimeSeconds = 2.0,
                TrailMaxPoints = 12,
                TrailMinSpacingCells = 1.0,
                ChromaticAberrationEnabled = true
            });

            field.PushFromMouseDelta(100, 160, charW, charH);
            field.PushFromMouseDelta(140, 160, charW, charH, dtOverrideSeconds: 0.05);
            var (beforeOx, beforeOy) = field.SampleOffset(12, 10, 0, charW, charH);
            TestBase.AssertTrue(Math.Abs(beforeOx) + Math.Abs(beforeOy) > 0.05,
                $"expected sway right after motion (ox={beforeOx}, oy={beforeOy})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Past visual-rest window but intentionally before hard idle grace (no Thread.Sleep flake).
            double visualIdle = WindSwayField.IdleVisualRestSeconds + 0.01;
            TestBase.AssertTrue(visualIdle < WindSwayField.IdleGraceSeconds,
                "test idle must sit between visual rest and hard grace",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            field.SetIdleSecondsForTests(visualIdle);

            TestBase.AssertTrue(field.GetIdleSecondsForTests() >= WindSwayField.IdleVisualRestSeconds,
                $"expected idle past visual rest (idleFor={field.GetIdleSecondsForTests()})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(field.GetIdleSecondsForTests() < WindSwayField.IdleGraceSeconds,
                $"expected still inside hard-grace window (idleFor={field.GetIdleSecondsForTests()})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var (ox, oy) = field.SampleOffset(12, 10, 0, charW, charH);
            TestBase.AssertTrue(Math.Abs(ox) < 1e-6 && Math.Abs(oy) < 1e-6,
                $"visual rest should zero offsets before hard-zero (ox={ox}, oy={oy})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMotionDeadzoneIgnoresJitter()
        {
            Console.WriteLine("\n--- Motion deadzone ignores jitter ---");
            var field = new WindSwayField();
            field.ApplyConfig(new WindSwayConfig
            {
                Enabled = true,
                ImpulseGain = 1.0,
                MaxWind = 10,
                DecayPerSecond = 0
            });

            field.PushFromMouseDelta(0, 0, 10, 16);
            // Sub-deadzone jitter must not invent wind.
            field.PushFromMouseDelta(WindSwayField.MotionDeadzonePixels * 0.5, 0, 10, 16, dtOverrideSeconds: 0.016);
            var (jitterWx, _) = field.GetWindForTests();
            TestBase.AssertTrue(Math.Abs(jitterWx) < 1e-6,
                $"deadzone jitter should not build wind (got {jitterWx})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            field.PushFromMouseDelta(30, 0, 10, 16, dtOverrideSeconds: 0.05);
            var (realWx, _) = field.GetWindForTests();
            TestBase.AssertTrue(realWx > 0.5,
                $"real motion past deadzone should build wind (got {realWx})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestWakeFalloffFarIsZero()
        {
            Console.WriteLine("\n--- Wake falloff far is zero ---");
            var field = new WindSwayField();
            var cfg = BaseCfg();
            cfg.WakeRadiusCells = 6;
            cfg.FarInfluence = 0.2;
            field.ApplyConfig(cfg);
            field.PushFromMouseDelta(50, 80, 10, 16);
            field.PushFromMouseDelta(60, 80, 10, 16, dtOverrideSeconds: 0.05);

            var (nearOx, nearOy) = field.SampleOffset(5, 5, 0, 10, 16);
            var (farOx, farOy) = field.SampleOffset(40, 30, 0, 10, 16);

            TestBase.AssertTrue(Math.Abs(nearOx) + Math.Abs(nearOy) > 0.02,
                $"near-field should sway (ox={nearOx}, oy={nearOy})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(Math.Abs(farOx) < 1e-6 && Math.Abs(farOy) < 1e-6,
                $"far beyond wake radius should be zero (ox={farOx}, oy={farOy})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestFasterMouseLargerNearOffset()
        {
            Console.WriteLine("\n--- Faster mouse → larger near offset ---");
            const double charW = 10;
            const double charH = 16;

            WindSwayConfig MakeCfg() => new WindSwayConfig
            {
                Enabled = true,
                ImpulseGain = 1.0,
                MaxWind = 10,
                DecayPerSecond = 0,
                MaxOffsetFraction = 0.2,
                VerticalScale = 1.0,
                WakeRadiusCells = 12,
                NearInfluence = 1.0,
                FarInfluence = 0.2,
                WakeRearBias = 0,
                WakeFalloffPower = 1,
                SpeedReferenceCellsPerSec = 50,
                TrailLifetimeSeconds = 2,
                WaveLength = 1000
            };

            var slow = new WindSwayField();
            slow.ApplyConfig(MakeCfg());
            slow.PushFromMouseDelta(100, 160, charW, charH);
            slow.PushFromMouseDelta(120, 160, charW, charH, dtOverrideSeconds: 0.5);

            var fast = new WindSwayField();
            fast.ApplyConfig(MakeCfg());
            fast.PushFromMouseDelta(100, 160, charW, charH);
            fast.PushFromMouseDelta(120, 160, charW, charH, dtOverrideSeconds: 0.05);

            var (slowOx, slowOy) = slow.SampleOffset(11, 10, 0, charW, charH);
            var (fastOx, fastOy) = fast.SampleOffset(11, 10, 0, charW, charH);
            double slowMag = Math.Abs(slowOx) + Math.Abs(slowOy);
            double fastMag = Math.Abs(fastOx) + Math.Abs(fastOy);

            TestBase.AssertTrue(fastMag > slowMag * 1.5,
                $"fast wake should exceed slow ({fastMag} vs {slowMag})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestTrailLingersAfterMouseLeaves()
        {
            Console.WriteLine("\n--- Trail lingers after mouse leaves ---");
            const double charW = 10;
            const double charH = 16;
            var field = new WindSwayField();
            var cfg = BaseCfg();
            cfg.WakeRadiusCells = 8;
            cfg.TrailMinSpacingCells = 1.0;
            field.ApplyConfig(cfg);

            field.PushFromMouseDelta(50, 80, charW, charH);
            field.PushFromMouseDelta(60, 80, charW, charH, dtOverrideSeconds: 0.05);
            field.PushFromMouseDelta(500, 500, charW, charH, dtOverrideSeconds: 0.05);

            TestBase.AssertTrue(field.GetTrailCountForTests() >= 2,
                $"expected trail points after path (got {field.GetTrailCountForTests()})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var (ox, oy) = field.SampleOffset(5, 5, 0, charW, charH);
            TestBase.AssertTrue(Math.Abs(ox) + Math.Abs(oy) > 0.02,
                $"stale trail location should still sway (ox={ox}, oy={oy})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestTrailExpires()
        {
            Console.WriteLine("\n--- Trail expires ---");
            const double charW = 10;
            const double charH = 16;
            var field = new WindSwayField();
            var cfg = BaseCfg();
            cfg.WakeRadiusCells = 8;
            cfg.TrailLifetimeSeconds = 0.08;
            cfg.TrailMinSpacingCells = 1.0;
            field.ApplyConfig(cfg);

            field.PushFromMouseDelta(50, 80, charW, charH);
            field.PushFromMouseDelta(60, 80, charW, charH, dtOverrideSeconds: 0.02);
            field.PushFromMouseDelta(500, 500, charW, charH, dtOverrideSeconds: 0.02);

            Thread.Sleep(150);

            TestBase.AssertTrue(field.GetTrailCountForTests() == 0,
                $"trail should be empty after lifetime (got {field.GetTrailCountForTests()})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var (ox, oy) = field.SampleOffset(5, 5, 0, charW, charH);
            TestBase.AssertTrue(Math.Abs(ox) < 1e-6 && Math.Abs(oy) < 1e-6,
                $"expired trail location should be still (ox={ox}, oy={oy})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestNearStrongerThanFar()
        {
            Console.WriteLine("\n--- Near stronger than far (gradient) ---");
            const double charW = 10;
            const double charH = 16;
            var field = new WindSwayField();
            var cfg = BaseCfg();
            cfg.WakeRadiusCells = 20;
            cfg.NearInfluence = 1.0;
            cfg.FarInfluence = 0.2;
            cfg.WakeRearBias = 0;
            field.ApplyConfig(cfg);

            // Mouse at cell (~10, 10)
            field.PushFromMouseDelta(95, 155, charW, charH);
            field.PushFromMouseDelta(100, 160, charW, charH, dtOverrideSeconds: 0.05);

            var (nearOx, nearOy) = field.SampleOffset(10, 10, 0, charW, charH);
            var (midOx, midOy) = field.SampleOffset(18, 10, 0, charW, charH); // ~8 cells out
            double nearMag = Math.Abs(nearOx) + Math.Abs(nearOy);
            double midMag = Math.Abs(midOx) + Math.Abs(midOy);

            TestBase.AssertTrue(nearMag > midMag * 1.2,
                $"near should exceed mid-radius ({nearMag} vs {midMag})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRearStrongerThanFront()
        {
            Console.WriteLine("\n--- Rear stronger than front (wake bias) ---");
            const double charW = 10;
            const double charH = 16;
            var field = new WindSwayField();
            var cfg = BaseCfg();
            cfg.WakeRadiusCells = 20;
            cfg.NearInfluence = 1.0;
            cfg.FarInfluence = 1.0; // isolate directional bias
            cfg.WakeFalloffPower = 1;
            cfg.WakeRearBias = 0.8;
            // Keep only the live cursor sample so a zero-wind first deposit cannot win the rear glyph.
            cfg.TrailMinSpacingCells = 100;
            field.ApplyConfig(cfg);

            // Seed left, then move rightward so wind X > 0; mouse at glyph-center x of cell 14.
            // Rear cell 10 (105) and front cell 18 (185) are each 4 cells from mouse at 145.
            field.PushFromMouseDelta(65, 160, charW, charH);
            field.PushFromMouseDelta(145, 160, charW, charH, dtOverrideSeconds: 0.05);

            var (rearOx, rearOy) = field.SampleOffset(10, 10, 0, charW, charH); // behind
            var (frontOx, frontOy) = field.SampleOffset(18, 10, 0, charW, charH); // in front
            double rearMag = Math.Abs(rearOx) + Math.Abs(rearOy);
            double frontMag = Math.Abs(frontOx) + Math.Abs(frontOy);

            TestBase.AssertTrue(rearMag > frontMag * 1.3,
                $"rear wake should exceed front ({rearMag} vs {frontMag})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSectionHeaderDistortionUnitDetection()
        {
            Console.WriteLine("\n--- Section header distortion unit detection ---");
            string rightHeader = AsciiArtAssets.UIText.CreateHeader("LOCATION");
            TestBase.AssertTrue(CanvasPrimitivesRenderer.ShouldRenderHeaderAsDistortionUnit(rightHeader),
                "CreateHeader lines should render as one distortion unit",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(CanvasPrimitivesRenderer.ShouldRenderHeaderAsDistortionUnit("====  STATS  ===="),
                "left panel ==== headers should render as one distortion unit",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!CanvasPrimitivesRenderer.ShouldRenderHeaderAsDistortionUnit("Dungeon:"),
                "normal labels should use per-glyph distortion",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSidePanelMembership()
        {
            Console.WriteLine("\n--- Side panel membership ---");
            LayoutConstants.UpdateGridDimensions(210, 52);
            LayoutConstants.UpdateEffectiveVisibleWidth(210 * 8, 8);

            TestBase.AssertTrue(CanvasPrimitivesRenderer.IsInSidePanel(LayoutConstants.LEFT_PANEL_X + 1),
                "left panel cell should count as side panel",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(CanvasPrimitivesRenderer.IsInSidePanel(LayoutConstants.RIGHT_PANEL_X + 1),
                "right panel cell should count as side panel",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!CanvasPrimitivesRenderer.IsInSidePanel(LayoutConstants.CENTER_PANEL_X + 5),
                "center panel cell should not count as side panel",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMaxOffsetRespectsConfigNearMouse()
        {
            Console.WriteLine("\n--- Max offset respects config near mouse ---");
            var field = new WindSwayField();
            var cfg = BaseCfg();
            cfg.MaxOffsetFraction = 0.1;
            cfg.WakeRadiusCells = 12;
            cfg.NearInfluence = 1.0;
            cfg.FarInfluence = 0.3;
            cfg.WakeRearBias = 0;
            cfg.SpeedReferenceCellsPerSec = 20;
            field.ApplyConfig(cfg);

            field.PushFromMouseDelta(190, 150, 10, 16);
            field.PushFromMouseDelta(200, 160, 10, 16, dtOverrideSeconds: 0.02);

            double charW = 10;
            double charH = 16;
            // dir-only offset ≤ MaxOffsetFraction × cell size
            double maxPx = 0.1 * Math.Max(charW, charH) * 1.0;
            for (int i = 0; i < 5; i++)
            {
                var (ox, oy) = field.SampleOffset(19 + i, 10, 0, charW, charH);
                TestBase.AssertTrue(Math.Abs(ox) <= maxPx && Math.Abs(oy) <= maxPx,
                    $"offset within bound near mouse (ox={ox}, oy={oy}, max={maxPx})",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestWakeDebugEllipseUsesRadius()
        {
            Console.WriteLine("\n--- Wake debug ellipse uses cell-aspect radii ---");
            var field = new WindSwayField();
            var cfg = BaseCfg();
            cfg.WakeRadiusCells = 20;
            field.ApplyConfig(cfg);
            field.TrackMousePosition(100, 160);

            bool ok = field.TryGetWakeDebugEllipse(
                10, 16, out double cx, out double cy, out double rx, out double ry, out double rotation);
            TestBase.AssertTrue(ok, "debug ellipse should resolve with mouse tracked",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(Math.Abs(cx - 100) < 0.01 && Math.Abs(cy - 160) < 0.01,
                $"center should match mouse ({cx},{cy})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            // Idle default facing is upright: minor=cells×charW, major=cells×charH, rotation 0.
            TestBase.AssertTrue(Math.Abs(rx - 200) < 0.01 && Math.Abs(ry - 320) < 0.01,
                $"radii should be cell-aspect oval ({rx},{ry})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(Math.Abs(rotation) < 1e-9,
                $"idle upright rotation should be 0 (got {rotation})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestWakeEllipseRotatesWithMouseMotion()
        {
            Console.WriteLine("\n--- Wake ellipse rotates with mouse motion ---");
            const double charW = 10;
            const double charH = 16;
            var field = new WindSwayField();
            var cfg = BaseCfg();
            cfg.WakeRadiusCells = 10; // minor=100px, major=160px
            cfg.NearInfluence = 1.0;
            cfg.FarInfluence = 0.0;
            cfg.WakeRearBias = 0;
            cfg.WakeFalloffPower = 1;
            cfg.MaxOffsetFraction = 0.25;
            cfg.VerticalScale = 1.0;
            cfg.SpeedReferenceCellsPerSec = 40;
            field.ApplyConfig(cfg);

            // Horizontal motion → major axis lays along X (rotation ≈ -π/2).
            field.PushFromMouseDelta(105, 168, charW, charH);
            field.PushFromMouseDelta(205, 168, charW, charH, dtOverrideSeconds: 0.05);

            bool ok = field.TryGetWakeDebugEllipse(
                charW, charH, out _, out _, out double rx, out double ry, out double rotation);
            TestBase.AssertTrue(ok, "debug ellipse available after motion",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(Math.Abs(rx - 100) < 0.01 && Math.Abs(ry - 160) < 0.01,
                $"cell-aspect radii preserved ({rx},{ry})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(Math.Abs(rotation - (-Math.PI * 0.5)) < 1e-6,
                $"horizontal motion should rotate major onto X (got {rotation})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Mouse at (205,168). Along major (~140px east) inside; across minor (~112px south) outside.
            var (alongOx, alongOy) = field.SampleOffset(34, 10, 0, charW, charH);
            var (acrossOx, acrossOy) = field.SampleOffset(20, 17, 0, charW, charH);
            double alongMag = Math.Abs(alongOx) + Math.Abs(alongOy);
            double acrossMag = Math.Abs(acrossOx) + Math.Abs(acrossOy);
            TestBase.AssertTrue(alongMag > 0.02,
                $"along major (~140px) should sway when ellipse is horizontal (mag={alongMag})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(acrossMag < 1e-6,
                $"across minor (~112px) should be outside when ellipse is horizontal (mag={acrossMag})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            double uprightT = WindSwayField.EllipticalNormalizedRadius(0, 120, 0, 1, 100, 160);
            double horizontalT = WindSwayField.EllipticalNormalizedRadius(0, 120, 1, 0, 100, 160);
            TestBase.AssertTrue(uprightT < 1.0,
                $"120px south inside upright ellipse (t={uprightT})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(horizontalT > 1.0,
                $"120px south outside horizontal ellipse (t={horizontalT})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestTrackMouseUpdatesFacingWithoutWind()
        {
            Console.WriteLine("\n--- TrackMousePosition orients wake without wind push ---");
            var field = new WindSwayField();
            var cfg = BaseCfg();
            cfg.WakeRadiusCells = 10;
            field.ApplyConfig(cfg);

            field.TrackMousePosition(100, 100);
            field.TrackMousePosition(200, 100); // eastward — no PushFromMouseDelta

            bool ok = field.TryGetWakeDebugEllipse(
                10, 16, out _, out _, out _, out _, out double rotation);
            TestBase.AssertTrue(ok, "debug ellipse available after track-only motion",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(Math.Abs(rotation - (-Math.PI * 0.5)) < 1e-6,
                $"track-only horizontal motion should rotate major onto X (got {rotation})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestWakeDebugTransformKeepsCenter()
        {
            Console.WriteLine("\n--- Wake debug transform keeps oval centered on cursor ---");
            const double cx = 320;
            const double cy = 180;
            double rotation = -Math.PI * 0.5; // horizontal major
            var wrong = Matrix.CreateTranslation(cx, cy) * Matrix.CreateRotation(rotation);
            var right = GameCanvasControl.CreateWakeDebugTransform(cx, cy, rotation);

            var originWrong = wrong.Transform(new Point(0, 0));
            var originRight = right.Transform(new Point(0, 0));
            TestBase.AssertTrue(Math.Abs(originWrong.X - cx) > 1.0 || Math.Abs(originWrong.Y - cy) > 1.0,
                $"legacy Translation*Rotation must leave (0,0) (got {originWrong})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(Math.Abs(originRight.X - cx) < 1e-9 && Math.Abs(originRight.Y - cy) < 1e-9,
                $"Rotation*Translation must keep center at cursor (got {originRight})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestWakeDebugToggleDoesNotAffectSample()
        {
            Console.WriteLine("\n--- Wake debug toggle is runtime-only ---");
            var field = new WindSwayField();
            var cfg = BaseCfg();
            cfg.WakeRadiusCells = 22;
            field.ApplyConfig(cfg);
            field.TrackMousePosition(50, 80);

            field.ShowWakeRadiusDebug = true;
            TestBase.AssertTrue(field.ShowWakeRadiusDebug, "debug flag should turn on",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(Math.Abs(field.Config.WakeRadiusCells - 22) < 0.01,
                "debug flag must not mutate wake radius config",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            field.ShowWakeRadiusDebug = false;
            TestBase.AssertTrue(!field.ShowWakeRadiusDebug, "debug flag should turn off",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            bool stillOk = field.TryGetWakeDebugEllipse(10, 16, out _, out _, out double rx, out _, out _);
            TestBase.AssertTrue(stillOk && Math.Abs(rx - 220) < 0.01,
                "tracked mouse + radius still available after debug off",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestChromaticFringeZeroWhenIdle()
        {
            Console.WriteLine("\n--- Chromatic fringe is zero when idle ---");
            var (dx, dy) = WindSwayChromatic.ComputeFringe(0, 0, 0.10);
            TestBase.AssertTrue(dx == 0 && dy == 0, "zero offset → zero fringe",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var (dx2, dy2) = WindSwayChromatic.ComputeFringe(0.2, 0.1, 0.10);
            TestBase.AssertTrue(dx2 == 0 && dy2 == 0, "sub-IdleEpsilon offset → zero fringe",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(WindSwayChromatic.IdleEpsilon >= 0.4,
                $"IdleEpsilon should ignore subpixel smear (got {WindSwayChromatic.IdleEpsilon})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestChromaticFringeAlongOffset()
        {
            Console.WriteLine("\n--- Chromatic fringe follows offset direction ---");
            var (dx, dy) = WindSwayChromatic.ComputeFringe(10, 0, 0.10);
            TestBase.AssertTrue(Math.Abs(dx - 1.0) < 1e-9 && Math.Abs(dy) < 1e-9,
                $"horizontal 10px @ 0.10 → (1,0) got ({dx},{dy})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var (dx2, dy2) = WindSwayChromatic.ComputeFringe(0, 8, 0.25);
            TestBase.AssertTrue(Math.Abs(dx2) < 1e-9 && Math.Abs(dy2 - 2.0) < 1e-9,
                $"vertical 8px @ 0.25 → (0,2) got ({dx2},{dy2})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            double ox = 3, oy = 4; // mag 5
            var (dx3, dy3) = WindSwayChromatic.ComputeFringe(ox, oy, 0.2);
            TestBase.AssertTrue(Math.Abs(dx3 - 0.6) < 1e-9 && Math.Abs(dy3 - 0.8) < 1e-9,
                $"3-4-5 @ 0.2 → (0.6,0.8) got ({dx3},{dy3})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestChromaticFringeZeroSpread()
        {
            Console.WriteLine("\n--- Chromatic fringe is zero when spread ≤ 0 ---");
            var (dx, dy) = WindSwayChromatic.ComputeFringe(10, 5, 0);
            TestBase.AssertTrue(dx == 0 && dy == 0, "zero spread → zero fringe",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            var (dx2, dy2) = WindSwayChromatic.ComputeFringe(10, 5, -0.1);
            TestBase.AssertTrue(dx2 == 0 && dy2 == 0, "negative spread → zero fringe",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestChromaticOpacityToAlpha()
        {
            Console.WriteLine("\n--- Chromatic opacity maps to byte alpha ---");
            TestBase.AssertTrue(WindSwayChromatic.OpacityToAlpha(0.35) == 89,
                "0.35 → ~89", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(WindSwayChromatic.OpacityToAlpha(0) == 0,
                "0 → 0", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(WindSwayChromatic.OpacityToAlpha(1) == 255,
                "1 → 255", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(WindSwayChromatic.OpacityToAlpha(2) == 255,
                "clamp high", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(WindSwayChromatic.OpacityToAlpha(-1) == 0,
                "clamp low", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestChromaticConfigDefaults()
        {
            Console.WriteLine("\n--- Chromatic config defaults ---");
            var cfg = new WindSwayConfig();
            TestBase.AssertTrue(cfg.ChromaticAberrationEnabled, "CA enabled by default",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(Math.Abs(cfg.ChromaticSpreadFraction - 0.10) < 1e-9,
                "spread default 0.10", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(Math.Abs(cfg.ChromaticOpacity - 0.35) < 1e-9,
                "opacity default 0.35", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMayAffectTextUsesWakeBounds()
        {
            Console.WriteLine("\n--- MayAffectText uses wake AABB ---");
            const double charW = 10;
            const double charH = 16;
            var field = new WindSwayField();
            var cfg = BaseCfg();
            cfg.WakeRadiusCells = 6;
            field.ApplyConfig(cfg);
            field.PushFromMouseDelta(50, 80, charW, charH);
            field.PushFromMouseDelta(60, 80, charW, charH, dtOverrideSeconds: 0.05);

            TestBase.AssertTrue(field.BeginFrame(charW, charH), "frame should activate after impulse",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            try
            {
                TestBase.AssertTrue(field.MayAffectText(5, 5, 4, charW, charH),
                    "text near mouse should be inside wake AABB",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(!field.MayAffectText(40, 30, 4, charW, charH),
                    "text far from wake should be culled",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                field.EndFrame();
            }
        }

        private static void TestFrameActiveExposesBeginFrameSnapshot()
        {
            Console.WriteLine("\n--- FrameActive / FrameConfig paint snapshot ---");
            const double charW = 10;
            const double charH = 16;
            var field = new WindSwayField();
            var cfg = BaseCfg();
            cfg.SidePanelsOnly = true;
            cfg.ChromaticSpreadFraction = 0.22;
            field.ApplyConfig(cfg);

            TestBase.AssertTrue(!field.FrameActive, "inactive before BeginFrame",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            field.PushFromMouseDelta(50, 80, charW, charH);
            field.PushFromMouseDelta(60, 80, charW, charH, dtOverrideSeconds: 0.05);
            TestBase.AssertTrue(field.BeginFrame(charW, charH), "BeginFrame after impulse",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            try
            {
                TestBase.AssertTrue(field.FrameActive, "FrameActive during paint",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(field.FrameConfig.SidePanelsOnly, "FrameConfig mirrors SidePanelsOnly",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(
                    Math.Abs(field.FrameConfig.ChromaticSpreadFraction - 0.22) < 1e-9,
                    "FrameConfig mirrors chromatic spread",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                field.EndFrame();
            }

            TestBase.AssertTrue(!field.FrameActive, "FrameActive cleared after EndFrame",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestPerfDefaultsAreLeaner()
        {
            Console.WriteLine("\n--- Perf-oriented config defaults ---");
            var cfg = new WindSwayConfig();
            TestBase.AssertTrue(cfg.TrailMaxPoints <= 12,
                $"trail max should stay lean (got {cfg.TrailMaxPoints})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(cfg.SettleIntervalMs >= 20,
                $"settle interval should be ≥20ms (got {cfg.SettleIntervalMs})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(cfg.TrailLifetimeSeconds <= 0.45,
                $"trail lifetime should be shorter (got {cfg.TrailLifetimeSeconds})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
