using System;
using System.Collections.Generic;
using RPGGame.Combat.Sequence;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Tests for the PROTOTYPE narrative pacing gate (GameData/NarrativePacing.json).
    ///
    /// The point of the gate is that the shipped default changes nothing. Most of these
    /// assert the "stays quiet about staying quiet" side: passthrough suppresses no swing
    /// and no step, and the file as committed resolves to passthrough.
    /// </summary>
    public static class NarrativePacingTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Narrative Pacing Tests ===\n");
            _testsRun = _testsPassed = _testsFailed = 0;

            try
            {
                ShippedConfig_Resolves_To_Passthrough();
                Passthrough_Suppresses_Nothing();
                ZeroSwingChance_Always_Suppresses_Swing();
                FullSwingChance_Never_Suppresses_Swing();
                ZeroStepChance_Always_Suppresses_That_Step();
                UnlistedStep_Keeps_Speaking();
                EmptyStepChance_Suppresses_No_Step();
            }
            finally
            {
                // Never leave an override behind for later suites.
                NarrativePacing.OverrideForTesting(null);
                NarrativePacing.Invalidate();
            }

            TestBase.PrintSummary("Narrative Pacing Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void ShippedConfig_Resolves_To_Passthrough()
        {
            Console.WriteLine("--- Shipped config is live and wired ---");

            // This assertion used to read "the committed file must resolve to passthrough",
            // which was right while the narrator was an unproven prototype and wrong once the
            // branch's whole purpose was showing it. The property worth keeping is not "the
            // default is off" - it is "the file is genuinely read", which is what catches a
            // config that has silently stopped being parsed.
            //
            // Both halves matter. Profile.Full must still be inert, so the fallback for a
            // missing or unreadable file changes nothing; and the committed file must resolve
            // to something that is NOT the fallback, which proves parsing works end to end.
            // A file that failed to parse returns Profile.Full from the catch, so a single
            // "is it passthrough" check cannot tell a deliberate default from a broken read.

            TestBase.AssertTrue(NarrativePacing.Profile.Full.IsPassthrough,
                "Profile.Full must stay inert - it is the fallback for a missing or unreadable file",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            NarrativePacing.OverrideForTesting(null);
            NarrativePacing.Invalidate();

            var active = NarrativePacing.Active;
            Console.WriteLine($"    [committed] narratorEnabled={active.NarratorEnabled} "
                              + $"swingChance={active.SwingChance} "
                              + $"maxSentences={active.MaxSentencesPerSwing} "
                              + $"paragraphTargetMs={active.ParagraphTargetMs}");

            TestBase.AssertTrue(!active.IsPassthrough,
                "NarrativePacing.json as committed must resolve to a real profile, not the "
                + "fallback - if this fails the file is missing, unparseable, or disabled",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(active.NarratorEnabled,
                "The committed profile must let the narrator speak - this branch exists to show him",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(active.ParagraphTargetMs > 0,
                "The committed profile must override the flat 8000ms paragraph budget, or "
                + "shorter prose just types more slowly",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void Passthrough_Suppresses_Nothing()
        {
            Console.WriteLine("\n--- Passthrough profile suppresses nothing ---");

            var profile = NarrativePacing.Profile.Full;
            NarrativePacing.OverrideForTesting(profile, new Random(1));

            bool anySwingSuppressed = false;
            bool anyStepSuppressed = false;
            for (int i = 0; i < 200; i++)
            {
                if (NarrativePacing.SuppressSwing(profile))
                    anySwingSuppressed = true;

                foreach (var kind in CombatSequenceFlavorTokens.NarrativeBeatOrder)
                {
                    if (NarrativePacing.SuppressStep(profile, kind))
                        anyStepSuppressed = true;
                }
            }

            TestBase.AssertTrue(!anySwingSuppressed,
                "Passthrough must never suppress a swing",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!anyStepSuppressed,
                "Passthrough must never suppress a step",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void ZeroSwingChance_Always_Suppresses_Swing()
        {
            Console.WriteLine("\n--- swingChance 0.0 silences every swing ---");

            var profile = new NarrativePacing.Profile { SwingChance = 0.0 };
            NarrativePacing.OverrideForTesting(profile, new Random(2));

            bool allSuppressed = true;
            for (int i = 0; i < 100; i++)
            {
                if (!NarrativePacing.SuppressSwing(profile))
                    allSuppressed = false;
            }

            TestBase.AssertTrue(allSuppressed,
                "swingChance 0.0 should suppress every swing",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void FullSwingChance_Never_Suppresses_Swing()
        {
            Console.WriteLine("\n--- swingChance 1.0 narrates every swing ---");

            var profile = new NarrativePacing.Profile { SwingChance = 1.0 };
            NarrativePacing.OverrideForTesting(profile, new Random(3));

            bool noneSuppressed = true;
            for (int i = 0; i < 100; i++)
            {
                if (NarrativePacing.SuppressSwing(profile))
                    noneSuppressed = false;
            }

            TestBase.AssertTrue(noneSuppressed,
                "swingChance 1.0 should never suppress a swing",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void ZeroStepChance_Always_Suppresses_That_Step()
        {
            Console.WriteLine("\n--- stepChance 0.0 silences that step only ---");

            var profile = new NarrativePacing.Profile
            {
                StepChance = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Tempo"] = 0.0
                }
            };
            NarrativePacing.OverrideForTesting(profile, new Random(4));

            bool tempoAlwaysSuppressed = true;
            bool actionEverSuppressed = false;
            for (int i = 0; i < 100; i++)
            {
                if (!NarrativePacing.SuppressStep(profile, CombatSequenceStepKind.Tempo))
                    tempoAlwaysSuppressed = false;
                if (NarrativePacing.SuppressStep(profile, CombatSequenceStepKind.Action))
                    actionEverSuppressed = true;
            }

            TestBase.AssertTrue(tempoAlwaysSuppressed,
                "stepChance 0.0 should suppress that step every time",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!actionEverSuppressed,
                "A step absent from stepChance must not be suppressed",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void UnlistedStep_Keeps_Speaking()
        {
            Console.WriteLine("\n--- Unlisted step kinds keep speaking ---");

            // Scale-proof: a step kind nobody has added to the file yet must not go silent.
            var profile = new NarrativePacing.Profile
            {
                StepChance = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Roll"] = 0.0
                }
            };
            NarrativePacing.OverrideForTesting(profile, new Random(5));

            bool anyOtherSuppressed = false;
            foreach (var kind in CombatSequenceFlavorTokens.NarrativeBeatOrder)
            {
                if (kind == CombatSequenceStepKind.Roll)
                    continue;

                for (int i = 0; i < 25; i++)
                {
                    if (NarrativePacing.SuppressStep(profile, kind))
                        anyOtherSuppressed = true;
                }
            }

            TestBase.AssertTrue(!anyOtherSuppressed,
                "Only steps listed in stepChance may be suppressed",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void EmptyStepChance_Suppresses_No_Step()
        {
            Console.WriteLine("\n--- Empty stepChance suppresses no step ---");

            var profile = new NarrativePacing.Profile
            {
                SwingChance = 0.5,
                StepChance = new Dictionary<string, double>()
            };
            NarrativePacing.OverrideForTesting(profile, new Random(6));

            bool anySuppressed = false;
            foreach (var kind in CombatSequenceFlavorTokens.NarrativeBeatOrder)
            {
                for (int i = 0; i < 25; i++)
                {
                    if (NarrativePacing.SuppressStep(profile, kind))
                        anySuppressed = true;
                }
            }

            TestBase.AssertTrue(!anySuppressed,
                "An empty stepChance map must leave every step speaking",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
