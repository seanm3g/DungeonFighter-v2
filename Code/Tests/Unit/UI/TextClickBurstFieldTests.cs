using System;
using RPGGame;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Effects;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Unit tests for multi-zone click-charge explode / stay / click-rebuild
    /// (<see cref="TextClickBurstField"/>).
    /// </summary>
    public static class TextClickBurstFieldTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== TextClickBurstField Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            bool prevDistortion = DeveloperModeState.AreDistortionEffectsEnabled;
            DeveloperModeState.SetDistortionEffectsEnabled(true);
            try
            {
                TestChargeBuildsOnClicks();
                TestChargeDecaysWhenIdle();
                TestExplodeTriggersAtThreshold();
                TestSampleNonZeroDuringExplodeOut();
                TestSampleStaysAtPeakWithoutAutoReform();
                TestVelocityNonZeroDuringOutZeroWhenLanded();
                TestRotationRandomDuringExplode();
                TestDistanceVarianceAcrossGlyphs();
                TestClickRebuildsOneLetterAtATime();
                TestIdleAutoRebuildAfterTenSeconds();
                TestIdleAutoRebuildIntervalFiveHundredMs();
                TestHoleClickResetsAutoRebuildIdle();
                TestClickOutsideDoesNotRebuildCreatesSecondZone();
                TestExplodedOriginStaysPinnedWhenClickingElsewhere();
                TestClickInsideMessRebuildsThatZoneOnly();
                TestDisabledIgnoresClicks();
                TestRuntimeDistortionOffIgnoresClicks();
                TestMayAffectTextUsesRadius();
            }
            finally
            {
                DeveloperModeState.SetDistortionEffectsEnabled(prevDistortion);
            }

            TestBase.PrintSummary("TextClickBurstField Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static TextClickBurstConfig BaseCfg() => new TextClickBurstConfig
        {
            Enabled = true,
            ClicksToExplode = 5,
            ChargeDecayPerSecond = 0,
            ImpulseDecayPerSecond = 0,
            ImpulseRadiusCells = 12,
            MaxImpulseOffsetFraction = 0.35,
            ExplodeMaxOffsetFraction = 3.0,
            DistanceVarianceMin = 0.2,
            DistanceVarianceMax = 1.45,
            MaxRotationRadians = 7.5,
            ExplodeOutSeconds = 0.28,
            ExplodeHoldSeconds = 0,
            ReformSeconds = 0.22,
            LettersToRebuild = 14,
            AutoRebuildIdleSeconds = 10.0,
            AutoRebuildIntervalSeconds = 0.5,
            ChromaticVelocitySeconds = 0.12,
            VerticalScale = 1.0
        };

        private static void Explode(TextClickBurstField field, double originX = 55, double originY = 40)
        {
            field.SetTestClockSeconds(0);
            for (int i = 0; i < 5; i++)
                field.NotifyClick(originX, originY, 10, 16, nowOverrideSeconds: i * 0.01);
        }

        private static void TestChargeBuildsOnClicks()
        {
            Console.WriteLine("--- Charge builds on clicks ---");
            var field = new TextClickBurstField();
            field.ApplyConfig(BaseCfg());
            field.SetTestClockSeconds(0);
            field.NotifyClick(50, 50, 10, 16, nowOverrideSeconds: 0);
            field.NotifyClick(50, 50, 10, 16, nowOverrideSeconds: 0.01);
            double charge = field.GetChargeForTests();
            TestBase.AssertTrue(charge > 0.35 && charge < 0.45,
                $"two of five clicks should leave charge ~0.4 (got {charge})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(field.IsActive,
                "field should be active after click",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestChargeDecaysWhenIdle()
        {
            Console.WriteLine("\n--- Charge decays when idle ---");
            var field = new TextClickBurstField();
            var cfg = BaseCfg();
            cfg.ChargeDecayPerSecond = 0.55;
            cfg.ImpulseDecayPerSecond = 5;
            field.ApplyConfig(cfg);
            field.SetTestClockSeconds(0);
            field.NotifyClick(50, 50, 10, 16, nowOverrideSeconds: 0);
            field.NotifyClick(50, 50, 10, 16, nowOverrideSeconds: 0.01);
            double before = field.GetChargeForTests();
            field.TickForTests(2.0);
            double after = field.GetChargeForTests();
            TestBase.AssertTrue(before > 0.3,
                $"expected charged before decay (got {before})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(after < before * 0.2,
                $"charge should decay substantially ({before} → {after})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestExplodeTriggersAtThreshold()
        {
            Console.WriteLine("\n--- Explode triggers at threshold ---");
            var field = new TextClickBurstField();
            field.ApplyConfig(BaseCfg());
            field.SetTestClockSeconds(0);
            for (int i = 0; i < 4; i++)
                field.NotifyClick(50, 50, 10, 16, nowOverrideSeconds: i * 0.01);
            TestBase.AssertTrue(!field.IsExplodingForTests(),
                "should not explode before 5th click",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            field.NotifyClick(50, 50, 10, 16, nowOverrideSeconds: 0.05);
            TestBase.AssertTrue(field.IsExplodingForTests(),
                "5th click should start explode",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(field.GetChargeForTests() < 0.01,
                "charge should reset on explode",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSampleNonZeroDuringExplodeOut()
        {
            Console.WriteLine("\n--- Sample non-zero during explode out ---");
            var field = new TextClickBurstField();
            field.ApplyConfig(BaseCfg());
            Explode(field);

            // Mid out-phase (explode starts ~0.04)
            field.TickForTests(0.16);
            var (ox, oy) = field.SampleOffset(5, 2, 1, 10, 16);
            TestBase.AssertTrue(Math.Abs(ox) + Math.Abs(oy) > 5.0,
                $"expected strong explode offset (ox={ox}, oy={oy})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSampleStaysAtPeakWithoutAutoReform()
        {
            Console.WriteLine("\n--- Sample stays at peak without auto-reform ---");
            var field = new TextClickBurstField();
            field.ApplyConfig(BaseCfg());
            Explode(field);

            // End of out → peak (explode ~0.04 + 0.28)
            field.TickForTests(0.40);
            var (peakOx, peakOy) = field.SampleOffset(5, 2, 1, 10, 16);
            double peakMag = Math.Abs(peakOx) + Math.Abs(peakOy);
            TestBase.AssertTrue(peakMag > 15.0,
                $"expected peak explode magnitude (got {peakMag})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Long after out — must still be scattered (no auto-reform)
            field.TickForTests(5.0);
            TestBase.AssertTrue(field.IsExplodingForTests(),
                "should stay exploded until click-rebuild",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            var (holdOx, holdOy) = field.SampleOffset(5, 2, 1, 10, 16);
            double holdMag = Math.Abs(holdOx) + Math.Abs(holdOy);
            TestBase.AssertTrue(Math.Abs(holdMag - peakMag) < 0.5,
                $"scatter should stay at peak ({peakMag} → {holdMag})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestVelocityNonZeroDuringOutZeroWhenLanded()
        {
            Console.WriteLine("\n--- Velocity non-zero during out, zero when landed ---");
            var field = new TextClickBurstField();
            field.ApplyConfig(BaseCfg());
            Explode(field);

            field.TickForTests(0.12);
            var flying = field.Sample(5, 2, 1, 10, 16);
            double flyVel = Math.Abs(flying.VelocityX) + Math.Abs(flying.VelocityY);
            TestBase.AssertTrue(flyVel > 1.0,
                $"expected flight velocity during out (got {flyVel})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            field.TickForTests(1.0);
            var landed = field.Sample(5, 2, 1, 10, 16);
            double landVel = Math.Abs(landed.VelocityX) + Math.Abs(landed.VelocityY);
            double landMag = Math.Abs(landed.OffsetX) + Math.Abs(landed.OffsetY);
            TestBase.AssertTrue(landMag > 10.0,
                $"landed letter should still be displaced (mag={landMag})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(landVel < 1e-3,
                $"landed letter velocity should be ~0 for clean glyph (got {landVel})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRotationRandomDuringExplode()
        {
            Console.WriteLine("\n--- Rotation random during explode ---");
            var field = new TextClickBurstField();
            field.ApplyConfig(BaseCfg());
            Explode(field);
            field.TickForTests(0.50);

            double rot0 = field.Sample(5, 2, 0, 10, 16).RotationRadians;
            double rot1 = field.Sample(5, 2, 1, 10, 16).RotationRadians;
            double rot2 = field.Sample(5, 2, 2, 10, 16).RotationRadians;
            TestBase.AssertTrue(Math.Abs(rot0) + Math.Abs(rot1) + Math.Abs(rot2) > 0.5,
                $"expected non-trivial rotation (r0={rot0}, r1={rot1}, r2={rot2})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(
                Math.Abs(rot0 - rot1) > 0.05 || Math.Abs(rot1 - rot2) > 0.05 || Math.Abs(rot0 - rot2) > 0.05,
                $"expected rotation variance across glyphs (r0={rot0}, r1={rot1}, r2={rot2})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestDistanceVarianceAcrossGlyphs()
        {
            Console.WriteLine("\n--- Distance variance across glyphs ---");
            var field = new TextClickBurstField();
            field.ApplyConfig(BaseCfg());
            Explode(field);
            field.TickForTests(0.50);

            // Same radial distance from origin (55,40): left vs right neighbors.
            var (oxL, oyL) = field.SampleOffset(4, 2, 0, 10, 16); // center ~45,40
            var (oxR, oyR) = field.SampleOffset(6, 2, 0, 10, 16); // center ~65,40
            double magL = Math.Sqrt(oxL * oxL + oyL * oyL);
            double magR = Math.Sqrt(oxR * oxR + oyR * oyR);
            double ratio = Math.Max(magL, magR) / Math.Max(1e-3, Math.Min(magL, magR));
            TestBase.AssertTrue(ratio > 1.15,
                $"expected distance variance at equal radius (L={magL}, R={magR}, ratio={ratio})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestClickRebuildsOneLetterAtATime()
        {
            Console.WriteLine("\n--- Click rebuilds one letter at a time ---");
            var field = new TextClickBurstField();
            var cfg = BaseCfg();
            cfg.LettersToRebuild = 10;
            field.ApplyConfig(cfg);
            Explode(field);

            // Land at peak
            field.TickForTests(0.50);
            TestBase.AssertTrue(field.GetRebuildProgressForTests() < 0.01,
                "rebuild should start at 0",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            field.NotifyClick(55, 40, 10, 16, nowOverrideSeconds: 0.60);
            double progress = field.GetRebuildProgressForTests();
            TestBase.AssertTrue(Math.Abs(progress - 0.1) < 0.001,
                $"one click should advance ~1/10 rebuild (got {progress})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(field.IsExplodingForTests(),
                "should still be exploding mid-rebuild",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Finish rebuild clicks
            for (int i = 0; i < 9; i++)
                field.NotifyClick(55, 40, 10, 16, nowOverrideSeconds: 0.70 + i * 0.05);

            TestBase.AssertTrue(field.GetRebuildProgressForTests() >= 0.999,
                "10 clicks should fill rebuild",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // After last letter anim
            field.TickForTests(1.50);
            TestBase.AssertTrue(!field.IsExplodingForTests(),
                "explode should end after full click-rebuild + anim",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            var (ox, oy) = field.SampleOffset(5, 2, 1, 10, 16);
            TestBase.AssertTrue(Math.Abs(ox) < 0.05 && Math.Abs(oy) < 0.05,
                $"glyphs should be home after rebuild (ox={ox}, oy={oy})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestIdleAutoRebuildAfterTenSeconds()
        {
            Console.WriteLine("\n--- Idle auto-rebuild after 10 seconds ---");
            var field = new TextClickBurstField();
            var cfg = BaseCfg();
            cfg.LettersToRebuild = 10;
            cfg.AutoRebuildIdleSeconds = 10.0;
            cfg.AutoRebuildIntervalSeconds = 0.5;
            field.ApplyConfig(cfg);
            Explode(field); // explode ~0.04

            // Landed (~0.32) but still inside idle window
            field.TickForTests(9.0);
            TestBase.AssertTrue(field.GetRebuildProgressForTests() < 0.01,
                "should not autofill before idle expires",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // landTime ≈ 0.04+0.28 = 0.32; first letter due at 10.32
            field.TickForTests(10.40);
            double progress = field.GetRebuildProgressForTests();
            TestBase.AssertTrue(Math.Abs(progress - 0.1) < 0.001,
                $"first autofill letter ~1/10 (got {progress})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(field.IsExplodingForTests(),
                "should still be exploding after first autofill letter",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestIdleAutoRebuildIntervalFiveHundredMs()
        {
            Console.WriteLine("\n--- Idle auto-rebuild fills one letter every 500ms ---");
            var field = new TextClickBurstField();
            var cfg = BaseCfg();
            cfg.LettersToRebuild = 10;
            cfg.AutoRebuildIdleSeconds = 10.0;
            cfg.AutoRebuildIntervalSeconds = 0.5;
            field.ApplyConfig(cfg);
            Explode(field);

            // First due ~10.32; at 11.40 → letters at 10.32, 10.82, 11.32 = 3 letters
            field.TickForTests(11.40);
            double progress = field.GetRebuildProgressForTests();
            TestBase.AssertTrue(Math.Abs(progress - 0.3) < 0.001,
                $"three autofill steps at 500ms (got {progress})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHoleClickResetsAutoRebuildIdle()
        {
            Console.WriteLine("\n--- Hole click resets auto-rebuild idle ---");
            var field = new TextClickBurstField();
            var cfg = BaseCfg();
            cfg.LettersToRebuild = 10;
            cfg.AutoRebuildIdleSeconds = 10.0;
            cfg.AutoRebuildIntervalSeconds = 0.5;
            field.ApplyConfig(cfg);
            Explode(field);
            field.TickForTests(0.50);

            // Manual cleanup click mid-idle
            field.NotifyClick(55, 40, 10, 16, nowOverrideSeconds: 2.0);
            TestBase.AssertTrue(Math.Abs(field.GetRebuildProgressForTests() - 0.1) < 0.001,
                "manual click should rebuild one letter",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // 5s later — still inside the reset 10s idle
            field.TickForTests(7.0);
            TestBase.AssertTrue(Math.Abs(field.GetRebuildProgressForTests() - 0.1) < 0.001,
                "autofill must wait another full idle after hole click",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Idle from click at 2.0 → next due at 12.0
            field.TickForTests(12.1);
            double progress = field.GetRebuildProgressForTests();
            TestBase.AssertTrue(Math.Abs(progress - 0.2) < 0.001,
                $"autofill resumes one letter after reset idle (got {progress})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestClickOutsideDoesNotRebuildCreatesSecondZone()
        {
            Console.WriteLine("\n--- Click outside does not rebuild; creates second zone ---");
            var field = new TextClickBurstField();
            var cfg = BaseCfg();
            cfg.LettersToRebuild = 10;
            field.ApplyConfig(cfg);
            Explode(field, originX: 55, originY: 40);
            field.TickForTests(0.50);

            // Far outside radius (12 cells * 10px = 120px)
            field.NotifyClick(400, 400, 10, 16, nowOverrideSeconds: 0.60);
            TestBase.AssertTrue(field.GetRebuildProgressForTests() < 0.01,
                "outside click must not rebuild the first mess",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(field.GetZoneCountForTests() >= 2,
                $"outside click should start a second zone (got {field.GetZoneCountForTests()})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            for (int i = 0; i < 4; i++)
                field.NotifyClick(400, 400, 10, 16, nowOverrideSeconds: 0.70 + i * 0.01);

            TestBase.AssertTrue(field.GetExplodingZoneCountForTests() >= 2,
                $"two sections should be exploded (got {field.GetExplodingZoneCountForTests()})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestExplodedOriginStaysPinnedWhenClickingElsewhere()
        {
            Console.WriteLine("\n--- Exploded origin stays pinned when clicking elsewhere ---");
            var field = new TextClickBurstField();
            field.ApplyConfig(BaseCfg());
            Explode(field, originX: 55, originY: 40);
            field.TickForTests(0.50);

            var (beforeOx, beforeOy) = field.SampleOffset(5, 2, 1, 10, 16);
            double beforeMag = Math.Abs(beforeOx) + Math.Abs(beforeOy);
            TestBase.AssertTrue(beforeMag > 15.0,
                $"expected scatter at original site (got {beforeMag})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Charge a distant second site — first mess must stay put.
            for (int i = 0; i < 5; i++)
                field.NotifyClick(400, 400, 10, 16, nowOverrideSeconds: 0.60 + i * 0.01);
            field.TickForTests(1.0);

            var (afterOx, afterOy) = field.SampleOffset(5, 2, 1, 10, 16);
            double afterMag = Math.Abs(afterOx) + Math.Abs(afterOy);
            TestBase.AssertTrue(Math.Abs(afterMag - beforeMag) < 0.5,
                $"original mess should stay pinned ({beforeMag} → {afterMag})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestClickInsideMessRebuildsThatZoneOnly()
        {
            Console.WriteLine("\n--- Click inside mess rebuilds that zone only ---");
            var field = new TextClickBurstField();
            var cfg = BaseCfg();
            cfg.LettersToRebuild = 10;
            field.ApplyConfig(cfg);

            Explode(field, originX: 55, originY: 40);
            field.TickForTests(0.50);
            for (int i = 0; i < 5; i++)
                field.NotifyClick(400, 400, 10, 16, nowOverrideSeconds: 0.60 + i * 0.01);
            field.TickForTests(1.0);

            TestBase.AssertTrue(field.GetExplodingZoneCountForTests() == 2,
                $"expected two exploded zones (got {field.GetExplodingZoneCountForTests()})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Cleanup clicks on first mess only
            for (int i = 0; i < 10; i++)
                field.NotifyClick(55, 40, 10, 16, nowOverrideSeconds: 1.20 + i * 0.05);
            field.TickForTests(2.0);

            TestBase.AssertTrue(field.GetExplodingZoneCountForTests() == 1,
                $"first mess cleaned; second should remain (got {field.GetExplodingZoneCountForTests()})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var (oxNear, oyNear) = field.SampleOffset(5, 2, 1, 10, 16);
            TestBase.AssertTrue(Math.Abs(oxNear) < 0.05 && Math.Abs(oyNear) < 0.05,
                $"cleaned zone glyphs should be home (ox={oxNear}, oy={oyNear})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Far glyph near second origin should still be scattered
            // Origin 400,400 → grid ~40,25
            var (oxFar, oyFar) = field.SampleOffset(40, 25, 0, 10, 16);
            double farMag = Math.Abs(oxFar) + Math.Abs(oyFar);
            TestBase.AssertTrue(farMag > 5.0,
                $"second mess should still be scattered (mag={farMag})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestDisabledIgnoresClicks()
        {
            Console.WriteLine("\n--- Disabled ignores clicks ---");
            var field = new TextClickBurstField();
            var cfg = BaseCfg();
            cfg.Enabled = false;
            field.ApplyConfig(cfg);
            field.SetTestClockSeconds(0);
            field.NotifyClick(50, 50, 10, 16, nowOverrideSeconds: 0);
            TestBase.AssertTrue(field.GetChargeForTests() < 1e-6,
                "disabled field should not charge",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!field.IsActive,
                "disabled field should not be active",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRuntimeDistortionOffIgnoresClicks()
        {
            Console.WriteLine("\n--- F6 runtime distortion off ignores clicks ---");
            bool prev = DeveloperModeState.AreDistortionEffectsEnabled;
            try
            {
                DeveloperModeState.SetDistortionEffectsEnabled(false);
                var field = new TextClickBurstField();
                field.ApplyConfig(BaseCfg());
                field.SetTestClockSeconds(0);
                field.NotifyClick(50, 50, 10, 16, nowOverrideSeconds: 0);
                TestBase.AssertTrue(field.GetChargeForTests() < 1e-6,
                    "runtime-off field should not charge",
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

        private static void TestMayAffectTextUsesRadius()
        {
            Console.WriteLine("\n--- MayAffectText uses radius ---");
            var field = new TextClickBurstField();
            field.ApplyConfig(BaseCfg());
            field.SetTestClockSeconds(0);
            field.NotifyClick(50, 80, 10, 16, nowOverrideSeconds: 0);
            TestBase.AssertTrue(field.BeginFrame(10, 16),
                "BeginFrame should succeed after click",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            try
            {
                // Origin at px (50,80) → near grid (5,5); radius 12 cells * 10px = 120px
                TestBase.AssertTrue(field.MayAffectText(5, 5, 4, 10, 16),
                    "text near origin should be in radius",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(!field.MayAffectText(40, 40, 4, 10, 16),
                    "far text should be outside radius",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                field.EndFrame();
            }
        }
    }
}
