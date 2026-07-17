using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.ActionInteractionLab;
using RPGGame.BattleStatistics;
using RPGGame.Combat.Formatting;
using RPGGame.Entity.Services;
using RPGGame.Tests;
using RPGGame.UI;
using RPGGame.UI.Avalonia.ActionInteractionLab;
using RPGGame.UI.Avalonia.Display;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.ColorSystem;
using RPGGame.Utils;

namespace RPGGame.Tests.Unit.ActionInteractionLab
{
    public static class ActionInteractionLabLeftPanelTests
    {
        public static void RunAll(ref int run, ref int pass, ref int fail)
        {
            LeftPanelStatAdjustment_StrArmorAndFloors(ref run, ref pass, ref fail);
            LeftPanelStatAdjustment_ActionSlots(ref run, ref pass, ref fail);
            LeftPanelStatAdjustment_HeroHpDamageAndHeal(ref run, ref pass, ref fail);
            LeftPanelStatAdjustment_HeroLevelClamp(ref run, ref pass, ref fail);
            LeftPanelStatAdjustment_LevelUpMirrorsGameLevelUpForWeapon(ref run, ref pass, ref fail);
            LeftPanelHeroLevelSyncsLabEnemy_EnemyRowIndependent(ref run, ref pass, ref fail);
            GetTotalArmorIncludesLabBonus(ref run, ref pass, ref fail);
        }



        internal static void LeftPanelStatAdjustment_StrArmorAndFloors(ref int run, ref int passed, ref int failed)
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


        internal static void LeftPanelStatAdjustment_ActionSlots(ref int run, ref int passed, ref int failed)
        {
            var c = TestDataBuilders.Character().WithName("LabSlotAdj").Build();
            int baseSlots = ComboSequenceMaxHelper.GetEffectiveMax(c);
            string p = ActionLabLeftPanelStatAdjustment.StatHoverPrefix;
            TestBase.AssertTrue(ActionLabLeftPanelStatAdjustment.TryApply(c, p + "actionslots", +2), "slots +2", ref run, ref passed, ref failed);
            TestBase.AssertEqual(2, c.ActionLabActionSlotBonus, "slot bonus", ref run, ref passed, ref failed);
            TestBase.AssertEqual(baseSlots + 2, ComboSequenceMaxHelper.GetEffectiveMax(c), "effective slots increased", ref run, ref passed, ref failed);
            TestBase.AssertTrue(ActionLabLeftPanelStatAdjustment.TryApply(c, p + "actionslots", -5), "slots toward floor", ref run, ref passed, ref failed);
            TestBase.AssertEqual(0, c.ActionLabActionSlotBonus, "slot bonus min 0", ref run, ref passed, ref failed);
            TestBase.AssertEqual(baseSlots, ComboSequenceMaxHelper.GetEffectiveMax(c), "effective slots restored", ref run, ref passed, ref failed);
        }


        internal static void LeftPanelStatAdjustment_HeroHpDamageAndHeal(ref int run, ref int passed, ref int failed)
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


        internal static void LeftPanelStatAdjustment_HeroLevelClamp(ref int run, ref int passed, ref int failed)
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


        internal static void LeftPanelStatAdjustment_LevelUpMirrorsGameLevelUpForWeapon(ref int run, ref int passed, ref int failed)
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
        internal static void LeftPanelHeroLevelSyncsLabEnemy_EnemyRowIndependent(ref int run, ref int passed, ref int failed)
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


        internal static void GetTotalArmorIncludesLabBonus(ref int run, ref int passed, ref int failed)
        {
            var c = TestDataBuilders.Character().Build();
            int baseArmor = c.GetTotalArmor();
            c.ActionLabArmorBonus = 4;
            TestBase.AssertEqual(baseArmor + 4, c.GetTotalArmor(), "GetTotalArmor adds ActionLabArmorBonus", ref run, ref passed, ref failed);
        }
    }
}
