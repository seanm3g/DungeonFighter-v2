using System;
using RPGGame.Actions;
using RPGGame.Actions.RollModification;
using RPGGame.Combat.Calculators;
using RPGGame.Tests;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Comprehensive tests for CombatResults
    /// Tests combat result formatting and UI formatting
    /// </summary>
    public static class CombatResultsTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all CombatResults tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== CombatResults Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestFormatDamageDisplaySeparated();
            TestFormatDamageDisplayColored();
            TestRollInfoShowsQueuedSheetAmpInAmpLine();
            TestRollInfoShowsSheetAmpStackedOnTechniqueAmp();
            TestFormatMissMessageColored();
            TestFormatNonAttackActionColored();
            TestFormatHealthMilestoneColored();
            TestFormatBlockMessageColored();
            TestFormatDodgeMessageColored();
            TestFormatStatusEffectColored();
            TestFormatHealingMessageColored();
            TestCheckHealthMilestones();
            TestExecuteActionWithUIAndStatusEffectsColored();
            TestWeaponPoisonSingleApplicationColoredExecutePath();
            TestWeaponPoisonNotAppliedOnNonCritColoredExecutePath();
            TestQueuedActionModsShowImmediatelyAfterQueuingAction();

            TestBase.PrintSummary("CombatResults Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Damage Display Tests

        private static void TestFormatDamageDisplaySeparated()
        {
            Console.WriteLine("--- Testing FormatDamageDisplaySeparated ---");

            var attacker = TestDataBuilders.Character().WithName("Attacker").Build();
            var target = TestDataBuilders.Enemy().WithName("Target").Build();
            var action = TestDataBuilders.CreateMockAction("JAB");

            var (damageText, rollInfo) = CombatResults.FormatDamageDisplaySeparated(
                attacker, target, 50, 45, action, 1.0, 1.0, 0, 10);

            TestBase.AssertNotNull(damageText,
                "Damage text should not be null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(rollInfo,
                "Roll info should not be null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (damageText != null)
            {
                TestBase.AssertTrue(damageText.Contains(attacker.Name),
                    "Damage text should contain attacker name",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestFormatDamageDisplayColored()
        {
            Console.WriteLine("\n--- Testing FormatDamageDisplayColored ---");

            var attacker = TestDataBuilders.Character().WithName("Attacker").Build();
            var target = TestDataBuilders.Enemy().WithName("Target").Build();
            var action = TestDataBuilders.CreateMockAction("JAB");

            var (damageText, rollInfo) = CombatResults.FormatDamageDisplayColored(
                attacker, target, 50, 45, action, 1.0, 1.0, 0, 10);

            TestBase.AssertNotNull(damageText,
                "Colored damage text should not be null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(rollInfo,
                "Colored roll info should not be null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>Roll footer amp must reflect ConsumedAmpModPercent (ModTrade / sheet AMP_MOD on next swing).</summary>
        private static void TestRollInfoShowsQueuedSheetAmpInAmpLine()
        {
            Console.WriteLine("\n--- Testing roll info shows queued sheet AMP_MOD ---");

            // Technique low enough that GetComboAmplifier() is 1.0 so sheet +10% alone yields 1.10x (slot mult 1.0).
            var attacker = TestDataBuilders.Character().WithName("Hero").WithStats(10, 10, 3, 10).Build();
            attacker.Effects.ConsumedAmpModPercent = 10;
            var target = TestDataBuilders.Enemy().WithName("Goblin").Build();
            var action = TestDataBuilders.CreateMockAction("JAB");
            action.IsComboAction = true;

            var (_, rollInfo) = CombatResults.FormatDamageDisplayColored(
                attacker, target, 12, 10, action, 1.0, 1.0, 0, 12);

            string rendered = ColoredTextRenderer.RenderAsMarkup(rollInfo);
            TestBase.AssertTrue(
                rendered.Contains("1.10x", StringComparison.Ordinal),
                "Colored roll info should show effective amp (1.10x) when ConsumedAmpModPercent is 10% and technique amp is 1.0",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var (_, rollInfoPlain) = CombatResults.FormatDamageDisplaySeparated(
                attacker, target, 12, 10, action, 1.0, 1.0, 0, 12);
            TestBase.AssertTrue(
                rollInfoPlain.Contains("1.10x", StringComparison.Ordinal),
                "Separated roll info should show effective amp (1.10x) when ConsumedAmpModPercent is 10% and technique amp is 1.0",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>Sheet AMP_MOD stacks on technique baseline when combo slot mult is 1.0 (e.g. opener).</summary>
        private static void TestRollInfoShowsSheetAmpStackedOnTechniqueAmp()
        {
            Console.WriteLine("\n--- Testing roll info stacks sheet AMP on technique amp ---");

            var attacker = TestDataBuilders.Character().WithName("Hero").WithStats(3, 3, 12, 3).Build();
            attacker.Effects.ConsumedAmpModPercent = 10;
            var target = TestDataBuilders.Enemy().WithName("Goblin").Build();
            var action = TestDataBuilders.CreateMockAction("JAB");
            action.IsComboAction = true;

            double expectedAmp = DamageCalculator.GetDisplayedComboMultiplier(attacker, 1.0, action);
            string expectedLabel = $"{expectedAmp:F2}x";

            var (_, rollInfo) = CombatResults.FormatDamageDisplayColored(
                attacker, target, 12, 10, action, 1.0, 1.0, 0, 12);
            string rendered = ColoredTextRenderer.RenderAsMarkup(rollInfo);
            TestBase.AssertTrue(
                rendered.Contains(expectedLabel, StringComparison.Ordinal),
                $"Colored roll info should show stacked amp {expectedLabel} (technique x sheet), got: {rendered}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Miss/Block/Dodge Tests

        private static void TestFormatMissMessageColored()
        {
            Console.WriteLine("\n--- Testing FormatMissMessageColored ---");

            var attacker = TestDataBuilders.Character().WithName("Attacker").Build();
            var target = TestDataBuilders.Enemy().WithName("Target").Build();
            var action = TestDataBuilders.CreateMockAction("JAB");

            var (missText, rollInfo) = CombatResults.FormatMissMessageColored(attacker, target, action, 0, 0, 3);

            TestBase.AssertNotNull(missText,
                "Miss text should not be null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(rollInfo,
                "Miss roll info should not be null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestFormatBlockMessageColored()
        {
            Console.WriteLine("\n--- Testing FormatBlockMessageColored ---");

            var defender = TestDataBuilders.Character().WithName("Defender").Build();
            var attacker = TestDataBuilders.Enemy().WithName("Attacker").Build();

            var blockText = CombatResults.FormatBlockMessageColored(defender, attacker, 10);

            TestBase.AssertNotNull(blockText,
                "Block text should not be null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestFormatDodgeMessageColored()
        {
            Console.WriteLine("\n--- Testing FormatDodgeMessageColored ---");

            var defender = TestDataBuilders.Character().WithName("Defender").Build();
            var attacker = TestDataBuilders.Enemy().WithName("Attacker").Build();

            var dodgeText = CombatResults.FormatDodgeMessageColored(defender, attacker);

            TestBase.AssertNotNull(dodgeText,
                "Dodge text should not be null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Other Action Tests

        private static void TestFormatNonAttackActionColored()
        {
            Console.WriteLine("\n--- Testing FormatNonAttackActionColored ---");

            var actor = TestDataBuilders.Character().WithName("Actor").Build();
            var target = TestDataBuilders.Enemy().WithName("Target").Build();
            var action = TestDataBuilders.CreateMockAction("HEAL");
            action.Type = ActionType.Heal;

            var (actionText, rollInfo) = CombatResults.FormatNonAttackActionColored(actor, target, action, 15, 2);

            TestBase.AssertNotNull(actionText,
                "Non-attack action text should not be null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(rollInfo,
                "Non-attack roll info should not be null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestFormatHealingMessageColored()
        {
            Console.WriteLine("\n--- Testing FormatHealingMessageColored ---");

            var healer = TestDataBuilders.Character().WithName("Healer").Build();
            var target = TestDataBuilders.Character().WithName("Target").Build();

            var healText = CombatResults.FormatHealingMessageColored(healer, target, 25);

            TestBase.AssertNotNull(healText,
                "Healing text should not be null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestFormatStatusEffectColored()
        {
            Console.WriteLine("\n--- Testing FormatStatusEffectColored ---");

            var target = TestDataBuilders.Character().WithName("Target").Build();

            var effectText = CombatResults.FormatStatusEffectColored(target, "Bleed", true, 3, 1);

            TestBase.AssertNotNull(effectText,
                "Status effect text should not be null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Health Milestone Tests

        private static void TestFormatHealthMilestoneColored()
        {
            Console.WriteLine("\n--- Testing FormatHealthMilestoneColored ---");

            var actor = TestDataBuilders.Character().WithName("Actor").Build();

            var milestoneText = CombatResults.FormatHealthMilestoneColored(actor, 0.5);

            TestBase.AssertNotNull(milestoneText,
                "Health milestone text should not be null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCheckHealthMilestones()
        {
            Console.WriteLine("\n--- Testing CheckHealthMilestones ---");

            var actor = TestDataBuilders.Character().WithName("Actor").Build();

            var milestones = CombatResults.CheckHealthMilestones(actor, 10);

            TestBase.AssertNotNull(milestones,
                "Health milestones should not be null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Execution Tests

        private static void TestExecuteActionWithUIAndStatusEffectsColored()
        {
            Console.WriteLine("\n--- Testing ExecuteActionWithUIAndStatusEffectsColored ---");

            var attacker = TestDataBuilders.Character().WithName("Attacker").Build();
            var target = TestDataBuilders.Enemy().WithName("Target").Build();
            var action = TestDataBuilders.CreateMockAction("JAB");

            var result = CombatResults.ExecuteActionWithUIAndStatusEffectsColored(
                attacker, target, action, null, null, null);

            TestBase.AssertNotNull(result.mainResult.actionText,
                "Action text should not be null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(result.mainResult.rollInfo,
                "Roll info should not be null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(result.statusEffects,
                "Status effects should not be null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Regression: <see cref="CombatResults.ExecuteActionWithUIAndStatusEffectsColored"/> must not run
        /// <see cref="CombatEffectsSimplified.ApplyStatusEffects"/> a second time after <see cref="ActionExecutionFlow"/> already applied on-hit effects (was doubling weapon poison %).
        /// </summary>
        private static void TestWeaponPoisonSingleApplicationColoredExecutePath()
        {
            Console.WriteLine("\n--- Regression: weapon poison % applied once on colored execute path ---");

            RollModificationManager.GetThresholdManager().Clear();
            ActionSelector.ClearStoredRolls();

            var hero = TestDataBuilders.Character().WithName("PoisonStriker").WithStats(20, 20, 20, 20).Build();
            var venomWeapon = TestDataBuilders.Weapon()
                .WithModification(new Modification
                {
                    Effect = "weaponPoison",
                    RolledValue = 1.0,
                    MinValue = 1,
                    MaxValue = 1
                })
                .Build();
            hero.EquipItem(venomWeapon, "weapon");

            var target = TestDataBuilders.Enemy().WithName("Goblin").WithHealth(100).Build();
            target.PoisonPercentOfMaxHealth = 0;

            var jab = TestDataBuilders.CreateMockAction("JAB");
            ActionSelector.SetStoredActionRoll(hero, 20);

            _ = CombatResults.ExecuteActionWithUIAndStatusEffectsColored(hero, target, jab, null, null, null);

            TestBase.AssertTrue(Math.Abs(target.PoisonPercentOfMaxHealth - 1.0) < 0.0001,
                $"Enemy poison % should match single weaponPoison application (1%), got {target.PoisonPercentOfMaxHealth}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            ActionSelector.RemoveStoredRoll(hero);
        }

        /// <summary>
        /// Weapon <c>weaponPoison</c> applies only on critical hits; a stored natural 12 is a hit but not a crit at default thresholds.
        /// </summary>
        private static void TestWeaponPoisonNotAppliedOnNonCritColoredExecutePath()
        {
            Console.WriteLine("\n--- Regression: weapon poison skipped on non-crit colored execute path ---");

            RollModificationManager.GetThresholdManager().Clear();
            ActionSelector.ClearStoredRolls();

            var hero = TestDataBuilders.Character().WithName("PoisonStriker").WithStats(20, 20, 20, 20).Build();
            var venomWeapon = TestDataBuilders.Weapon()
                .WithModification(new Modification
                {
                    Effect = "weaponPoison",
                    RolledValue = 1.0,
                    MinValue = 1,
                    MaxValue = 1
                })
                .Build();
            hero.EquipItem(venomWeapon, "weapon");

            var target = TestDataBuilders.Enemy().WithName("Goblin").WithHealth(100).Build();
            target.PoisonPercentOfMaxHealth = 0;

            var jab = TestDataBuilders.CreateMockAction("JAB");
            ActionSelector.SetStoredActionRoll(hero, 12);

            _ = CombatResults.ExecuteActionWithUIAndStatusEffectsColored(hero, target, jab, null, null, null);

            TestBase.AssertTrue(Math.Abs(target.PoisonPercentOfMaxHealth) < 0.0001,
                $"Enemy poison % should stay 0 on non-crit, got {target.PoisonPercentOfMaxHealth}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            ActionSelector.RemoveStoredRoll(hero);
        }

        /// <summary>
        /// UX: When an action queues action-mod bonuses (e.g. SPEED_MOD / AMP_MOD), the log should show those queued mods immediately
        /// after the action line, not wait until the following roll when they are consumed.
        /// </summary>
        private static void TestQueuedActionModsShowImmediatelyAfterQueuingAction()
        {
            Console.WriteLine("\n--- UX: queued action mods show immediately after action ---");

            RollModificationManager.GetThresholdManager().Clear();
            ActionSelector.ClearStoredRolls();

            var hero = TestDataBuilders.Character().WithName("Hero").WithStats(20, 20, 20, 20).Build();
            var target = TestDataBuilders.Enemy().WithName("Target").WithHealth(200).Build();

            var hasteStrike = TestDataBuilders.CreateMockAction("HASTE STRIKE");
            hasteStrike.Type = ActionType.Attack;
            // ModifierParser.ParsePercent expects percent points (e.g. "20" = 20%).
            hasteStrike.SpeedMod = "20";

            ActionSelector.SetStoredActionRoll(hero, 15); // hit at default thresholds, not necessarily crit
            var (_, statusEffects) = CombatResults.ExecuteActionWithUIAndStatusEffectsColored(hero, target, hasteStrike, null, null, null);

            string combined = string.Join("\n", statusEffects.Select(ColoredTextRenderer.RenderAsPlainText));
            TestBase.AssertTrue(
                combined.Contains("Next attack", StringComparison.OrdinalIgnoreCase) && combined.Contains("SPD", StringComparison.OrdinalIgnoreCase),
                $"Queued SPEED_MOD should be displayed immediately after action. Got:\n{combined}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            ActionSelector.RemoveStoredRoll(hero);
        }

        #endregion
    }
}
