using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.ActionInteractionLab;
using RPGGame.BattleStatistics;
using RPGGame.Combat.Formatting;
using RPGGame.Tests;
using RPGGame.UI;
using RPGGame.UI.Avalonia.ActionInteractionLab;
using RPGGame.UI.Avalonia.Display;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.Utils;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Tests for developer action lab helpers and <see cref="ActionSelector.SetStoredActionRoll"/>.
    /// </summary>
    public static class ActionInteractionLabTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;

            // AsyncLocal lab sim rolls and static test rolls can leak ordering-sensitive tests across earlier suites.
            Dice.ClearAsyncLabEncounterTestRoll();
            Dice.ClearTestRoll();

            StoredActionRollMatchesGetActionRoll(ref run, ref passed, ref failed);
            LabStepRoundTrip(ref run, ref passed, ref failed);
            LabSessionBeginsWithDefaultD20Selection16(ref run, ref passed, ref failed);
            LabSessionBegin_PicksRandomLoaderEnemyWhenCatalogPopulated(ref run, ref passed, ref failed);
            ResolveD20ForNextStepUsesSelectedWhenNotRandom(ref run, ref passed, ref failed);
            LabPlayerIsActiveForDisplayWhenInLabState(ref run, ref passed, ref failed);
            SetLabEnemyFromLoaderSwapsEnemy(ref run, ref passed, ref failed);
            CanvasContextRestoredAfterEndSession(ref run, ref passed, ref failed);
            LabCatalogSelectionAddsActionToCombo(ref run, ref passed, ref failed);
            LabRemoveFromComboShrinksSequence(ref run, ref passed, ref failed);
            EnemyComboSelectionUsesComboStepIndex(ref run, ref passed, ref failed);
            LabEnemyUsesPlayerComboStripForAmplification(ref run, ref passed, ref failed);
            ActionLabCatalogSync_EnemyNextUsesPlayerStrip(ref run, ref passed, ref failed);
            ActionLabCatalogSync_EnemyNextUsesPlayerStripWhenEnemyHasCombo(ref run, ref passed, ref failed);
            LabSessionSyncCatalogMatchesComputeHelper(ref run, ref passed, ref failed);
            ResetLabEncounterZerosBothSteps(ref run, ref passed, ref failed);
            ResetLabEncounterAsync_ClearsHistoryHpEffectsKeepsStripEnemy(ref run, ref passed, ref failed);
            LabEnemyTurnUsesEnemyPoolNotForcedCatalog(ref run, ref passed, ref failed);
            LabTotalActionTicks_StepUndoSimAndFightReset(ref run, ref passed, ref failed);
            LabCatalogSyncShowsSecondSlotWhenComboStepOne(ref run, ref passed, ref failed);
            LabNudgeComboStepClampsStrip(ref run, ref passed, ref failed);
            AddSelectedCatalogToStripHelperAddsAction(ref run, ref passed, ref failed);
            UndoReplayPreservesComboStripEdits(ref run, ref passed, ref failed);
            UndoReplayPreservesLabStatEdits(ref run, ref passed, ref failed);
            LeftPanelStatAdjustment_StrArmorAndFloors(ref run, ref passed, ref failed);
            LeftPanelStatAdjustment_HeroHpDamageAndHeal(ref run, ref passed, ref failed);
            LeftPanelStatAdjustment_HeroLevelClamp(ref run, ref passed, ref failed);
            LeftPanelStatAdjustment_LevelUpMirrorsGameLevelUpForWeapon(ref run, ref passed, ref failed);
            LeftPanelHeroLevelSyncsLabEnemy_EnemyRowIndependent(ref run, ref passed, ref failed);
            GetTotalArmorIncludesLabBonus(ref run, ref passed, ref failed);
            ActionLabWeaponFactory_BuildsWithPrefixSuffix(ref run, ref passed, ref failed);
            ActionLabWeaponFactory_BuildsWithMultiplePrefixesAndSuffixes(ref run, ref passed, ref failed);
            ActionLabWeaponFactory_FindIndexMatchesTypeAndTier(ref run, ref passed, ref failed);
            ActionLabArmorFactory_BuildsWithPrefixSuffix(ref run, ref passed, ref failed);
            ActionLabArmorFactory_BuildsWithMultiplePrefixesAndSuffixes(ref run, ref passed, ref failed);
            ActionLabArmorFactory_FindIndexMatchesSlotAndTier(ref run, ref passed, ref failed);
            ActionLabArmorFactory_FilterMapsBodyToChest(ref run, ref passed, ref failed);
            ActionLabGearCatalogFilter_Basics(ref run, ref passed, ref failed);
            ClearLabGear_UnequipsSlot(ref run, ref passed, ref failed);
            WouldNaturalRollSelectComboAction_MatchesSelectActionBasedOnRoll(ref run, ref passed, ref failed);
            ApplyCatalogScrollOffsetDelta_Clamps(ref run, ref passed, ref failed);
            EncounterSimulationBatchCount_ClampedTiers(ref run, ref passed, ref failed);
            UseParallelEncounterSimulation_DefaultsTrueAndMutable(ref run, ref passed, ref failed);
            RightPanelEnemyLabHover_IdFormat(ref run, ref passed, ref failed);
            EnemyLevelCaption_ShowsHeroDelta(ref run, ref passed, ref failed);
            RightPanelEnemyAdjustment_TryApplyWrongId(ref run, ref passed, ref failed);
            LabSession_ApplyLabEnemyLevelDelta_RebuildsDummy(ref run, ref passed, ref failed);
            CaptureSimulationSnapshot_IncludesEnemyLevel(ref run, ref passed, ref failed);
            TestCharacterFactory_DirectEnemy_ScalesByLevel(ref run, ref passed, ref failed);
            DirectStatEnemy_GetEffectiveStrengthUsesDamage(ref run, ref passed, ref failed);
            DirectStatEnemy_CombatLogSpeedMatchesPanelSeconds(ref run, ref passed, ref failed);

            TestBase.PrintSummary("ActionInteractionLabTests", run, passed, failed);
        }

        private static void LabSessionBeginsWithDefaultD20Selection16(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var hero = TestDataBuilders.Character().WithName("LabDefaultD20").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            TestBase.AssertTrue(lab != null, "Lab session exists for default d20", ref run, ref passed, ref failed);
            if (lab == null)
            {
                ActionInteractionLabSession.EndSession();
                return;
            }

            TestBase.AssertFalse(lab.UseRandomD20PerStep, "Lab starts with fixed d20 mode (not random)", ref run, ref passed, ref failed);
            TestBase.AssertEqual(16, lab.SelectedD20, "Lab default SelectedD20 is 16", ref run, ref passed, ref failed);
            TestBase.AssertEqual(16, lab.ResolveD20ForNextStep(), "ResolveD20ForNextStep uses default SelectedD20", ref run, ref passed, ref failed);
            ActionInteractionLabSession.EndSession();
        }

        private static void LabSessionBegin_PicksRandomLoaderEnemyWhenCatalogPopulated(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            EnemyLoader.LoadEnemies();
            var types = EnemyLoader.GetAllEnemyTypes();
            if (types.Count == 0)
            {
                TestBase.AssertTrue(true, "LabSessionBegin_PicksRandomLoaderEnemy skipped (no Enemies.json)", ref run, ref passed, ref failed);
                return;
            }

            var hero = TestDataBuilders.Character().WithName("LabRandFoe").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "LabSessionBegin_PicksRandomLoaderEnemy: session null", ref run, ref passed, ref failed);
                return;
            }

            var snap = lab.CaptureSimulationSnapshot();
            TestBase.AssertTrue(!string.IsNullOrEmpty(snap.SessionEnemyLoaderType), "Begin sets loader enemy type", ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                types.Any(t => string.Equals(t, snap.SessionEnemyLoaderType, StringComparison.OrdinalIgnoreCase)),
                "Snapshot enemy type is from catalog",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual(1, snap.EnemyLevel, "Begin uses level 1 loader enemy", ref run, ref passed, ref failed);

            types.Sort(StringComparer.OrdinalIgnoreCase);
            int idx = types.FindIndex(t => string.Equals(t, snap.SessionEnemyLoaderType, StringComparison.OrdinalIgnoreCase));
            TestBase.AssertTrue(idx >= 0, "Selected type in sorted list", ref run, ref passed, ref failed);
            int maxScroll = Math.Max(0, types.Count - ActionInteractionLabSession.EnemyCatalogVisibleRowCount);
            TestBase.AssertTrue(lab.EnemyCatalogScrollOffset >= 0 && lab.EnemyCatalogScrollOffset <= maxScroll, "Enemy scroll offset clamped", ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                lab.EnemyCatalogScrollOffset <= idx && idx < lab.EnemyCatalogScrollOffset + ActionInteractionLabSession.EnemyCatalogVisibleRowCount,
                "Selected enemy row is in visible scroll window",
                ref run, ref passed, ref failed);

            ActionInteractionLabSession.EndSession();
        }

        /// <summary>
        /// <see cref="ActionSelector.WouldNaturalRollSelectComboAction"/> must agree with
        /// <see cref="ActionSelector.SelectActionBasedOnRoll"/> for every d20 on a typical lab hero.
        /// </summary>
        private static void WouldNaturalRollSelectComboAction_MatchesSelectActionBasedOnRoll(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var jab = ActionLoader.GetAction("JAB");
            if (jab == null)
            {
                TestBase.AssertTrue(true, "WouldNaturalRollSelectComboAction_MatchesSelectActionBasedOnRoll skipped (no JAB)", ref run, ref passed, ref failed);
                return;
            }

            var hero = TestDataBuilders.Character().WithName("LabComboPred").Build();
            if (!jab.IsComboAction)
                jab.IsComboAction = true;
            hero.AddAction(jab, 1.0);
            hero.Actions.AddToCombo(jab);

            for (int r = 1; r <= 20; r++)
            {
                Dice.SetTestRoll(r);
                ActionSelector.ClearStoredRolls();
                var sel = ActionSelector.SelectActionBasedOnRoll(hero);
                bool expectCombo = sel != null && sel.IsComboAction;
                bool pred = ActionSelector.WouldNaturalRollSelectComboAction(hero, r);
                TestBase.AssertEqual(expectCombo, pred, $"WouldNaturalRollSelectComboAction roll {r}", ref run, ref passed, ref failed);
            }

            Dice.ClearTestRoll();
            ActionSelector.ClearStoredRolls();
        }

        private static void ApplyCatalogScrollOffsetDelta_Clamps(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var hero = TestDataBuilders.Character().WithName("LabCatalogScroll").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "ApplyCatalogScrollOffsetDelta_Clamps: session null", ref run, ref passed, ref failed);
                return;
            }

            var names = ActionLoader.GetAllActionNames();
            names.Sort(StringComparer.OrdinalIgnoreCase);
            if (names.Count == 0)
            {
                ActionInteractionLabSession.EndSession();
                TestBase.AssertTrue(true, "ApplyCatalogScrollOffsetDelta_Clamps skipped (no actions)", ref run, ref passed, ref failed);
                return;
            }

            lab.LastCatalogVisibleRowCount = 4;
            int maxScroll = Math.Max(0, names.Count - 4);
            lab.CatalogScrollOffset = 0;
            ActionLabInputCoordinator.ApplyCatalogScrollOffsetDelta(lab, -5, null);
            TestBase.AssertEqual(0, lab.CatalogScrollOffset, "negative delta clamps at 0", ref run, ref passed, ref failed);

            lab.CatalogScrollOffset = maxScroll;
            ActionLabInputCoordinator.ApplyCatalogScrollOffsetDelta(lab, 5, null);
            TestBase.AssertEqual(maxScroll, lab.CatalogScrollOffset, "positive delta clamps at maxScroll", ref run, ref passed, ref failed);

            lab.CatalogScrollOffset = 1;
            ActionLabInputCoordinator.ApplyCatalogScrollOffsetDelta(lab, -1, null);
            TestBase.AssertEqual(0, lab.CatalogScrollOffset, "single step toward earlier names", ref run, ref passed, ref failed);

            ActionInteractionLabSession.EndSession();
        }

        private static void EncounterSimulationBatchCount_ClampedTiers(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var hero = TestDataBuilders.Character().WithName("LabSimBatch").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "EncounterSimulationBatchCount_ClampedTiers: session null", ref run, ref passed, ref failed);
                return;
            }

            TestBase.AssertEqual(ActionLabEncounterSimulator.DefaultBatchEncounterCount, lab.EncounterSimulationBatchCount, "lab starts at default batch count", ref run, ref passed, ref failed);
            lab.CycleEncounterSimulationBatchCount(1);
            TestBase.AssertEqual(ActionLabEncounterSimulator.DefaultBatchEncounterCount, lab.EncounterSimulationBatchCount, "clamp up at max tier (no wrap to 1)", ref run, ref passed, ref failed);
            lab.CycleEncounterSimulationBatchCount(-1);
            TestBase.AssertEqual(100, lab.EncounterSimulationBatchCount, "one step down from 1000", ref run, ref passed, ref failed);
            lab.EncounterSimulationBatchCount = 1;
            lab.CycleEncounterSimulationBatchCount(-1);
            TestBase.AssertEqual(1, lab.EncounterSimulationBatchCount, "clamp down at min tier (no wrap to 1000)", ref run, ref passed, ref failed);
            lab.EncounterSimulationBatchCount = 7;
            lab.CycleEncounterSimulationBatchCount(1);
            TestBase.AssertEqual(ActionLabEncounterSimulator.DefaultBatchEncounterCount, lab.EncounterSimulationBatchCount, "non-tier count falls back to default index then +1 clamps at 1000", ref run, ref passed, ref failed);

            ActionInteractionLabSession.EndSession();
        }

        private static void UseParallelEncounterSimulation_DefaultsTrueAndMutable(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var hero = TestDataBuilders.Character().WithName("LabSimPar").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "UseParallelEncounterSimulation_DefaultsTrueAndMutable: session null", ref run, ref passed, ref failed);
                return;
            }

            TestBase.AssertTrue(lab.UseParallelEncounterSimulation, "lab starts with parallel encounter simulation", ref run, ref passed, ref failed);
            lab.UseParallelEncounterSimulation = false;
            TestBase.AssertFalse(lab.UseParallelEncounterSimulation, "parallel flag can be cleared", ref run, ref passed, ref failed);
            lab.UseParallelEncounterSimulation = true;
            TestBase.AssertTrue(lab.UseParallelEncounterSimulation, "parallel flag can be restored", ref run, ref passed, ref failed);

            ActionInteractionLabSession.EndSession();
        }

        private static void StoredActionRollMatchesGetActionRoll(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var character = TestDataBuilders.Character().WithName("LabRoll").Build();
            ActionSelector.ClearStoredRolls();
            ActionSelector.SetStoredActionRoll(character, 17);
            int roll = ActionSelector.GetActionRoll(character);
            TestBase.AssertEqual(17, roll, "GetActionRoll returns SetStoredActionRoll value", ref run, ref passed, ref failed);
            ActionSelector.ClearStoredRolls();
        }

        private static void LabStepRoundTrip(ref int run, ref int passed, ref int failed)
        {
            var step = new LabStep(12, "Jab");
            TestBase.AssertEqual(12, step.D20, "LabStep D20", ref run, ref passed, ref failed);
            TestBase.AssertTrue(string.Equals(step.ForcedActionName, "Jab", StringComparison.Ordinal), "LabStep name", ref run, ref passed, ref failed);
        }

        private static void LeftPanelStatAdjustment_StrArmorAndFloors(ref int run, ref int passed, ref int failed)
        {
            var c = TestDataBuilders.Character().WithName("LabStatAdj").Build();
            int s0 = c.Stats.Strength;
            string p = ActionLabLeftPanelStatAdjustment.StatHoverPrefix;
            TestBase.AssertTrue(ActionLabLeftPanelStatAdjustment.TryApply(c, p + "str", +1), "str +1 applies", ref run, ref passed, ref failed);
            TestBase.AssertEqual(s0 + 1, c.Stats.Strength, "STR bumped", ref run, ref passed, ref failed);
            TestBase.AssertTrue(ActionLabLeftPanelStatAdjustment.TryApply(c, p + "armor", +2), "armor +2", ref run, ref passed, ref failed);
            TestBase.AssertEqual(2, c.ActionLabArmorBonus, "armor bonus", ref run, ref passed, ref failed);
            TestBase.AssertTrue(!ActionLabLeftPanelStatAdjustment.TryApply(c, "not_a_stat", +1), "reject unknown id", ref run, ref passed, ref failed);
            TestBase.AssertTrue(ActionLabLeftPanelStatAdjustment.TryApply(c, p + "str", -10_000), "str large negative applies", ref run, ref passed, ref failed);
            TestBase.AssertEqual(1, c.Stats.Strength, "STR min 1", ref run, ref passed, ref failed);
            TestBase.AssertTrue(ActionLabLeftPanelStatAdjustment.TryApply(c, p + "armor", -5), "armor toward floor", ref run, ref passed, ref failed);
            TestBase.AssertEqual(0, c.ActionLabArmorBonus, "armor min 0", ref run, ref passed, ref failed);
        }

        private static void LeftPanelStatAdjustment_HeroHpDamageAndHeal(ref int run, ref int passed, ref int failed)
        {
            var c = TestDataBuilders.Character().WithName("LabHpClick").Build();
            TestBase.AssertEqual(LeftPanelHoverState.Prefix + "hero:hp", ActionLabLeftPanelStatAdjustment.HeroHpHoverId, "HeroHpHoverId matches panel", ref run, ref passed, ref failed);
            int max = c.GetEffectiveMaxHealth();
            c.CurrentHealth = max;
            ActionLabLeftPanelStatAdjustment.ApplyHeroHpClickDamagePercent(c);
            int loss = System.Math.Max(1, (int)System.Math.Ceiling(max * 0.05));
            TestBase.AssertEqual(max - loss, c.CurrentHealth, "left-click style 5% max HP damage", ref run, ref passed, ref failed);
            c.CurrentHealth = 3;
            ActionLabLeftPanelStatAdjustment.ApplyHeroHpClickDamagePercent(c);
            TestBase.AssertEqual(0, c.CurrentHealth, "current HP floors at 0", ref run, ref passed, ref failed);
            c.CurrentHealth = max - 10;
            ActionLabLeftPanelStatAdjustment.ApplyHeroHpRightClickHeal(c, 5);
            TestBase.AssertEqual(max - 5, c.CurrentHealth, "right-click +5 heal", ref run, ref passed, ref failed);
            c.CurrentHealth = max - 3;
            ActionLabLeftPanelStatAdjustment.ApplyHeroHpRightClickHeal(c, 5);
            TestBase.AssertEqual(max, c.CurrentHealth, "heal clamps to max", ref run, ref passed, ref failed);
        }

        private static void LeftPanelStatAdjustment_HeroLevelClamp(ref int run, ref int passed, ref int failed)
        {
            var c = TestDataBuilders.Character().WithName("LabLvl").Build();
            string id = ActionLabLeftPanelStatAdjustment.HeroLevelHoverId;
            TestBase.AssertEqual(LeftPanelHoverState.Prefix + "hero:level", id, "HeroLevelHoverId matches panel", ref run, ref passed, ref failed);
            c.Level = 5;
            TestBase.AssertTrue(ActionLabLeftPanelStatAdjustment.TryApply(c, id, +1), "level +1", ref run, ref passed, ref failed);
            TestBase.AssertEqual(6, c.Level, "level 6", ref run, ref passed, ref failed);
            TestBase.AssertTrue(ActionLabLeftPanelStatAdjustment.TryApply(c, id, -1), "level -1", ref run, ref passed, ref failed);
            TestBase.AssertEqual(5, c.Level, "level 5", ref run, ref passed, ref failed);
            c.Level = 99;
            TestBase.AssertTrue(ActionLabLeftPanelStatAdjustment.TryApply(c, id, +1), "level at cap still handled", ref run, ref passed, ref failed);
            TestBase.AssertEqual(99, c.Level, "level stays 99", ref run, ref passed, ref failed);
            c.Level = 1;
            TestBase.AssertTrue(ActionLabLeftPanelStatAdjustment.TryApply(c, id, -1), "level at floor still handled", ref run, ref passed, ref failed);
            TestBase.AssertEqual(1, c.Level, "level stays 1", ref run, ref passed, ref failed);
        }

        private static void LeftPanelStatAdjustment_LevelUpMirrorsGameLevelUpForWeapon(ref int run, ref int passed, ref int failed)
        {
            var c = TestDataBuilders.Character().WithName("LabLvlWeapon").WithLevel(1).Build();
            var sword = TestDataBuilders.Weapon().WithWeaponType(WeaponType.Sword).Build();
            c.EquipItem(sword, "weapon");

            int str0 = c.Stats.Strength;
            int agi0 = c.Stats.Agility;
            int tec0 = c.Stats.Technique;
            int int0 = c.Stats.Intelligence;
            int hp0 = c.MaxHealth;
            int wp0 = c.Progression.WarriorPoints;

            TestBase.AssertTrue(ActionLabLeftPanelStatAdjustment.TryApply(c, ActionLabLeftPanelStatAdjustment.HeroLevelHoverId, +1), "lab level +1", ref run, ref passed, ref failed);
            TestBase.AssertEqual(2, c.Level, "level becomes 2", ref run, ref passed, ref failed);
            TestBase.AssertEqual(str0 + 1, c.Stats.Strength, "warrior level-up +1 STR", ref run, ref passed, ref failed);
            TestBase.AssertEqual(agi0 + 3, c.Stats.Agility, "warrior level-up +3 AGI", ref run, ref passed, ref failed);
            TestBase.AssertEqual(tec0 + 1, c.Stats.Technique, "warrior level-up +1 TEC", ref run, ref passed, ref failed);
            TestBase.AssertEqual(int0 + 1, c.Stats.Intelligence, "warrior level-up +1 INT", ref run, ref passed, ref failed);
            TestBase.AssertTrue(c.MaxHealth > hp0, "max health increased on level-up", ref run, ref passed, ref failed);
            TestBase.AssertEqual(wp0 + 1, c.Progression.WarriorPoints, "warrior class point awarded", ref run, ref passed, ref failed);

            TestBase.AssertTrue(ActionLabLeftPanelStatAdjustment.TryApply(c, ActionLabLeftPanelStatAdjustment.HeroLevelHoverId, -1), "lab level -1", ref run, ref passed, ref failed);
            TestBase.AssertEqual(1, c.Level, "level back to 1", ref run, ref passed, ref failed);
            TestBase.AssertEqual(str0, c.Stats.Strength, "STR restored after level-down", ref run, ref passed, ref failed);
            TestBase.AssertEqual(agi0, c.Stats.Agility, "AGI restored after level-down", ref run, ref passed, ref failed);
            TestBase.AssertEqual(tec0, c.Stats.Technique, "TEC restored after level-down", ref run, ref passed, ref failed);
            TestBase.AssertEqual(int0, c.Stats.Intelligence, "INT restored after level-down", ref run, ref passed, ref failed);
            TestBase.AssertEqual(hp0, c.MaxHealth, "max health restored after level-down", ref run, ref passed, ref failed);
            TestBase.AssertEqual(wp0, c.Progression.WarriorPoints, "warrior point removed after level-down", ref run, ref passed, ref failed);
        }

        /// <summary>
        /// Hero level clicks mirror the level delta onto the lab enemy; right-panel enemy level does not move the hero.
        /// </summary>
        private static void LeftPanelHeroLevelSyncsLabEnemy_EnemyRowIndependent(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var hero = TestDataBuilders.Character().WithName("LabHeroEnSync").WithLevel(5).Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "LeftPanelHeroLevelSyncsLabEnemy: session null", ref run, ref passed, ref failed);
                return;
            }

            int enemyStart = lab.LabEnemy.Level;
            int heroStart = lab.LabPlayer.Level;
            TestBase.AssertTrue(
                ActionLabLeftPanelStatAdjustment.TryApply(lab.LabPlayer, ActionLabLeftPanelStatAdjustment.HeroLevelHoverId, +1),
                "hero level +1",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual(heroStart + 1, lab.LabPlayer.Level, "hero +1", ref run, ref passed, ref failed);
            TestBase.AssertEqual(enemyStart + 1, lab.LabEnemy.Level, "enemy mirrored +1", ref run, ref passed, ref failed);

            int heroAfterMirror = lab.LabPlayer.Level;
            TestBase.AssertTrue(
                ActionLabRightPanelEnemyAdjustment.TryApply(lab, ActionLabRightPanelEnemyAdjustment.EnemyLevelHoverId, +1),
                "enemy row +1 only",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual(heroAfterMirror, lab.LabPlayer.Level, "enemy-only click does not change hero", ref run, ref passed, ref failed);
            TestBase.AssertEqual(enemyStart + 2, lab.LabEnemy.Level, "enemy +1 from row", ref run, ref passed, ref failed);

            lab.LabPlayer.Level = 99;
            int enemyBeforeCapClick = lab.LabEnemy.Level;
            TestBase.AssertTrue(
                ActionLabLeftPanelStatAdjustment.TryApply(lab.LabPlayer, ActionLabLeftPanelStatAdjustment.HeroLevelHoverId, +1),
                "hero at cap click still handled",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual(99, lab.LabPlayer.Level, "hero stays 99", ref run, ref passed, ref failed);
            TestBase.AssertEqual(enemyBeforeCapClick, lab.LabEnemy.Level, "enemy unchanged when hero cannot level", ref run, ref passed, ref failed);

            ActionInteractionLabSession.EndSession();
        }

        private static void GetTotalArmorIncludesLabBonus(ref int run, ref int passed, ref int failed)
        {
            var c = TestDataBuilders.Character().Build();
            int baseArmor = c.GetTotalArmor();
            c.ActionLabArmorBonus = 4;
            TestBase.AssertEqual(baseArmor + 4, c.GetTotalArmor(), "GetTotalArmor adds ActionLabArmorBonus", ref run, ref passed, ref failed);
        }

        private static void ActionLabWeaponFactory_BuildsWithPrefixSuffix(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var data = new WeaponData
            {
                Type = "Sword",
                Name = "Test Blade",
                BaseDamage = 10,
                AttackSpeed = 1.0,
                Tier = 1,
            };
            var prefix = new Modification
            {
                Name = "Sharp",
                ItemRank = "Uncommon",
                Effect = "damage",
                MinValue = 2,
                MaxValue = 2,
                DiceResult = 5,
            };
            var suffix = new StatBonus { Name = "of Power", StatType = "Damage", Value = 3 };
            var w = ActionLabWeaponFactory.CreateWeapon(data, prefix, suffix);
            TestBase.AssertTrue(w.Name.Contains("Sharp", StringComparison.Ordinal), "prefix in generated name", ref run, ref passed, ref failed);
            TestBase.AssertTrue(w.Name.Contains("of Power", StringComparison.Ordinal), "suffix in generated name", ref run, ref passed, ref failed);
            TestBase.AssertEqual(1, w.Modifications.Count, "one modification", ref run, ref passed, ref failed);
            TestBase.AssertEqual(1, w.StatBonuses.Count, "one stat bonus", ref run, ref passed, ref failed);
        }

        private static void ActionLabWeaponFactory_BuildsWithMultiplePrefixesAndSuffixes(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var data = new WeaponData
            {
                Type = "Sword",
                Name = "Test Blade",
                BaseDamage = 10,
                AttackSpeed = 1.0,
                Tier = 1,
            };
            var prefixes = new[]
            {
                new Modification
                {
                    Name = "Sharp",
                    ItemRank = "Uncommon",
                    Effect = "damage",
                    MinValue = 1,
                    MaxValue = 1,
                    DiceResult = 5,
                },
                new Modification
                {
                    Name = "Heavy",
                    ItemRank = "Common",
                    Effect = "damage",
                    MinValue = 1,
                    MaxValue = 1,
                    DiceResult = 3,
                },
            };
            var suffixes = new[]
            {
                new StatBonus { Name = "of Power", StatType = "Damage", Value = 2 },
                new StatBonus { Name = "of Speed", StatType = "Agility", Value = 1 },
            };
            var w = ActionLabWeaponFactory.CreateWeapon(data, prefixes, suffixes);
            TestBase.AssertTrue(w.Name.Contains("Sharp", StringComparison.Ordinal), "first prefix in name", ref run, ref passed, ref failed);
            TestBase.AssertTrue(w.Name.Contains("Heavy", StringComparison.Ordinal), "second prefix in name", ref run, ref passed, ref failed);
            TestBase.AssertTrue(w.Name.Contains("of Power", StringComparison.Ordinal), "first suffix in name", ref run, ref passed, ref failed);
            TestBase.AssertTrue(w.Name.Contains("of Speed", StringComparison.Ordinal), "second suffix in name", ref run, ref passed, ref failed);
            TestBase.AssertEqual(2, w.Modifications.Count, "two modifications", ref run, ref passed, ref failed);
            TestBase.AssertEqual(2, w.StatBonuses.Count, "two stat bonuses", ref run, ref passed, ref failed);
        }

        private static void ActionLabWeaponFactory_FindIndexMatchesTypeAndTier(ref int run, ref int passed, ref int failed)
        {
            var weapons = new[]
            {
                new WeaponData { Type = "Mace", Name = "Crusher", BaseDamage = 5, AttackSpeed = 1.0, Tier = 1 },
                new WeaponData { Type = "Sword", Name = "Steel Sword", BaseDamage = 8, AttackSpeed = 1.0, Tier = 2 },
            };
            var equipped = new WeaponItem("Steel Sword", 2, 8, 1.0, WeaponType.Sword);
            int idx = ActionLabWeaponFactory.FindBestWeaponDataIndex(weapons, equipped);
            TestBase.AssertEqual(1, idx, "FindBestWeaponDataIndex matches type+tier", ref run, ref passed, ref failed);
        }

        private static void ActionLabArmorFactory_BuildsWithPrefixSuffix(ref int run, ref int passed, ref int failed)
        {
            var data = new ArmorData
            {
                Slot = "head",
                Name = "Test Helm",
                Armor = 5,
                Tier = 2,
            };
            var prefix = new Modification
            {
                Name = "Sturdy",
                ItemRank = "Common",
                Effect = "armor",
                MinValue = 1,
                MaxValue = 1,
                DiceResult = 0,
            };
            var suffix = new StatBonus { Name = "of Warding", StatType = "Armor", Value = 1 };
            var item = ActionLabArmorFactory.CreateArmor(data, prefix, suffix);
            TestBase.AssertTrue(item.Name.Contains("Sturdy", StringComparison.Ordinal), "prefix in generated name", ref run, ref passed, ref failed);
            TestBase.AssertTrue(item.Name.Contains("of Warding", StringComparison.Ordinal), "suffix in generated name", ref run, ref passed, ref failed);
            TestBase.AssertEqual(1, item.Modifications.Count, "one modification", ref run, ref passed, ref failed);
            TestBase.AssertEqual(1, item.StatBonuses.Count, "one stat bonus", ref run, ref passed, ref failed);
            TestBase.AssertTrue(item is HeadItem, "head armor type", ref run, ref passed, ref failed);
        }

        private static void ActionLabArmorFactory_BuildsWithMultiplePrefixesAndSuffixes(ref int run, ref int passed, ref int failed)
        {
            var data = new ArmorData
            {
                Slot = "head",
                Name = "Test Helm",
                Armor = 5,
                Tier = 2,
            };
            var prefixes = new[]
            {
                new Modification
                {
                    Name = "Sturdy",
                    ItemRank = "Common",
                    Effect = "armor",
                    MinValue = 1,
                    MaxValue = 1,
                    DiceResult = 0,
                },
                new Modification
                {
                    Name = "Reinforced",
                    ItemRank = "Uncommon",
                    Effect = "armor",
                    MinValue = 1,
                    MaxValue = 1,
                    DiceResult = 0,
                },
            };
            var suffixes = new[]
            {
                new StatBonus { Name = "of Warding", StatType = "Armor", Value = 1 },
                new StatBonus { Name = "of Health", StatType = "Health", Value = 5 },
            };
            var item = ActionLabArmorFactory.CreateArmor(data, prefixes, suffixes);
            TestBase.AssertTrue(item.Name.Contains("Sturdy", StringComparison.Ordinal), "first prefix in name", ref run, ref passed, ref failed);
            TestBase.AssertTrue(item.Name.Contains("Reinforced", StringComparison.Ordinal), "second prefix in name", ref run, ref passed, ref failed);
            TestBase.AssertTrue(item.Name.Contains("of Warding", StringComparison.Ordinal), "first suffix in name", ref run, ref passed, ref failed);
            TestBase.AssertTrue(item.Name.Contains("of Health", StringComparison.Ordinal), "second suffix in name", ref run, ref passed, ref failed);
            TestBase.AssertEqual(2, item.Modifications.Count, "two modifications", ref run, ref passed, ref failed);
            TestBase.AssertEqual(2, item.StatBonuses.Count, "two stat bonuses", ref run, ref passed, ref failed);
        }

        private static void ActionLabArmorFactory_FindIndexMatchesSlotAndTier(ref int run, ref int passed, ref int failed)
        {
            var armors = new[]
            {
                new ArmorData { Slot = "head", Name = "Cap", Armor = 1, Tier = 1 },
                new ArmorData { Slot = "head", Name = "Helm", Armor = 3, Tier = 2 },
            };
            var equipped = new HeadItem("Helm", 2, 3);
            int idx = ActionLabArmorFactory.FindBestArmorDataIndex(armors, equipped);
            TestBase.AssertEqual(1, idx, "FindBestArmorDataIndex matches slot+tier", ref run, ref passed, ref failed);
        }

        private static void ActionLabArmorFactory_FilterMapsBodyToChest(ref int run, ref int passed, ref int failed)
        {
            var all = new List<ArmorData>
            {
                new ArmorData { Slot = "head", Name = "H", Armor = 1, Tier = 1 },
                new ArmorData { Slot = "chest", Name = "C", Armor = 2, Tier = 1 },
                new ArmorData { Slot = "feet", Name = "F", Armor = 1, Tier = 1 },
            };
            var head = ActionLabArmorFactory.FilterArmorDataForEquipSlot(all, "head");
            TestBase.AssertEqual(1, head.Count, "one head row", ref run, ref passed, ref failed);
            var body = ActionLabArmorFactory.FilterArmorDataForEquipSlot(all, "body");
            TestBase.AssertEqual(1, body.Count, "one chest row for body slot", ref run, ref passed, ref failed);
            TestBase.AssertEqual("chest", body[0].Slot, "body equip maps to chest JSON", ref run, ref passed, ref failed);
        }

        private static void ActionLabGearCatalogFilter_Basics(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(ActionLabGearCatalogFilter_Basics));
            TestBase.AssertEqual("Common", ActionLabGearCatalogFilter.GetWeaponTierRarityBand(1), "T1 band", ref run, ref passed, ref failed);
            TestBase.AssertEqual("Legendary", ActionLabGearCatalogFilter.GetWeaponTierRarityBand(5), "T5 band", ref run, ref passed, ref failed);
            var wLow = new WeaponData { Tier = 1, Type = "Sword", Name = "Rusty" };
            TestBase.AssertTrue(ActionLabGearCatalogFilter.WeaponMatchesRarityFilter(wLow, null), "weapon rarity null", ref run, ref passed, ref failed);
            TestBase.AssertTrue(ActionLabGearCatalogFilter.WeaponMatchesRarityFilter(wLow, "Common"), "T1 common", ref run, ref passed, ref failed);
            TestBase.AssertTrue(!ActionLabGearCatalogFilter.WeaponMatchesRarityFilter(wLow, "Rare"), "T1 not rare", ref run, ref passed, ref failed);
            var wHigh = new WeaponData { Tier = 5, Type = "Mace", Name = "Smash" };
            TestBase.AssertTrue(ActionLabGearCatalogFilter.WeaponMatchesRarityFilter(wHigh, "Legendary"), "T5 legendary", ref run, ref passed, ref failed);
            TestBase.AssertTrue(ActionLabGearCatalogFilter.WeaponMatchesRarityFilter(wHigh, "Mythic"), "T5 mythic", ref run, ref passed, ref failed);
            var mod = new Modification { ItemRank = "Rare", Name = "Keen" };
            TestBase.AssertTrue(ActionLabGearCatalogFilter.ModificationMatchesRarityFilter(mod, "Rare"), "mod rare", ref run, ref passed, ref failed);
            TestBase.AssertTrue(!ActionLabGearCatalogFilter.ModificationMatchesRarityFilter(mod, "Common"), "mod not common", ref run, ref passed, ref failed);
            var sbOpen = new StatBonus { Name = "of Open", ItemRank = "" };
            TestBase.AssertTrue(ActionLabGearCatalogFilter.StatBonusMatchesRarityFilter(sbOpen, "Epic"), "blank suffix rank wildcard", ref run, ref passed, ref failed);
            var sbTagged = new StatBonus { Name = "of Tagged", ItemRank = "Legendary" };
            TestBase.AssertTrue(ActionLabGearCatalogFilter.StatBonusMatchesRarityFilter(sbTagged, "Legendary"), "suffix legendary", ref run, ref passed, ref failed);
            TestBase.AssertTrue(!ActionLabGearCatalogFilter.StatBonusMatchesRarityFilter(sbTagged, "Rare"), "suffix not rare", ref run, ref passed, ref failed);
            TestBase.AssertTrue(ActionLabGearCatalogFilter.WeaponMatchesTypeFilter(wHigh, "Mace"), "type mace", ref run, ref passed, ref failed);
            TestBase.AssertTrue(!ActionLabGearCatalogFilter.WeaponMatchesTypeFilter(wHigh, "Sword"), "type not sword", ref run, ref passed, ref failed);

            TestBase.AssertEqual("Cloth", ActionLabGearCatalogFilter.GetArmorCatalogClass("Cloth Cap"), "armor class cloth cap", ref run, ref passed, ref failed);
            TestBase.AssertEqual("Studded Leather", ActionLabGearCatalogFilter.GetArmorCatalogClass("Studded Leather Boots"), "armor class studded leather", ref run, ref passed, ref failed);
            TestBase.AssertEqual("Helm", ActionLabGearCatalogFilter.GetArmorCatalogClass("Helm"), "armor class single word", ref run, ref passed, ref failed);
            var aLow = new ArmorData { Slot = "head", Name = "Cloth Cap", Armor = 1, Tier = 1 };
            TestBase.AssertTrue(ActionLabGearCatalogFilter.ArmorMatchesRarityFilter(aLow, null), "armor rarity null", ref run, ref passed, ref failed);
            TestBase.AssertTrue(ActionLabGearCatalogFilter.ArmorMatchesRarityFilter(aLow, "Common"), "armor T1 common", ref run, ref passed, ref failed);
            TestBase.AssertTrue(!ActionLabGearCatalogFilter.ArmorMatchesRarityFilter(aLow, "Rare"), "armor T1 not rare", ref run, ref passed, ref failed);
            var aHigh = new ArmorData { Slot = "chest", Name = "Plate Mail", Armor = 5, Tier = 5 };
            TestBase.AssertTrue(ActionLabGearCatalogFilter.ArmorMatchesRarityFilter(aHigh, "Mythic"), "armor T5 mythic", ref run, ref passed, ref failed);
            TestBase.AssertTrue(ActionLabGearCatalogFilter.ArmorMatchesClassFilter(aLow, null), "armor class filter null", ref run, ref passed, ref failed);
            TestBase.AssertTrue(ActionLabGearCatalogFilter.ArmorMatchesClassFilter(aLow, "Cloth"), "armor class cloth", ref run, ref passed, ref failed);
            TestBase.AssertTrue(!ActionLabGearCatalogFilter.ArmorMatchesClassFilter(aLow, "Iron"), "armor class not iron", ref run, ref passed, ref failed);

            TestBase.AssertTrue(ActionLabGearCatalogFilter.ItemMatchesTierFilter(3, null), "tier filter null matches", ref run, ref passed, ref failed);
            TestBase.AssertTrue(ActionLabGearCatalogFilter.ItemMatchesTierFilter(3, 3), "tier filter exact", ref run, ref passed, ref failed);
            TestBase.AssertTrue(!ActionLabGearCatalogFilter.ItemMatchesTierFilter(2, 3), "tier filter mismatch", ref run, ref passed, ref failed);
        }

        private static void ClearLabGear_UnequipsSlot(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var data = new ArmorData { Slot = "head", Name = "LabClearHelm", Armor = 2, Tier = 1 };
            var head = ActionLabArmorFactory.CreateArmorWithoutAffixes(data);
            var hero = TestDataBuilders.Character().WithName("LabClearGear").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                ActionInteractionLabSession.EndSession();
                TestBase.AssertTrue(false, "ClearLabGear session exists", ref run, ref passed, ref failed);
                return;
            }

            lab.ApplyLabGear(head, "head");
            TestBase.AssertTrue(lab.LabPlayer.Head != null, "ClearLabGear pre: head equipped", ref run, ref passed, ref failed);
            lab.ClearLabGear("head");
            TestBase.AssertTrue(lab.LabPlayer.Head == null, "ClearLabGear post: head empty", ref run, ref passed, ref failed);
            ActionInteractionLabSession.EndSession();
        }

        private static void RightPanelEnemyLabHover_IdFormat(ref int run, ref int passed, ref int failed)
        {
            TestBase.AssertEqual("rphover:", RightPanelEnemyLabHoverState.Prefix, "rphover prefix", ref run, ref passed, ref failed);
            TestBase.AssertEqual("rphover:enemy:level", ActionLabRightPanelEnemyAdjustment.EnemyLevelHoverId, "enemy level hover id", ref run, ref passed, ref failed);
        }

        private static void EnemyLevelCaption_ShowsHeroDelta(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(EnemyLevelCaption_ShowsHeroDelta));
            var cap = ActionLabRightPanelEnemyAdjustment.FormatEnemyLevelCaptionWithHeroDelta;
            TestBase.AssertEqual("Lvl 9 (-2)", cap(9, 11), "enemy below hero (delta = enemy − hero)", ref run, ref passed, ref failed);
            TestBase.AssertEqual("Lvl 11 (+2)", cap(11, 9), "enemy above hero", ref run, ref passed, ref failed);
            TestBase.AssertEqual("Lvl 5 (0)", cap(5, 5), "same level", ref run, ref passed, ref failed);
        }

        private static void RightPanelEnemyAdjustment_TryApplyWrongId(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var hero = TestDataBuilders.Character().WithName("LabEnAdj").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            TestBase.AssertTrue(lab != null, "session", ref run, ref passed, ref failed);
            if (lab == null)
            {
                ActionInteractionLabSession.EndSession();
                return;
            }
            int lvl0 = lab.LabEnemy.Level;
            TestBase.AssertTrue(!ActionLabRightPanelEnemyAdjustment.TryApply(lab, "not_rphover", +1), "wrong id rejected", ref run, ref passed, ref failed);
            TestBase.AssertEqual(lvl0, lab.LabEnemy.Level, "level unchanged", ref run, ref passed, ref failed);
            ActionInteractionLabSession.EndSession();
        }

        private static void LabSession_ApplyLabEnemyLevelDelta_RebuildsDummy(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var hero = TestDataBuilders.Character().WithName("LabEnLv").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                ActionInteractionLabSession.EndSession();
                return;
            }
            int hp1 = lab.LabEnemy.MaxHealth;
            lab.ApplyLabEnemyLevelDelta(1);
            TestBase.AssertEqual(2, lab.LabEnemy.Level, "enemy level 2", ref run, ref passed, ref failed);
            TestBase.AssertTrue(lab.LabEnemy.MaxHealth >= hp1, "scaled HP at least L1", ref run, ref passed, ref failed);
            TestBase.AssertTrue(ActionLabRightPanelEnemyAdjustment.TryApply(lab, ActionLabRightPanelEnemyAdjustment.EnemyLevelHoverId, -1), "TryApply -1", ref run, ref passed, ref failed);
            TestBase.AssertEqual(1, lab.LabEnemy.Level, "back to 1", ref run, ref passed, ref failed);
            ActionInteractionLabSession.EndSession();
        }

        private static void CaptureSimulationSnapshot_IncludesEnemyLevel(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var hero = TestDataBuilders.Character().WithName("LabSnapEn").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                ActionInteractionLabSession.EndSession();
                return;
            }
            lab.ApplyLabEnemyLevelDelta(2);
            var snap = lab.CaptureSimulationSnapshot();
            TestBase.AssertEqual(3, snap.EnemyLevel, "snapshot enemy level", ref run, ref passed, ref failed);
            ActionInteractionLabSession.EndSession();
        }

        private static void TestCharacterFactory_DirectEnemy_ScalesByLevel(ref int run, ref int passed, ref int failed)
        {
            var cfg = LabCombatSnapshot.DefaultTestEnemyBattleConfig;
            var e1 = TestCharacterFactory.CreateTestEnemy(cfg, 0, 1);
            var e3 = TestCharacterFactory.CreateTestEnemy(cfg, 0, 3);
            TestBase.AssertEqual(1, e1.Level, "factory L1", ref run, ref passed, ref failed);
            TestBase.AssertEqual(3, e3.Level, "factory L3", ref run, ref passed, ref failed);
            TestBase.AssertTrue(e3.MaxHealth >= e1.MaxHealth, "higher level has >= HP", ref run, ref passed, ref failed);
        }

        private static void DirectStatEnemy_GetEffectiveStrengthUsesDamage(ref int run, ref int passed, ref int failed)
        {
            var cfg = LabCombatSnapshot.DefaultTestEnemyBattleConfig;
            var e = TestCharacterFactory.CreateTestEnemy(cfg, 0, 1);
            TestBase.AssertTrue(e.UsesDirectCombatStats(), "dummy is direct-stat", ref run, ref passed, ref failed);
            TestBase.AssertTrue(e.GetEffectiveStrength() > 0, "effective strength uses damage", ref run, ref passed, ref failed);
        }

        /// <summary>
        /// Enemy subclasses Character: combat log speed must use <see cref="Enemy.GetTotalAttackSpeed"/> (right panel Spd),
        /// not <see cref="Character.GetTotalAttackSpeed"/> / SpeedCalculator, for direct-stat lab dummies.
        /// </summary>
        private static void DirectStatEnemy_CombatLogSpeedMatchesPanelSeconds(ref int run, ref int passed, ref int failed)
        {
            _ = GameConfiguration.Instance;
            ActionLoader.LoadActions();
            EnemyLoader.LoadEnemies();
            var cfg = LabCombatSnapshot.DefaultTestEnemyBattleConfig;
            var enemy = TestCharacterFactory.CreateTestEnemy(cfg, 0, 1);
            TestBase.AssertTrue(enemy.UsesDirectCombatStats(), "dummy is direct-stat for speed test", ref run, ref passed, ref failed);
            if (!enemy.UsesDirectCombatStats() || enemy.ActionPool.Count == 0)
                return;

            var strike = enemy.ActionPool[0].action;
            double panelSpd = enemy.GetTotalAttackSpeed();
            double expected = panelSpd * strike.Length;
            double actual = ActionSpeedCalculator.CalculateActualActionSpeed(enemy, strike);
            TestBase.AssertTrue(
                Math.Abs(actual - expected) < 0.02,
                $"log speed ≈ Spd×Len (expect {expected:F3}s, got {actual:F3}s)",
                ref run, ref passed, ref failed);
        }

        private static void ResolveD20ForNextStepUsesSelectedWhenNotRandom(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var hero = TestDataBuilders.Character().WithName("LabResolveD20").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            TestBase.AssertTrue(lab != null, "Lab for ResolveD20", ref run, ref passed, ref failed);
            if (lab == null)
            {
                ActionInteractionLabSession.EndSession();
                return;
            }
            lab.UseRandomD20PerStep = false;
            lab.SelectedD20 = 14;
            TestBase.AssertEqual(14, lab.ResolveD20ForNextStep(), "Fixed mode uses SelectedD20", ref run, ref passed, ref failed);
            ActionInteractionLabSession.EndSession();
        }

        private static void LabPlayerIsActiveForDisplayWhenInLabState(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            EnemyLoader.LoadEnemies();
            var stateManager = new GameStateManager();
            var hero = TestDataBuilders.Character().WithName("LabDisplayActive").Build();
            stateManager.AddCharacter(hero);
            stateManager.TransitionToState(GameState.ActionInteractionLab);
            var ctx = new CanvasContextManager();
            ctx.SetDungeonName("Dungeon X");
            ctx.SetRoomName("Room Y");
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, ctx);
            var lab = ActionInteractionLabSession.Current;
            TestBase.AssertTrue(lab != null, "Lab session exists", ref run, ref passed, ref failed);
            if (lab == null)
            {
                ActionInteractionLabSession.EndSession();
                return;
            }

            bool labPlayerActive = DisplayStateCoordinator.IsCharacterActive(lab.LabPlayer, stateManager);
            TestBase.AssertTrue(labPlayerActive, "DisplayStateCoordinator treats LabPlayer as active in lab state", ref run, ref passed, ref failed);
            ActionInteractionLabSession.EndSession();
        }

        private static void SetLabEnemyFromLoaderSwapsEnemy(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            EnemyLoader.LoadEnemies();
            var types = EnemyLoader.GetAllEnemyTypes();
            if (types.Count == 0)
            {
                TestBase.AssertTrue(true, "SetLabEnemyFromLoader skipped (no Enemies.json next to test host)", ref run, ref passed, ref failed);
                return;
            }

            var stateManager = new GameStateManager();
            var hero = TestDataBuilders.Character().WithName("LabEnemySwap").Build();
            stateManager.AddCharacter(hero);
            stateManager.TransitionToState(GameState.ActionInteractionLab);
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "SetLabEnemyFromLoader: session null", ref run, ref passed, ref failed);
                return;
            }

            string pick = types[types.Count > 1 ? 1 : 0];
            lab.SetLabEnemyFromLoader(pick, 1);
            TestBase.AssertFalse(string.IsNullOrEmpty(lab.LabEnemy.Name), "SetLabEnemyFromLoader sets enemy name", ref run, ref passed, ref failed);
            TestBase.AssertTrue(lab.LabEnemy.MaxHealth > 0, "SetLabEnemyFromLoader sets valid enemy", ref run, ref passed, ref failed);
            ActionInteractionLabSession.EndSession();
        }

        private static void CanvasContextRestoredAfterEndSession(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            EnemyLoader.LoadEnemies();
            var stateManager = new GameStateManager();
            var hero = TestDataBuilders.Character().WithName("LabCtxRestore").Build();
            stateManager.AddCharacter(hero);
            stateManager.TransitionToState(GameState.ActionInteractionLab);
            var ctx = new CanvasContextManager();
            ctx.SetCurrentCharacter(hero);
            ctx.SetDungeonName("BeforeLab");
            ctx.SetRoomName("BeforeRoom");

            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, ctx);
            TestBase.AssertTrue(ActionInteractionLabSession.Current != null, "Lab with context started", ref run, ref passed, ref failed);
            ActionInteractionLabSession.EndSession();

            TestBase.AssertTrue(ReferenceEquals(ctx.GetCurrentCharacter(), hero), "Context character restored after EndSession", ref run, ref passed, ref failed);
            TestBase.AssertEqual("BeforeLab", ctx.GetDungeonName(), "Dungeon name restored", ref run, ref passed, ref failed);
            TestBase.AssertEqual("BeforeRoom", ctx.GetRoomName(), "Room name restored", ref run, ref passed, ref failed);
        }

        /// <summary>
        /// Mirrors catalog click behavior: resolve name → GetAction → ensure combo-eligible → AddToCombo on lab clone.
        /// </summary>
        private static void LabCatalogSelectionAddsActionToCombo(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var names = ActionLoader.GetAllActionNames();
            if (names.Count == 0)
            {
                TestBase.AssertTrue(true, "LabCatalogSelectionAddsActionToCombo skipped (no actions)", ref run, ref passed, ref failed);
                return;
            }

            names.Sort(StringComparer.OrdinalIgnoreCase);
            var hero = TestDataBuilders.Character().WithName("LabComboCatalog").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "LabCatalogSelectionAddsActionToCombo: session null", ref run, ref passed, ref failed);
                return;
            }

            int before = lab.LabPlayer.GetComboActions().Count;
            string actionName = names[0];
            var action = ActionLoader.GetAction(actionName);
            TestBase.AssertTrue(action != null, "LabCatalogSelectionAddsActionToCombo GetAction", ref run, ref passed, ref failed);
            if (action != null)
            {
                if (!action.IsComboAction)
                    action.IsComboAction = true;
                lab.LabPlayer.AddToCombo(action);
            }

            int after = lab.LabPlayer.GetComboActions().Count;
            TestBase.AssertTrue(after > before, "LabCatalogSelectionAddsActionToCombo increases combo count", ref run, ref passed, ref failed);
            ActionInteractionLabSession.EndSession();
        }

        private static void LabRemoveFromComboShrinksSequence(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var hero = TestDataBuilders.Character().WithName("LabComboRemove").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "LabRemoveFromComboShrinksSequence: session null", ref run, ref passed, ref failed);
                return;
            }

            var names = ActionLoader.GetAllActionNames();
            var action = ActionLoader.GetAction("JAB");
            if (action == null && names.Count > 0)
                action = ActionLoader.GetAction(names[0]);
            TestBase.AssertTrue(action != null, "LabRemoveFromComboShrinksSequence GetAction", ref run, ref passed, ref failed);
            if (action == null)
            {
                ActionInteractionLabSession.EndSession();
                return;
            }

            if (!action.IsComboAction)
                action.IsComboAction = true;
            lab.LabPlayer.AddToCombo(action);
            var combo = lab.LabPlayer.GetComboActions();
            TestBase.AssertTrue(combo.Count > 0, "LabRemoveFromComboShrinksSequence has entry", ref run, ref passed, ref failed);
            int n = combo.Count;
            // Remove the catalog action we added (not necessarily index 0 — that slot may be a required weapon basic).
            lab.LabPlayer.RemoveFromCombo(action);
            TestBase.AssertEqual(n - 1, lab.LabPlayer.GetComboActions().Count, "LabRemoveFromComboShrinksSequence removes one", ref run, ref passed, ref failed);
            ActionInteractionLabSession.EndSession();
        }

        /// <summary>
        /// Lab enemies have no combo strip; amplification should follow the lab player's configured sequence.
        /// </summary>
        private static void LabEnemyUsesPlayerComboStripForAmplification(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var names = ActionLoader.GetAllActionNames();
            if (names.Count == 0)
            {
                TestBase.AssertTrue(true, "LabEnemyUsesPlayerComboStripForAmplification skipped (no actions)", ref run, ref passed, ref failed);
                return;
            }

            names.Sort(StringComparer.OrdinalIgnoreCase);
            var hero = TestDataBuilders.Character().WithName("LabAmpStrip").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "LabEnemyUsesPlayerComboStripForAmplification: session null", ref run, ref passed, ref failed);
                return;
            }

            var a = ActionLoader.GetAction(names[0]);
            var b = ActionLoader.GetAction(names.Count > 1 ? names[1] : names[0]);
            TestBase.AssertTrue(a != null && b != null, "LabEnemyUsesPlayerComboStripForAmplification GetAction", ref run, ref passed, ref failed);
            if (a == null || b == null)
            {
                ActionInteractionLabSession.EndSession();
                return;
            }

            if (!a.IsComboAction) a.IsComboAction = true;
            if (!b.IsComboAction) b.IsComboAction = true;
            lab.LabPlayer.AddToCombo(a);
            lab.LabPlayer.AddToCombo(b);

            // Begin() picks a random catalog enemy; strip must be empty for this amplification scenario.
            foreach (var act in lab.LabEnemy.GetComboActions().ToList())
                lab.LabEnemy.RemoveFromCombo(act);
            TestBase.AssertTrue(lab.LabEnemy.GetComboActions().Count == 0, "Lab enemy has empty combo strip for amp test", ref run, ref passed, ref failed);

            lab.LabEnemy.ComboStep = 1;
            double mult = ActionUtilities.CalculateDamageMultiplier(lab.LabEnemy, a);
            double baseAmp = lab.LabEnemy.GetComboAmplifier();
            double expected = System.Math.Pow(baseAmp, 1);
            TestBase.AssertTrue(System.Math.Abs(mult - expected) < 0.0001,
                $"Lab enemy amp should use player strip length (expect {expected:F4}, got {mult:F4})", ref run, ref passed, ref failed);

            ActionInteractionLabSession.EndSession();
        }

        /// <summary>
        /// Pure helper: when next is lab enemy with no strip, catalog name comes from hero strip index.
        /// </summary>
        private static void ActionLabCatalogSync_EnemyNextUsesPlayerStrip(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var names = ActionLoader.GetAllActionNames();
            if (names.Count < 2)
            {
                TestBase.AssertTrue(true, "ActionLabCatalogSync_EnemyNext skipped (need 2+ actions)", ref run, ref passed, ref failed);
                return;
            }

            names.Sort(StringComparer.OrdinalIgnoreCase);
            var hero = TestDataBuilders.Character().WithName("LabSyncPure").Build();
            var enemy = TestCharacterFactory.CreateTestEnemy(
                new BattleConfiguration
                {
                    PlayerDamage = 10,
                    PlayerAttackSpeed = 1.0,
                    PlayerArmor = 0,
                    PlayerHealth = 100,
                    EnemyDamage = 10,
                    EnemyAttackSpeed = 0.65,
                    EnemyArmor = 5,
                    EnemyHealth = 150
                }, 0);

            var a = ActionLoader.GetAction(names[0]);
            var b = ActionLoader.GetAction(names[1]);
            if (a == null || b == null)
            {
                TestBase.AssertTrue(false, "ActionLabCatalogSync_EnemyNext GetAction", ref run, ref passed, ref failed);
                return;
            }

            if (!a.IsComboAction) a.IsComboAction = true;
            if (!b.IsComboAction) b.IsComboAction = true;
            hero.AddToCombo(a);
            hero.AddToCombo(b);
            TestBase.AssertTrue(enemy.GetComboActions().Count == 0, "Test enemy has empty combo strip", ref run, ref passed, ref failed);

            hero.ComboStep = 1;
            string? pick = ActionLabCatalogSync.ComputeSelectedCatalogName(enemy, hero, enemy);
            TestBase.AssertTrue(
                pick != null && string.Equals(pick, hero.GetComboActions()[1].Name, StringComparison.OrdinalIgnoreCase),
                "ComputeSelectedCatalogName uses player strip when enemy next and enemy strip empty",
                ref run, ref passed, ref failed);
        }

        /// <summary>
        /// Loader enemies can have a non-empty combo strip; catalog must still follow the hero's combo step, not slot 0 on the enemy.
        /// </summary>
        private static void ActionLabCatalogSync_EnemyNextUsesPlayerStripWhenEnemyHasCombo(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var names = ActionLoader.GetAllActionNames();
            if (names.Count < 2)
            {
                TestBase.AssertTrue(true, "ActionLabCatalogSync_EnemyHasCombo skipped (need 2+ actions)", ref run, ref passed, ref failed);
                return;
            }

            names.Sort(StringComparer.OrdinalIgnoreCase);
            var hero = TestDataBuilders.Character().WithName("LabSyncEnemyCombo").Build();
            var enemy = TestCharacterFactory.CreateTestEnemy(
                new BattleConfiguration
                {
                    PlayerDamage = 10,
                    PlayerAttackSpeed = 1.0,
                    PlayerArmor = 0,
                    PlayerHealth = 100,
                    EnemyDamage = 10,
                    EnemyAttackSpeed = 0.65,
                    EnemyArmor = 5,
                    EnemyHealth = 150
                }, 0);

            var a = ActionLoader.GetAction(names[0]);
            var b = ActionLoader.GetAction(names[1]);
            if (a == null || b == null)
            {
                TestBase.AssertTrue(false, "ActionLabCatalogSync_EnemyHasCombo GetAction", ref run, ref passed, ref failed);
                return;
            }

            if (!a.IsComboAction) a.IsComboAction = true;
            if (!b.IsComboAction) b.IsComboAction = true;
            hero.AddToCombo(a);
            hero.AddToCombo(b);
            enemy.AddToCombo(a);
            enemy.AddToCombo(b);
            TestBase.AssertTrue(enemy.GetComboActions().Count >= 2, "Test enemy has combo strip", ref run, ref passed, ref failed);

            hero.ComboStep = 1;
            enemy.ComboStep = 0;
            string? pick = ActionLabCatalogSync.ComputeSelectedCatalogName(enemy, hero, enemy);
            TestBase.AssertTrue(
                pick != null && string.Equals(pick, hero.GetComboActions()[1].Name, StringComparison.OrdinalIgnoreCase),
                "ComputeSelectedCatalogName ignores enemy strip when enemy has combo; uses hero step",
                ref run, ref passed, ref failed);
        }

        private static void LabSessionSyncCatalogMatchesComputeHelper(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var names = ActionLoader.GetAllActionNames();
            if (names.Count < 2)
            {
                TestBase.AssertTrue(true, "LabSessionSyncCatalogMatchesComputeHelper skipped", ref run, ref passed, ref failed);
                return;
            }

            names.Sort(StringComparer.OrdinalIgnoreCase);
            var hero = TestDataBuilders.Character().WithName("LabSyncSession").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "LabSessionSyncCatalog: session null", ref run, ref passed, ref failed);
                return;
            }

            var a = ActionLoader.GetAction(names[0]);
            var b = ActionLoader.GetAction(names[1]);
            if (a == null || b == null)
            {
                ActionInteractionLabSession.EndSession();
                TestBase.AssertTrue(false, "LabSessionSyncCatalog GetAction", ref run, ref passed, ref failed);
                return;
            }

            if (!a.IsComboAction) a.IsComboAction = true;
            if (!b.IsComboAction) b.IsComboAction = true;
            lab.LabPlayer.AddToCombo(a);
            lab.LabPlayer.AddToCombo(b);

            lab.SyncCatalogSelectionToUpcomingActor();
            var expected = ActionLabCatalogSync.ComputeSelectedCatalogName(lab.GetNextActorToAct(), lab.LabPlayer, lab.LabEnemy);
            TestBase.AssertTrue(
                expected != null && string.Equals(lab.SelectedCatalogActionName, expected, StringComparison.OrdinalIgnoreCase),
                "Session SyncCatalogSelectionToUpcomingActor matches ComputeSelectedCatalogName",
                ref run, ref passed, ref failed);

            ActionInteractionLabSession.EndSession();
        }

        /// <summary>
        /// Lab steps pass a catalog action for the hero; enemy turns must still resolve from the enemy's <see cref="Actor.ActionPool"/>, not that name.
        /// </summary>
        private static void LabEnemyTurnUsesEnemyPoolNotForcedCatalog(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            EnemyLoader.LoadEnemies();
            var drunk = ActionLoader.GetAction("DRUNKEN BRAWLER");
            if (drunk == null)
            {
                TestBase.AssertTrue(true, "LabEnemyTurnUsesEnemyPool skipped (no DRUNKEN BRAWLER)", ref run, ref passed, ref failed);
                return;
            }

            var types = EnemyLoader.GetAllEnemyTypes();
            if (types == null || !types.Any(t => string.Equals(t, "Goblin", StringComparison.OrdinalIgnoreCase)))
            {
                TestBase.AssertTrue(true, "LabEnemyTurnUsesEnemyPool skipped (no Goblin in Enemies.json)", ref run, ref passed, ref failed);
                return;
            }

            var stateManager = new GameStateManager();
            var hero = TestDataBuilders.Character().WithName("LabEnemyPool").Build();
            stateManager.AddCharacter(hero);
            stateManager.TransitionToState(GameState.ActionInteractionLab);
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "LabEnemyTurnUsesEnemyPool: session null", ref run, ref passed, ref failed);
                return;
            }

            lab.SetLabEnemyFromLoader("Goblin", 1);
            if (!drunk.IsComboAction)
                drunk.IsComboAction = true;
            lab.LabPlayer.AddToCombo(drunk);

            // Speed order can schedule multiple player turns before the Goblin; step until the enemy actually acts.
            Action? used = null;
            for (int i = 0; i < 40; i++)
            {
                var r = lab.StepAsync(10, "DRUNKEN BRAWLER").GetAwaiter().GetResult();
                if (r != CombatSingleTurnResult.Advanced)
                    break;
                used = ActionExecutor.GetLastUsedAction(lab.LabEnemy);
                if (used != null)
                    break;
            }

            bool poolOk = used != null
                && !string.Equals(used.Name, "DRUNKEN BRAWLER", StringComparison.OrdinalIgnoreCase)
                && (string.Equals(used.Name, "JAB", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(used.Name, "TAUNT", StringComparison.OrdinalIgnoreCase));
            TestBase.AssertTrue(poolOk,
                $"Lab enemy turn should use Goblin pool (JAB/TAUNT), not catalog DRUNKEN BRAWLER; got '{used?.Name ?? "null"}'",
                ref run, ref passed, ref failed);

            ActionInteractionLabSession.EndSession();
        }

        /// <summary>
        /// <see cref="ActionInteractionLabSession.LabTotalActionTicks"/> tracks interactive steps (history length),
        /// adds encounter turn totals after batch sim, drops on undo, and resets when the lab clears fight history.
        /// </summary>
        private static void LabTotalActionTicks_StepUndoSimAndFightReset(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var jab = ActionLoader.GetAction("JAB");
            if (jab == null)
            {
                TestBase.AssertTrue(true, "LabTotalActionTicks skipped (no JAB)", ref run, ref passed, ref failed);
                return;
            }

            var hero = TestDataBuilders.Character().WithName("LabActionTicks").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "LabTotalActionTicks: session null", ref run, ref passed, ref failed);
                return;
            }

            TestBase.AssertEqual(0, lab.LabTotalActionTicks, "LabTotalActionTicks starts at 0", ref run, ref passed, ref failed);

            foreach (var a in lab.LabPlayer.GetComboActions().ToList())
                lab.LabPlayer.RemoveFromCombo(a);
            if (!jab.IsComboAction)
                jab.IsComboAction = true;
            lab.LabPlayer.AddToCombo(jab);
            lab.SelectedCatalogActionName = "JAB";

            var stepResult = lab.StepAsync(18, "JAB").GetAwaiter().GetResult();
            TestBase.AssertTrue(
                stepResult == CombatSingleTurnResult.Advanced
                || stepResult == CombatSingleTurnResult.EnemyDefeated
                || stepResult == CombatSingleTurnResult.PlayerDefeated,
                "LabTotalActionTicks step returns a recorded outcome",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual(1, lab.LabTotalActionTicks, "LabTotalActionTicks after one step", ref run, ref passed, ref failed);

            var report = new ActionLabEncounterSimulationReport();
            report.Encounters.Add(new EncounterMetrics { Turns = 3 });
            report.Encounters.Add(new EncounterMetrics { Turns = 5 });
            lab.RecordEncounterSimulationTurns(report);
            TestBase.AssertEqual(9, lab.LabTotalActionTicks, "LabTotalActionTicks includes simulated encounter turns", ref run, ref passed, ref failed);

            lab.UndoLastStepAsync().GetAwaiter().GetResult();
            TestBase.AssertEqual(8, lab.LabTotalActionTicks, "LabTotalActionTicks after undo (sim total kept)", ref run, ref passed, ref failed);

            EnemyLoader.LoadEnemies();
            var types = EnemyLoader.GetAllEnemyTypes();
            if (types == null || types.Count == 0)
            {
                TestBase.AssertTrue(true, "LabTotalActionTicks fight reset skipped (no loader enemies)", ref run, ref passed, ref failed);
            }
            else
            {
                types.Sort(StringComparer.OrdinalIgnoreCase);
                lab.SetLabEnemyFromLoader(types[0], 1);
                TestBase.AssertEqual(0, lab.LabTotalActionTicks, "LabTotalActionTicks cleared when fight history resets", ref run, ref passed, ref failed);
            }

            ActionInteractionLabSession.EndSession();
        }

        /// <summary>
        /// Catalog sync follows hero <see cref="Character.ComboStep"/>: at step 1 with a two-action strip, selection is the second action (e.g. SLAM after RAGE).
        /// </summary>
        private static void LabCatalogSyncShowsSecondSlotWhenComboStepOne(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var rage = ActionLoader.GetAction("RAGE");
            var slam = ActionLoader.GetAction("SLAM");
            if (rage == null || slam == null)
            {
                TestBase.AssertTrue(true, "LabCatalogSyncSecondSlot skipped (no RAGE/SLAM)", ref run, ref passed, ref failed);
                return;
            }

            var hero = TestDataBuilders.Character().WithName("LabCatalogStep").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "LabCatalogSyncSecondSlot: session null", ref run, ref passed, ref failed);
                return;
            }

            foreach (var a in lab.LabPlayer.GetComboActions().ToList())
                lab.LabPlayer.RemoveFromCombo(a);
            if (!rage.IsComboAction) rage.IsComboAction = true;
            if (!slam.IsComboAction) slam.IsComboAction = true;
            lab.LabPlayer.AddToCombo(rage);
            lab.LabPlayer.AddToCombo(slam);
            var combo = lab.LabPlayer.GetComboActions();
            TestBase.AssertTrue(combo.Count >= 2, "Strip should have two actions", ref run, ref passed, ref failed);
            lab.LabPlayer.ComboStep = 1;
            lab.SyncCatalogSelectionToUpcomingActor();

            string expectedSecond = combo[1].Name;
            var nextName = ActionLabCatalogSync.ComputeSelectedCatalogName(lab.GetNextActorToAct(), lab.LabPlayer, lab.LabEnemy);
            TestBase.AssertTrue(
                nextName != null && string.Equals(nextName, expectedSecond, StringComparison.OrdinalIgnoreCase),
                $"Catalog sync at ComboStep 1 should be second strip slot ({expectedSecond}); got '{nextName}'",
                ref run, ref passed, ref failed);

            ActionInteractionLabSession.EndSession();
        }

        private static void LabNudgeComboStepClampsStrip(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var rage = ActionLoader.GetAction("RAGE");
            var slam = ActionLoader.GetAction("SLAM");
            if (rage == null || slam == null)
            {
                TestBase.AssertTrue(true, "LabNudgeComboStep skipped (no RAGE/SLAM)", ref run, ref passed, ref failed);
                return;
            }

            var hero = TestDataBuilders.Character().WithName("LabNudge").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "LabNudgeComboStep: session null", ref run, ref passed, ref failed);
                return;
            }

            foreach (var a in lab.LabPlayer.GetComboActions().ToList())
                lab.LabPlayer.RemoveFromCombo(a);
            if (!rage.IsComboAction) rage.IsComboAction = true;
            if (!slam.IsComboAction) slam.IsComboAction = true;
            lab.LabPlayer.AddToCombo(rage);
            lab.LabPlayer.AddToCombo(slam);
            int n = lab.LabPlayer.GetComboActions().Count;
            lab.LabPlayer.ComboStep = 0;
            lab.NudgeLabPlayerComboStep(-1);
            TestBase.AssertEqual(0, lab.LabPlayer.ComboStep % n, "Nudge -1 from first slot stays on first slot", ref run, ref passed, ref failed);
            lab.NudgeLabPlayerComboStep(1);
            TestBase.AssertEqual(1, lab.LabPlayer.ComboStep % n, "Nudge +1 from first slot advances to second", ref run, ref passed, ref failed);
            lab.NudgeLabPlayerComboStep(1);
            TestBase.AssertEqual(1, lab.LabPlayer.ComboStep % n, "Nudge +1 from last slot stays on last slot", ref run, ref passed, ref failed);
            lab.NudgeLabPlayerComboStep(-1);
            TestBase.AssertEqual(0, lab.LabPlayer.ComboStep % n, "Nudge -1 from last slot returns to first", ref run, ref passed, ref failed);

            ActionInteractionLabSession.EndSession();
        }

        private static void AddSelectedCatalogToStripHelperAddsAction(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var names = ActionLoader.GetAllActionNames();
            if (names.Count == 0)
            {
                TestBase.AssertTrue(true, "AddSelectedCatalogToStrip skipped (no actions)", ref run, ref passed, ref failed);
                return;
            }

            names.Sort(StringComparer.OrdinalIgnoreCase);
            var hero = TestDataBuilders.Character().WithName("LabAddStrip").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "AddSelectedCatalogToStrip: session null", ref run, ref passed, ref failed);
                return;
            }

            string pick = names[names.Count > 1 ? 1 : 0];
            lab.SelectedCatalogActionName = pick;
            int before = lab.LabPlayer.GetComboActions().Count;
            lab.AddSelectedCatalogActionToComboStrip();
            int after = lab.LabPlayer.GetComboActions().Count;
            TestBase.AssertTrue(after > before, "AddSelectedCatalogActionToComboStrip increases combo count", ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                lab.LabPlayer.GetComboActions().Any(a => string.Equals(a.Name, pick, StringComparison.OrdinalIgnoreCase)),
                "Strip contains selected catalog action",
                ref run, ref passed, ref failed);

            ActionInteractionLabSession.EndSession();
        }

        /// <summary>
        /// Undo replays from initial clone JSON; combo strip edits must be reapplied so Back only removes the last combat step.
        /// </summary>
        private static void UndoReplayPreservesComboStripEdits(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var names = ActionLoader.GetAllActionNames();
            if (names.Count == 0)
            {
                TestBase.AssertTrue(true, "UndoReplayPreservesComboStripEdits skipped (no actions)", ref run, ref passed, ref failed);
                return;
            }

            names.Sort(StringComparer.OrdinalIgnoreCase);
            var hero = TestDataBuilders.Character().WithName("LabUndoStrip").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "UndoReplayPreservesComboStripEdits: session null", ref run, ref passed, ref failed);
                return;
            }

            string pick = names[names.Count > 1 ? 1 : 0];
            lab.SelectedCatalogActionName = pick;
            lab.AddSelectedCatalogActionToComboStrip();
            int comboCountBeforeStep = lab.LabPlayer.GetComboActions().Count;
            var stripNamesBefore = lab.LabPlayer.GetComboActions().Select(a => a.Name).ToList();

            lab.StepAsync(lab.ResolveD20ForNextStep(), lab.SelectedCatalogActionName).GetAwaiter().GetResult();
            lab.UndoLastStepAsync().GetAwaiter().GetResult();

            int comboCountAfterUndo = lab.LabPlayer.GetComboActions().Count;
            var stripNamesAfter = lab.LabPlayer.GetComboActions().Select(a => a.Name).ToList();

            TestBase.AssertEqual(comboCountBeforeStep, comboCountAfterUndo, "Undo preserves combo strip count", ref run, ref passed, ref failed);
            TestBase.AssertEqual(stripNamesBefore.Count, stripNamesAfter.Count, "Undo preserves strip name list length", ref run, ref passed, ref failed);
            for (int i = 0; i < stripNamesBefore.Count; i++)
            {
                TestBase.AssertTrue(
                    string.Equals(stripNamesBefore[i], stripNamesAfter[i], StringComparison.OrdinalIgnoreCase),
                    $"Undo preserves strip order [{i}]",
                    ref run, ref passed, ref failed);
            }

            ActionInteractionLabSession.EndSession();
        }

        /// <summary>
        /// Undo replay must not restore hero from stale <c>_initialPlayerJson</c> for left-panel lab stat edits.
        /// </summary>
        private static void UndoReplayPreservesLabStatEdits(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var names = ActionLoader.GetAllActionNames();
            if (names.Count == 0)
            {
                TestBase.AssertTrue(true, "UndoReplayPreservesLabStatEdits skipped (no actions)", ref run, ref passed, ref failed);
                return;
            }

            names.Sort(StringComparer.OrdinalIgnoreCase);
            var hero = TestDataBuilders.Character().WithName("LabUndoStats").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "UndoReplayPreservesLabStatEdits: session null", ref run, ref passed, ref failed);
                return;
            }

            string p = ActionLabLeftPanelStatAdjustment.StatHoverPrefix;
            TestBase.AssertTrue(ActionLabLeftPanelStatAdjustment.TryApply(lab.LabPlayer, p + "str", +4), "str +4 for undo stat test", ref run, ref passed, ref failed);
            TestBase.AssertTrue(ActionLabLeftPanelStatAdjustment.TryApply(lab.LabPlayer, p + "armor", +3), "armor +3 for undo stat test", ref run, ref passed, ref failed);
            TestBase.AssertTrue(ActionLabLeftPanelStatAdjustment.TryApply(lab.LabPlayer, ActionLabLeftPanelStatAdjustment.HeroLevelHoverId, +2), "level +2 for undo stat test", ref run, ref passed, ref failed);

            int expectStr = lab.LabPlayer.Stats.Strength;
            int expectArmorBonus = lab.LabPlayer.ActionLabArmorBonus;
            int expectLevel = lab.LabPlayer.Level;

            string pick = names[names.Count > 1 ? 1 : 0];
            lab.SelectedCatalogActionName = pick;
            lab.AddSelectedCatalogActionToComboStrip();

            lab.StepAsync(lab.ResolveD20ForNextStep(), lab.SelectedCatalogActionName).GetAwaiter().GetResult();
            lab.UndoLastStepAsync().GetAwaiter().GetResult();

            TestBase.AssertEqual(expectStr, lab.LabPlayer.Stats.Strength, "Undo preserves lab STR", ref run, ref passed, ref failed);
            TestBase.AssertEqual(expectArmorBonus, lab.LabPlayer.ActionLabArmorBonus, "Undo preserves ActionLabArmorBonus", ref run, ref passed, ref failed);
            TestBase.AssertEqual(expectLevel, lab.LabPlayer.Level, "Undo preserves lab level", ref run, ref passed, ref failed);

            ActionInteractionLabSession.EndSession();
        }

        private static void ResetLabEncounterZerosBothSteps(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var hero = TestDataBuilders.Character().WithName("LabResetCombo").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "ResetLabEncounterZerosBothSteps: session null", ref run, ref passed, ref failed);
                return;
            }

            lab.LabPlayer.ComboStep = 4;
            lab.LabEnemy.ComboStep = 2;
            lab.ResetLabEncounterAsync().GetAwaiter().GetResult();
            TestBase.AssertEqual(0, lab.LabPlayer.ComboStep, "ResetLabEncounter zeros LabPlayer.ComboStep", ref run, ref passed, ref failed);
            TestBase.AssertEqual(0, lab.LabEnemy.ComboStep, "ResetLabEncounter zeros LabEnemy.ComboStep", ref run, ref passed, ref failed);
            ActionInteractionLabSession.EndSession();
        }

        /// <summary>
        /// <see cref="ActionInteractionLabSession.ResetLabEncounterAsync"/> clears undo history and DoT, refills HP,
        /// and leaves the combo strip and current enemy unchanged.
        /// </summary>
        private static void ResetLabEncounterAsync_ClearsHistoryHpEffectsKeepsStripEnemy(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var jab = ActionLoader.GetAction("JAB");
            if (jab == null)
            {
                TestBase.AssertTrue(true, "ResetLabEncounterAsync_ClearsHistory skipped (no JAB)", ref run, ref passed, ref failed);
                return;
            }

            var hero = TestDataBuilders.Character().WithName("LabEncounterReset").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "ResetLabEncounterAsync_ClearsHistory: session null", ref run, ref passed, ref failed);
                return;
            }

            string enemyName = lab.LabEnemy.Name;
            int enemyLevel = lab.LabEnemy.Level;

            foreach (var a in lab.LabPlayer.GetComboActions().ToList())
                lab.LabPlayer.RemoveFromCombo(a, ignoreWeaponRequirement: true);
            if (!jab.IsComboAction)
                jab.IsComboAction = true;
            lab.LabPlayer.AddToCombo(jab);
            int stripCount = lab.LabPlayer.GetComboActions().Count;
            lab.SelectedCatalogActionName = "JAB";

            lab.LabPlayer.CurrentHealth = 3;
            lab.LabEnemy.CurrentHealth = 7;
            lab.LabPlayer.PoisonPercentOfMaxHealth = 4;
            lab.LabPlayer.ComboStep = 3;

            lab.StepAsync(18, "JAB").GetAwaiter().GetResult();
            TestBase.AssertTrue(lab.LabTotalActionTicks > 0, "ResetLabEncounter test: history recorded a step", ref run, ref passed, ref failed);

            lab.ResetLabEncounterAsync().GetAwaiter().GetResult();

            TestBase.AssertEqual(0, lab.History.Count, "ResetLabEncounter clears step history", ref run, ref passed, ref failed);
            TestBase.AssertEqual(0, lab.LabTotalActionTicks, "ResetLabEncounter zeros LabTotalActionTicks", ref run, ref passed, ref failed);
            TestBase.AssertEqual(lab.LabPlayer.GetEffectiveMaxHealth(), lab.LabPlayer.CurrentHealth, "ResetLabEncounter refills hero HP", ref run, ref passed, ref failed);
            TestBase.AssertEqual(lab.LabEnemy.GetEffectiveMaxHealth(), lab.LabEnemy.CurrentHealth, "ResetLabEncounter refills enemy HP", ref run, ref passed, ref failed);
            TestBase.AssertEqual(0.0, lab.LabPlayer.PoisonPercentOfMaxHealth, "ResetLabEncounter clears poison %", ref run, ref passed, ref failed);
            TestBase.AssertEqual(stripCount, lab.LabPlayer.GetComboActions().Count, "ResetLabEncounter keeps combo strip size", ref run, ref passed, ref failed);
            TestBase.AssertEqual(0, lab.LabPlayer.ComboStep, "ResetLabEncounter zeros hero combo step", ref run, ref passed, ref failed);
            TestBase.AssertTrue(string.Equals(enemyName, lab.LabEnemy.Name, StringComparison.Ordinal),
                "ResetLabEncounter keeps same enemy name", ref run, ref passed, ref failed);
            TestBase.AssertEqual(enemyLevel, lab.LabEnemy.Level, "ResetLabEncounter keeps enemy level", ref run, ref passed, ref failed);

            ActionInteractionLabSession.EndSession();
        }

        /// <summary>
        /// With effective INT ≥ threshold, enemies use <see cref="Character.ComboStep"/> for combo picks (same ordered rule as heroes).
        /// </summary>
        private static void EnemyComboSelectionUsesComboStepIndex(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            ActionSelector.ClearStoredRolls();

            var enemy = new Enemy(
                name: "ComboStepEnemy",
                level: 1,
                maxHealth: 80,
                damage: 8,
                armor: 0,
                attackSpeed: 1.0,
                primaryAttribute: PrimaryAttribute.Strength,
                isLiving: true,
                archetype: EnemyArchetype.Berserker,
                useDirectStats: true);
            enemy.Intelligence = GameConstants.ComboSequenceIntelligenceThreshold;

            var a1 = new Action("EC_ONE", comboOrder: 0, damageMultiplier: 1.0, length: 1.0, isComboAction: true);
            var a2 = new Action("EC_TWO", comboOrder: 1, damageMultiplier: 1.0, length: 1.0, isComboAction: true);
            enemy.AddToCombo(a1);
            enemy.AddToCombo(a2);

            Dice.SetTestRoll(20);
            ActionSelector.SetStoredActionRoll(enemy, 20);
            enemy.ComboStep = 0;
            var pick0 = ActionSelector.SelectActionByEntityType(enemy);
            TestBase.AssertTrue(pick0 != null && string.Equals(pick0.Name, "EC_ONE", StringComparison.Ordinal),
                "Enemy ComboStep 0 selects first combo action", ref run, ref passed, ref failed);

            enemy.ComboStep = 1;
            Dice.SetTestRoll(20);
            ActionSelector.SetStoredActionRoll(enemy, 20);
            var pick1 = ActionSelector.SelectActionByEntityType(enemy);
            TestBase.AssertTrue(pick1 != null && string.Equals(pick1.Name, "EC_TWO", StringComparison.Ordinal),
                "Enemy ComboStep 1 selects second combo action", ref run, ref passed, ref failed);

            Dice.ClearTestRoll();
            ActionSelector.ClearStoredRolls();
        }
    }
}
