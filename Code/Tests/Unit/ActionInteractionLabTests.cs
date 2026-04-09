using System;
using System.Linq;
using RPGGame;
using RPGGame.ActionInteractionLab;
using RPGGame.BattleStatistics;
using RPGGame.Tests;
using RPGGame.UI;
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

            StoredActionRollMatchesGetActionRoll(ref run, ref passed, ref failed);
            LabStepRoundTrip(ref run, ref passed, ref failed);
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
            ResetLabComboZerosBothSteps(ref run, ref passed, ref failed);
            LabEnemyTurnUsesEnemyPoolNotForcedCatalog(ref run, ref passed, ref failed);
            LabCatalogSyncShowsSecondSlotWhenComboStepOne(ref run, ref passed, ref failed);
            LabNudgeComboStepWrapsStrip(ref run, ref passed, ref failed);
            AddSelectedCatalogToStripHelperAddsAction(ref run, ref passed, ref failed);
            UndoReplayPreservesComboStripEdits(ref run, ref passed, ref failed);
            LeftPanelStatAdjustment_StrArmorAndFloors(ref run, ref passed, ref failed);
            LeftPanelStatAdjustment_HeroLevelClamp(ref run, ref passed, ref failed);
            GetTotalArmorIncludesLabBonus(ref run, ref passed, ref failed);
            ActionLabWeaponFactory_BuildsWithPrefixSuffix(ref run, ref passed, ref failed);
            ActionLabWeaponFactory_FindIndexMatchesTypeAndTier(ref run, ref passed, ref failed);
            WouldNaturalRollSelectComboAction_MatchesSelectActionBasedOnRoll(ref run, ref passed, ref failed);

            TestBase.PrintSummary("ActionInteractionLabTests", run, passed, failed);
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
            lab.LabPlayer.RemoveFromCombo(combo[0]);
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

            TestBase.AssertTrue(lab.LabEnemy.GetComboActions().Count == 0, "Lab enemy starts with empty combo strip", ref run, ref passed, ref failed);

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

        private static void LabNudgeComboStepWrapsStrip(ref int run, ref int passed, ref int failed)
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
            lab.LabPlayer.ComboStep = 0;
            lab.NudgeLabPlayerComboStep(-1);
            int n = lab.LabPlayer.GetComboActions().Count;
            TestBase.AssertEqual(1, lab.LabPlayer.ComboStep % n, "Nudge -1 from slot 0 wraps to last slot", ref run, ref passed, ref failed);
            lab.NudgeLabPlayerComboStep(1);
            TestBase.AssertEqual(0, lab.LabPlayer.ComboStep % n, "Nudge +1 returns to slot 0", ref run, ref passed, ref failed);

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

        private static void ResetLabComboZerosBothSteps(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var hero = TestDataBuilders.Character().WithName("LabResetCombo").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "ResetLabComboZerosBothSteps: session null", ref run, ref passed, ref failed);
                return;
            }

            lab.LabPlayer.ComboStep = 4;
            lab.LabEnemy.ComboStep = 2;
            lab.ResetLabCombo();
            TestBase.AssertEqual(0, lab.LabPlayer.ComboStep, "ResetLabCombo zeros LabPlayer.ComboStep", ref run, ref passed, ref failed);
            TestBase.AssertEqual(0, lab.LabEnemy.ComboStep, "ResetLabCombo zeros LabEnemy.ComboStep", ref run, ref passed, ref failed);
            ActionInteractionLabSession.EndSession();
        }

        /// <summary>
        /// Enemies must use <see cref="Character.ComboStep"/> for combo picks (not random), matching hero behavior and action lab sequencing.
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
