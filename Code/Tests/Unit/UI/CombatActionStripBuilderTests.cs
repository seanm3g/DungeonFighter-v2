using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Data;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Managers;

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

            // GetStripSwingDisplayPercents: base vs effective+combo amp on second strip slot
            var twoForAmp = CreateCharacterWithTwoComboActions();
            var pdAmp = CombatActionStripBuilder.BuildPanelData(twoForAmp);
            var secondAction = twoForAmp.GetComboActions()[1];
            ActionPanelInfo panel1 = pdAmp[1];
            CombatActionStripBuilder.GetStripSwingDisplayPercents(in panel1, twoForAmp, secondAction, ActionStripDamageLineMode.BaseIntrinsic, out double baseDmgPct, out double baseSpdPct);
            TestBase.AssertTrue(
                Math.Abs(baseDmgPct - panel1.DamageBase) < 0.02 && Math.Abs(baseSpdPct - panel1.SpeedBase) < 0.02,
                "GetStripSwingDisplayPercents BaseIntrinsic uses intrinsic damage and speed %",
                ref run, ref passed, ref failed);
            double ampSlot1 = ActionUtilities.CalculateDamageMultiplier(twoForAmp, secondAction);
            CombatActionStripBuilder.GetStripSwingDisplayPercents(in panel1, twoForAmp, secondAction, ActionStripDamageLineMode.EffectiveWithComboAmp, out double effDmgPct, out _);
            TestBase.AssertTrue(
                Math.Abs(effDmgPct - panel1.DamageModified * ampSlot1) < 0.02 && ampSlot1 > 1.0001,
                "GetStripSwingDisplayPercents EffectiveWithComboAmp multiplies slot damage % by combo amp (slot 1)",
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

            // BuildPanelData: FIFO ACTION layers preview on upcoming combo slots (not only current ComboStep)
            var charThreeAction = CreateCharacterWithThreeComboActions();
            charThreeAction.ComboStep = 1;
            charThreeAction.Effects.AddPendingActionBonusesNextHeroRoll(new List<ActionAttackBonusItem> { new ActionAttackBonusItem { Type = "DAMAGE_MOD", Value = 25 } });
            charThreeAction.Effects.AddPendingActionBonusesNextHeroRoll(new List<ActionAttackBonusItem> { new ActionAttackBonusItem { Type = "DAMAGE_MOD", Value = 25 } });
            var fifoPanels = CombatActionStripBuilder.BuildPanelData(charThreeAction);
            TestBase.AssertTrue(fifoPanels != null && fifoPanels.Count >= 3
                && fifoPanels[1].DamageModified > fifoPanels[1].DamageBase
                && fifoPanels[2].DamageModified > fifoPanels[2].DamageBase,
                "BuildPanelData: two FIFO layers at ComboStep 1 buff slots 1 and 2",
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
            TestBase.AssertTrue(
                (tipJoined.Contains("Dmg ", StringComparison.Ordinal) || tipJoined.Contains("% damage", StringComparison.Ordinal))
                && tipJoined.Contains("Spd ", StringComparison.Ordinal),
                "BuildActionTooltipLines uses strip-style damage line and Spd percentage",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(tipOk != null && tipOk.Count >= 3 && tipOk[1] == "",
                "BuildActionTooltipLines inserts a blank line after action title",
                ref run, ref passed, ref failed);

            // Multihit: strip panel + tooltip use NxPct% damage (matches combat tick count basis)
            var charMultiHit = CreateCharacterWithComboAction();
            var comboMulti = charMultiHit.GetComboActions();
            if (comboMulti != null && comboMulti.Count > 0)
                comboMulti[0].Advanced.MultiHitCount = 2;
            var panelMulti = CombatActionStripBuilder.BuildPanelData(charMultiHit);
            TestBase.AssertTrue(panelMulti != null && panelMulti.Count >= 1 && panelMulti[0].EffectiveMultiHitCount == 2,
                "BuildPanelData: MultiHitCount 2 yields EffectiveMultiHitCount 2",
                ref run, ref passed, ref failed);
            string stripDmgLine = CombatActionStripBuilder.FormatSwingDamageLine(panelMulti![0].EffectiveMultiHitCount, panelMulti[0].DamageBase);
            TestBase.AssertTrue(stripDmgLine == "2x100% damage",
                "FormatSwingDamageLine shows multihit as NxPct% damage",
                ref run, ref passed, ref failed);
            var tipMulti = CombatActionStripBuilder.BuildActionTooltipLines(charMultiHit, 0, 80);
            string tipMultiJoined = tipMulti != null ? string.Join("\n", tipMulti) : "";
            TestBase.AssertTrue(tipMultiJoined.Contains("2x100% damage", StringComparison.Ordinal),
                "BuildActionTooltipLines includes multihit damage segment",
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

            string tipStrikeJoined = string.Join("\n", CombatActionStripBuilder.BuildActionTooltipLines(charWithCombo, 0, 80));
            TestBase.AssertTrue(
                !tipStrikeJoined.Contains("Weapon basic", StringComparison.Ordinal) && !tipStrikeJoined.Contains("must stay in your sequence", StringComparison.Ordinal),
                "BuildActionTooltipLines omits required-weapon-basic reminder (shown via strip name color only)",
                ref run, ref passed, ref failed);

            var charOpener = CreateCharacterWithTaggedComboAction(isOpener: true, isFinisher: false, name: "OpeningMove");
            string tipOpener = string.Join("\n", CombatActionStripBuilder.BuildActionTooltipLines(charOpener, 0, 80));
            TestBase.AssertTrue(
                tipOpener.Contains("Opener", StringComparison.Ordinal) && tipOpener.Contains("first combo slot", StringComparison.Ordinal),
                "BuildActionTooltipLines notes opener role",
                ref run, ref passed, ref failed);

            var charFinisher = CreateCharacterWithTaggedComboAction(isOpener: false, isFinisher: true, name: "ClosingMove");
            string tipFinisher = string.Join("\n", CombatActionStripBuilder.BuildActionTooltipLines(charFinisher, 0, 80));
            TestBase.AssertTrue(
                tipFinisher.Contains("Finisher", StringComparison.Ordinal) && tipFinisher.Contains("last combo slot", StringComparison.Ordinal),
                "BuildActionTooltipLines notes finisher role",
                ref run, ref passed, ref failed);

            var charMechanical = CreateCharacterWithMechanicallyRichAction();
            string tipMechanical = string.Join("\n", CombatActionStripBuilder.BuildActionTooltipLines(charMechanical, 0, 120, 80));
            TestBase.AssertTrue(
                !tipMechanical.Contains("Type Attack | Target AOE | combo action", StringComparison.Ordinal)
                && tipMechanical.Contains("Hero accuracy +2", StringComparison.Ordinal)
                && tipMechanical.Contains("Enemy accuracy -3", StringComparison.Ordinal)
                && tipMechanical.Contains("Roll dice: 2d20 TakeHighest", StringComparison.Ordinal)
                && tipMechanical.Contains("Set thresholds H=6 C=12 Cr=19 Cm=2", StringComparison.Ordinal)
                && tipMechanical.Contains("Triggers: ONHIT, ONCRITICAL", StringComparison.Ordinal)
                && tipMechanical.Contains("Statuses: Stun, Bleed +2, Poison +3% max HP, Burn +4", StringComparison.Ordinal)
                && tipMechanical.Contains("Combo route: jump to slot 3", StringComparison.Ordinal)
                && tipMechanical.Contains("Chain position bonuses: Damage +10% per AmpTier", StringComparison.Ordinal)
                && tipMechanical.Contains("Stat bonus (Dungeon): STR +2, PRIMARY +1", StringComparison.Ordinal)
                && tipMechanical.Contains("Threshold rules: Enemy Health <= 25%", StringComparison.Ordinal)
                && tipMechanical.Contains("Accumulation: HitsLanded -> Damage +5", StringComparison.Ordinal),
                "BuildActionTooltipLines includes full runtime mechanics for a complex action",
                ref run, ref passed, ref failed);

            var richAction = charMechanical.GetComboActions()[0];
            string summaryMechanical = CombatActionStripBuilder.BuildActionMechanicalModSummary(charMechanical, richAction, 0);
            TestBase.AssertTrue(
                summaryMechanical.Contains("ACTION x2: +1 ACC, +20% DMG", StringComparison.Ordinal)
                && summaryMechanical.Contains("Hero thresholds H:-1 C:+2 Cr:-1 Cm:+1", StringComparison.Ordinal)
                && summaryMechanical.Contains("Enemy thresholds H:+1 C:-2 Cr:+3 Cm:-1", StringComparison.Ordinal),
                "BuildActionMechanicalModSummary includes action-bonus and hero/enemy threshold mechanics",
                ref run, ref passed, ref failed);

            var wrap = CombatActionStripBuilder.WrapTextToLines("hello world wide", 5);
            TestBase.AssertTrue(wrap != null && wrap.Count >= 2 && wrap.TrueForAll(l => l.Length <= 5),
                "WrapTextToLines respects max width",
                ref run, ref passed, ref failed);

            var paragraphLines = new List<string>();
            CombatActionStripBuilder.AddWrappedTooltipParagraph(paragraphLines, "first paragraph", 40, 8);
            CombatActionStripBuilder.AddWrappedTooltipParagraph(paragraphLines, "second paragraph", 40, 8);
            TestBase.AssertTrue(paragraphLines.Count == 3 && paragraphLines[1] == "",
                "AddWrappedTooltipParagraph inserts one blank spacer between paragraphs",
                ref run, ref passed, ref failed);

            // Strip card tail: deferred sheet accuracy appears even when swing roll bonus is 0 (CalculateRollBonus excludes deferred RollBonus)
            var deferAcc = TestDataBuilders.CreateMockAction("TauntLike", ActionType.Attack);
            deferAcc.Cadence = "";
            deferAcc.Advanced.RollBonus = 1;
            var deferTail = CombatActionStripBuilder.BuildActionStripModifierTailLines(deferAcc, 0, 80, 8);
            string deferJoined = string.Join(" ", deferTail);
            TestBase.AssertTrue(
                deferJoined.Contains("Hero accuracy +1", StringComparison.Ordinal) && deferJoined.Contains("on hit: next roll", StringComparison.Ordinal),
                "BuildActionStripModifierTailLines lists deferred Hero accuracy when swing Acc total is 0",
                ref run, ref passed, ref failed);

            // ATTACK cadence: sheet hero accuracy is in roll total — omit duplicate Hero accuracy line
            var nowAcc = TestDataBuilders.CreateMockAction("NowAcc", ActionType.Attack);
            nowAcc.Cadence = "ATTACK";
            nowAcc.Advanced.RollBonus = 2;
            var nowTail = CombatActionStripBuilder.BuildActionStripModifierTailLines(nowAcc, 2, 80, 8);
            string nowJoined = string.Join(" ", nowTail);
            TestBase.AssertTrue(
                nowJoined.Contains("Acc +2", StringComparison.Ordinal) && !nowJoined.Contains("Hero accuracy", StringComparison.Ordinal),
                "BuildActionStripModifierTailLines omits redundant Hero accuracy when it is on the current roll",
                ref run, ref passed, ref failed);

            var statTailAction = TestDataBuilders.CreateMockAction("BuffSwing", ActionType.Attack);
            statTailAction.Cadence = "ATTACK";
            statTailAction.Advanced.StatBonuses = new List<StatBonusEntry>
            {
                new StatBonusEntry { Type = "STR", Value = 1 },
                new StatBonusEntry { Type = "HIT", Value = -1 }
            };
            var statTail = CombatActionStripBuilder.BuildActionStripModifierTailLines(statTailAction, 0, 80, 8);
            string statJoined = string.Join(" ", statTail);
            TestBase.AssertTrue(
                statJoined.Contains("Stats:", StringComparison.Ordinal) && statJoined.Contains("STR +1", StringComparison.Ordinal) && statJoined.Contains("HIT -1", StringComparison.Ordinal),
                "BuildActionStripModifierTailLines includes compact advanced stat bonuses",
                ref run, ref passed, ref failed);

            var abilityTailAction = TestDataBuilders.CreateMockAction("CadenceMix", ActionType.Attack);
            abilityTailAction.Cadence = "ATTACK";
            abilityTailAction.ActionAttackBonuses = new ActionAttackBonuses();
            abilityTailAction.ActionAttackBonuses.BonusGroups.Add(new ActionAttackBonusGroup
            {
                CadenceType = "Ability",
                Bonuses = new List<ActionAttackBonusItem> { new ActionAttackBonusItem { Type = "HIT", Value = 1 } }
            });
            abilityTailAction.ActionAttackBonuses.BonusGroups.Add(new ActionAttackBonusGroup
            {
                CadenceType = "ACTION",
                Count = 1,
                Bonuses = new List<ActionAttackBonusItem> { new ActionAttackBonusItem { Type = "ACCURACY", Value = 2 } }
            });
            var abilityTail = CombatActionStripBuilder.BuildActionStripModifierTailLines(abilityTailAction, 0, 80, 8);
            string abilityJoined = string.Join(" ", abilityTail);
            TestBase.AssertTrue(
                !abilityJoined.Contains("Ability:", StringComparison.Ordinal) && abilityJoined.Contains("ACTION:", StringComparison.Ordinal),
                "BuildActionStripModifierTailLines omits Ability cadence bonus groups but keeps other cadences",
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

        private static Character CreateCharacterWithTaggedComboAction(bool isOpener, bool isFinisher, string name)
        {
            var character = TestDataBuilders.Character().WithName("TaggedCombo").WithStats(10, 10, 10, 10).Build();
            var weapon = TestDataBuilders.Weapon().WithWeaponType(WeaponType.Wand).WithBaseDamage(5).Build();
            character.EquipItem(weapon, "weapon");
            var action = TestDataBuilders.CreateMockAction(name, ActionType.Attack);
            action.DamageMultiplier = 1.0;
            action.Length = 1.0;
            action.IsComboAction = true;
            action.ComboRouting ??= new ComboRoutingProperties();
            action.ComboRouting.IsOpener = isOpener;
            action.ComboRouting.IsFinisher = isFinisher;
            character.AddAction(action, 1.0);
            character.Actions.AddToCombo(action);
            return character;
        }

        private static Character CreateCharacterWithMechanicallyRichAction()
        {
            var character = TestDataBuilders.Character().WithName("Mechanics").WithStats(10, 10, 10, 10).Build();
            var weapon = TestDataBuilders.Weapon().WithBaseDamage(5).Build();
            character.EquipItem(weapon, "weapon");

            var action = TestDataBuilders.CreateMockAction("MechanicsMatrix", ActionType.Attack);
            action.Target = TargetType.Environment;
            action.DamageMultiplier = 1.25;
            action.Length = 0.75;
            action.IsComboAction = true;
            action.Cadence = "Dungeon";
            action.ComboBonusAmount = 1;
            action.ComboBonusDuration = 2;

            action.Advanced.RollBonus = 2;
            action.Advanced.EnemyRollBonus = -3;
            action.Advanced.RollBonusDuration = 2;
            action.Advanced.MultiHitCount = 3;
            action.Advanced.StatBonuses = new List<StatBonusEntry>
            {
                new StatBonusEntry { Type = "STR", Value = 2 },
                new StatBonusEntry { Type = "PRIMARY", Value = 1 }
            };
            action.Advanced.SkipNextTurn = true;
            action.Advanced.GuaranteeNextSuccess = true;
            action.Advanced.RepeatLastAction = true;
            action.Advanced.ConditionalDamageMultiplier = 1.5;
            action.Advanced.Thresholds = new List<ThresholdEntry>
            {
                new ThresholdEntry { Qualifier = "Enemy", Type = "Health", Operator = "<=", ValueKind = "%", Value = 0.25 }
            };
            action.Advanced.Accumulations = new List<AccumulationEntry>
            {
                new AccumulationEntry { Type = "HitsLanded", ModifiesParam = "Damage", Value = 5, ValueKind = "#" }
            };

            action.RollMods.MultipleDiceCount = 2;
            action.RollMods.MultipleDiceMode = "TakeHighest";
            action.RollMods.ExplodingDice = true;
            action.RollMods.ExplodingDiceThreshold = 19;
            action.RollMods.AllowReroll = true;
            action.RollMods.RerollChance = 0.25;
            action.RollMods.Additive = 1;
            action.RollMods.Multiplier = 1.5;
            action.RollMods.Min = 2;
            action.RollMods.Max = 18;
            action.RollMods.HitThresholdAdjustment = -1;
            action.RollMods.ComboThresholdAdjustment = 2;
            action.RollMods.CriticalHitThresholdAdjustment = -1;
            action.RollMods.CriticalMissThresholdAdjustment = 1;
            action.RollMods.EnemyHitThresholdAdjustment = 1;
            action.RollMods.EnemyComboThresholdAdjustment = -2;
            action.RollMods.EnemyCriticalHitThresholdAdjustment = 3;
            action.RollMods.EnemyCriticalMissThresholdAdjustment = -1;
            action.RollMods.HitThresholdOverride = 6;
            action.RollMods.ComboThresholdOverride = 12;
            action.RollMods.CriticalHitThresholdOverride = 19;
            action.RollMods.CriticalMissThresholdOverride = 2;
            action.RollMods.ApplyThresholdAdjustmentsToBoth = true;

            action.Triggers.TriggerConditions = new List<string> { "ONHIT", "ONCRITICAL" };
            action.CausesStun = true;
            action.CausesBleed = true;
            action.BleedAmountToAdd = 2;
            action.CausesPoison = true;
            action.PoisonPercentToAdd = 3;
            action.CausesBurn = true;
            action.BurnAmountToAdd = 4;

            action.ActionAttackBonuses = new ActionAttackBonuses();
            action.ActionAttackBonuses.BonusGroups.Add(new ActionAttackBonusGroup
            {
                Keyword = "ACTION",
                CadenceType = "ACTION",
                Count = 2,
                Bonuses = new List<ActionAttackBonusItem>
                {
                    new ActionAttackBonusItem { Type = "ACCURACY", Value = 1 },
                    new ActionAttackBonusItem { Type = "DAMAGE_MOD", Value = 20 }
                }
            });

            action.ComboRouting = new ComboRoutingProperties
            {
                JumpToSlot = 3,
                ChainPosition = "Second",
                ChainLength = "4",
                Reset = "true",
                ModifyBasedOnChainPosition = "true",
                ChainPositionBonuses = new List<ChainPositionBonusEntry>
                {
                    new ChainPositionBonusEntry { ModifiesParam = "Damage", Value = 10, ValueKind = "%", PositionBasis = "AmpTier" }
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

        private static Character CreateCharacterWithThreeComboActions()
        {
            var character = TestDataBuilders.Character().WithName("Test").WithStats(10, 10, 10, 10).Build();
            var weapon = TestDataBuilders.Weapon().WithBaseDamage(5).Build();
            character.EquipItem(weapon, "weapon");
            for (int i = 0; i < 3; i++)
            {
                var action = TestDataBuilders.CreateMockAction($"Action{i + 1}", ActionType.Attack);
                action.DamageMultiplier = 1.0;
                action.Length = 1.0;
                action.IsComboAction = true;
                character.AddAction(action, 1.0);
                character.Actions.AddToCombo(action);
            }
            return character;
        }
    }
}
