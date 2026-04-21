using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Tests for CombatActionStripBuilder (abilities list with dynamic info for the action strip).
    /// </summary>
    public static class CombatActionStripBuilderTests
    {
        /// <summary>
        /// Runs all CombatActionStripBuilder tests.
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== CombatActionStripBuilder Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            var nullLines = CombatActionStripBuilder.BuildLines(null!, 80, 8);
            TestBase.AssertTrue(nullLines != null && nullLines.Count == 0,
                "BuildLines(null) returns empty list",
                ref run, ref passed, ref failed);

            var characterNoActions = new Character("Test", 1);
            var noActionLines = CombatActionStripBuilder.BuildLines(characterNoActions, 80, 8);
            TestBase.AssertTrue(noActionLines != null && noActionLines.Count >= 1 && noActionLines[0] == "(No abilities)",
                "BuildLines(character with no combo actions) returns (No abilities)",
                ref run, ref passed, ref failed);

            var limitedLines = CombatActionStripBuilder.BuildLines(characterNoActions, 40, 3);
            TestBase.AssertTrue(limitedLines != null && limitedLines.Count <= 3,
                "BuildLines respects maxLines",
                ref run, ref passed, ref failed);

            var nullPanelData = CombatActionStripBuilder.BuildPanelData(null);
            TestBase.AssertTrue(nullPanelData != null && nullPanelData.Count == 0,
                "BuildPanelData(null) returns empty list",
                ref run, ref passed, ref failed);

            var noActionPanelData = CombatActionStripBuilder.BuildPanelData(characterNoActions);
            TestBase.AssertTrue(noActionPanelData != null && noActionPanelData.Count == 0,
                "BuildPanelData(character with no combo actions) returns empty",
                ref run, ref passed, ref failed);

            int selectedIndex = characterNoActions.ComboStep % 1;
            TestBase.AssertTrue(selectedIndex == 0,
                "Selected index ComboStep % count is 0 when count is 0 (no div by zero)",
                ref run, ref passed, ref failed);

            // BuildPanelData: character with combo actions returns damage, speed, thresholds
            var charWithCombo = CreateCharacterWithComboAction();
            var panelData = CombatActionStripBuilder.BuildPanelData(charWithCombo);
            TestBase.AssertTrue(panelData != null && panelData.Count >= 1,
                "BuildPanelData(character with combo) returns non-empty",
                ref run, ref passed, ref failed);
            if (panelData != null && panelData.Count >= 1)
            {
                var info = panelData[0];
                TestBase.AssertTrue(info.DamageBase >= 0 && info.SpeedBase >= 0,
                    "BuildPanelData: DamageBase and SpeedBase are non-negative",
                    ref run, ref passed, ref failed);
                TestBase.AssertTrue(Math.Abs(info.DamageBase - 100.0) < 0.001,
                    "BuildPanelData: DamageBase is action multiplier as % (1.0 mult -> 100%)",
                    ref run, ref passed, ref failed);
            }

            // Low damage multiplier (e.g. Rage-style) shows as small % of character base, not raw HP
            var charLowDmg = CreateCharacterWithComboAction(damageMultiplier: 0.1);
            var lowPanel = CombatActionStripBuilder.BuildPanelData(charLowDmg);
            TestBase.AssertTrue(lowPanel != null && lowPanel.Count >= 1 && Math.Abs(lowPanel[0].DamageBase - 10.0) < 0.001,
                "BuildPanelData: 0.1 DamageMultiplier -> DamageBase 10%",
                ref run, ref passed, ref failed);

            // BuildPanelData: no modifiers -> DamageModified == DamageBase, SpeedModified == SpeedBase
            TestBase.AssertTrue(panelData != null && panelData.Count >= 1 && panelData[0].DamageModified == panelData[0].DamageBase && panelData[0].SpeedModified == panelData[0].SpeedBase,
                "BuildPanelData: no modifiers yields base == modified (white display)",
                ref run, ref passed, ref failed);

            // BuildPanelData: positive DAMAGE_MOD on slot 0 -> DamageModified > DamageBase (green)
            charWithCombo.Effects.AddPendingActionBonuses(0, new List<ActionAttackBonusItem> { new ActionAttackBonusItem { Type = "DAMAGE_MOD", Value = 50 } });
            panelData = CombatActionStripBuilder.BuildPanelData(charWithCombo);
            TestBase.AssertTrue(panelData != null && panelData.Count >= 1 && panelData[0].DamageModified > panelData[0].DamageBase,
                "BuildPanelData: +50% DAMAGE_MOD on slot yields DamageModified > DamageBase (green)",
                ref run, ref passed, ref failed);

            // BuildPanelData: negative SPEED_MOD on slot -> lower effective speed % (red, slower)
            charWithCombo.Effects.ClearAllTempEffects();
            charWithCombo.Effects.AddPendingActionBonuses(0, new List<ActionAttackBonusItem> { new ActionAttackBonusItem { Type = "SPEED_MOD", Value = -20 } });
            panelData = CombatActionStripBuilder.BuildPanelData(charWithCombo);
            TestBase.AssertTrue(panelData != null && panelData.Count >= 1 && panelData[0].SpeedModified < panelData[0].SpeedBase,
                "BuildPanelData: -20% SPEED_MOD yields SpeedModified < SpeedBase (red, slower)",
                ref run, ref passed, ref failed);

            // BuildPanelData: +20% SPEED_MOD on slot 2 (Adrenal Surge pattern) -> slot 2 shows higher speed %
            var charWithTwoActions = CreateCharacterWithTwoComboActions();
            charWithTwoActions.Effects.AddPendingActionBonuses(1, new List<ActionAttackBonusItem> { new ActionAttackBonusItem { Type = "SPEED_MOD", Value = 20 } });
            panelData = CombatActionStripBuilder.BuildPanelData(charWithTwoActions);
            TestBase.AssertTrue(panelData != null && panelData.Count >= 2 && panelData[1].SpeedModified > panelData[1].SpeedBase,
                "BuildPanelData: +20% SPEED_MOD on slot 2 yields SpeedModified > SpeedBase (green, faster)",
                ref run, ref passed, ref failed);

            // BuildPanelData: action with threshold adjustments -> ThresholdText non-empty
            var charWithThresholdAction = CreateCharacterWithThresholdAction();
            panelData = CombatActionStripBuilder.BuildPanelData(charWithThresholdAction);
            TestBase.AssertTrue(panelData != null && panelData.Count >= 1 && !string.IsNullOrEmpty(panelData[0].ThresholdText),
                "BuildPanelData: action with threshold adjustments yields ThresholdText",
                ref run, ref passed, ref failed);

            // BuildPanelData: chain-position accuracy -> AccuracyRollBonus matches slot (strip card / tooltip basis)
            var charChainAcc = CreateCharacterWithChainAccuracyOnSlot0();
            var chainPanels = CombatActionStripBuilder.BuildPanelData(charChainAcc);
            TestBase.AssertTrue(chainPanels != null && chainPanels.Count >= 1 && chainPanels[0].AccuracyRollBonus > 0,
                "BuildPanelData: positive chain accuracy yields AccuracyRollBonus > 0 for action strip card",
                ref run, ref passed, ref failed);

            // BuildActiveModifierLines
            var nullModLines = CombatActionStripBuilder.BuildActiveModifierLines(null);
            TestBase.AssertTrue(nullModLines != null && nullModLines.Count == 0,
                "BuildActiveModifierLines(null) returns empty list",
                ref run, ref passed, ref failed);

            var emptyModLines = CombatActionStripBuilder.BuildActiveModifierLines(characterNoActions);
            TestBase.AssertTrue(emptyModLines != null && emptyModLines.Count == 0,
                "BuildActiveModifierLines(character with no bonuses) returns empty",
                ref run, ref passed, ref failed);

            var charWithBonuses = new Character("Test", 1);
            charWithBonuses.Effects.AddPendingActionBonusesNextHeroRoll(new List<ActionAttackBonusItem> { new ActionAttackBonusItem { Type = "COMBO", Value = 3 } });
            var modLines = CombatActionStripBuilder.BuildActiveModifierLines(charWithBonuses);
            TestBase.AssertTrue(modLines != null && modLines.Count >= 1 && modLines[0].Contains("Next roll:") && modLines[0].Contains("COMBO"),
                "BuildActiveModifierLines shows pending ACTION bonus as next roll",
                ref run, ref passed, ref failed);

            // BuildActionTooltipLines + word wrap (hover tooltip content)
            var tipNull = CombatActionStripBuilder.BuildActionTooltipLines(null, 0, 40);
            TestBase.AssertTrue(tipNull != null && tipNull.Count == 0,
                "BuildActionTooltipLines(null, ...) returns empty",
                ref run, ref passed, ref failed);

            var tipBad = CombatActionStripBuilder.BuildActionTooltipLines(charWithCombo, 50, 40);
            TestBase.AssertTrue(tipBad != null && tipBad.Count == 0,
                "BuildActionTooltipLines with out-of-range index returns empty",
                ref run, ref passed, ref failed);

            var tipOk = CombatActionStripBuilder.BuildActionTooltipLines(charWithCombo, 0, 40);
            TestBase.AssertTrue(tipOk != null && tipOk.Count >= 1 && tipOk[0].IndexOf("Strike", StringComparison.Ordinal) >= 0,
                "BuildActionTooltipLines includes action name",
                ref run, ref passed, ref failed);
            string tipJoined = tipOk != null ? string.Join("\n", tipOk) : "";
            TestBase.AssertTrue(!tipJoined.Contains("Accuracy:", StringComparison.Ordinal) && !tipJoined.Contains("Effective:", StringComparison.Ordinal),
                "BuildActionTooltipLines has no accuracy / effective combat-readout lines",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(tipJoined.Contains("Dmg ", StringComparison.Ordinal) && tipJoined.Contains("Spd ", StringComparison.Ordinal),
                "BuildActionTooltipLines uses strip-style Dmg/Spd percentages",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(!tipJoined.Contains("(Normal)", StringComparison.Ordinal),
                "BuildActionTooltipLines omits speed flavor labels from action details",
                ref run, ref passed, ref failed);

            var charSheetMods = CreateCharacterWithComboAction();
            var comboActs = charSheetMods.GetComboActions();
            if (comboActs != null && comboActs.Count > 0)
            {
                comboActs[0].EnemyDamageMod = "10";
                comboActs[0].AmpMod = "10";
            }
            var tipSheet = CombatActionStripBuilder.BuildActionTooltipLines(charSheetMods, 0, 80);
            string tipSheetJoined = tipSheet != null ? string.Join("\n", tipSheet) : "";
            TestBase.AssertTrue(tipSheetJoined.Contains("enemy damage +10%", StringComparison.Ordinal)
                && tipSheetJoined.Contains("Hero Amp +10%", StringComparison.Ordinal),
                "BuildActionTooltipLines lists spreadsheet hero/enemy mods on separate friendly lines",
                ref run, ref passed, ref failed);

            var charFlavor = CreateCharacterWithComboAction();
            var acts = charFlavor.GetComboActions();
            if (acts != null && acts.Count > 0)
                acts[0].Description = "NARRATIVE_FLAVOR_ONLY_XYZ";
            var tipFlavor = CombatActionStripBuilder.BuildActionTooltipLines(charFlavor, 0, 80);
            string tipFlavorJoined = tipFlavor != null ? string.Join("\n", tipFlavor) : "";
            TestBase.AssertTrue(!tipFlavorJoined.Contains("NARRATIVE_FLAVOR_ONLY_XYZ", StringComparison.Ordinal),
                "BuildActionTooltipLines omits narrative Action.Description",
                ref run, ref passed, ref failed);

            var wrap = CombatActionStripBuilder.WrapTextToLines("hello world wide", 5);
            TestBase.AssertTrue(wrap != null && wrap.Count >= 2 && wrap.TrueForAll(l => l.Length <= 5),
                "WrapTextToLines respects max width",
                ref run, ref passed, ref failed);

            TestBase.PrintSummary("CombatActionStripBuilder Tests", run, passed, failed);
        }

        private static Character CreateCharacterWithComboAction(double damageMultiplier = 1.0)
        {
            var character = TestDataBuilders.Character().WithName("Test").WithStats(10, 10, 10, 10).Build();
            var weapon = TestDataBuilders.Weapon().WithBaseDamage(5).Build();
            character.EquipItem(weapon, "weapon");
            var action = TestDataBuilders.CreateMockAction("Strike", ActionType.Attack);
            action.DamageMultiplier = damageMultiplier;
            action.Length = 1.0;
            action.IsComboAction = true;
            character.AddAction(action, 1.0);
            character.Actions.AddToCombo(action);
            return character;
        }

        private static Character CreateCharacterWithThresholdAction()
        {
            var character = TestDataBuilders.Character().WithName("Test").WithStats(10, 10, 10, 10).Build();
            var weapon = TestDataBuilders.Weapon().WithBaseDamage(5).Build();
            character.EquipItem(weapon, "weapon");
            var action = TestDataBuilders.CreateMockAction("PreciseStrike", ActionType.Attack);
            action.DamageMultiplier = 1.0;
            action.Length = 1.0;
            action.IsComboAction = true;
            action.RollMods.HitThresholdAdjustment = 2;
            action.RollMods.ComboThresholdAdjustment = 1;
            character.AddAction(action, 1.0);
            character.Actions.AddToCombo(action);
            return character;
        }

        /// <summary>Single combo action with +1 chain accuracy at slot 0 (empty position basis, value 1).</summary>
        private static Character CreateCharacterWithChainAccuracyOnSlot0()
        {
            var character = TestDataBuilders.Character().WithName("ChainAccStrip").WithStats(10, 10, 10, 10).Build();
            var weapon = TestDataBuilders.Weapon().WithBaseDamage(5).Build();
            character.EquipItem(weapon, "weapon");
            var action = TestDataBuilders.CreateMockAction("AnimeCharge", ActionType.Attack);
            action.DamageMultiplier = 1.0;
            action.Length = 1.0;
            action.IsComboAction = true;
            action.ComboRouting = new ComboRoutingProperties
            {
                ModifyBasedOnChainPosition = "true",
                ChainPositionBonuses = new List<ChainPositionBonusEntry>
                {
                    new ChainPositionBonusEntry { ModifiesParam = "Accuracy", Value = 1, ValueKind = "#", PositionBasis = "" }
                }
            };
            character.AddAction(action, 1.0);
            character.Actions.AddToCombo(action);
            return character;
        }

        private static Character CreateCharacterWithTwoComboActions()
        {
            var character = TestDataBuilders.Character().WithName("Test").WithStats(10, 10, 10, 10).Build();
            var weapon = TestDataBuilders.Weapon().WithBaseDamage(5).Build();
            character.EquipItem(weapon, "weapon");
            var action1 = TestDataBuilders.CreateMockAction("AdrenalSurge", ActionType.Attack);
            action1.DamageMultiplier = 1.0;
            action1.Length = 0.9;
            action1.IsComboAction = true;
            var action2 = TestDataBuilders.CreateMockAction("Rage", ActionType.Attack);
            action2.DamageMultiplier = 1.0;
            action2.Length = 0.5;
            action2.IsComboAction = true;
            character.AddAction(action1, 1.0);
            character.AddAction(action2, 1.0);
            character.Actions.AddToCombo(action1);
            character.Actions.AddToCombo(action2);
            return character;
        }
    }
}
