using System;
using RPGGame;
using RPGGame.Actions;
using RPGGame.Combat.Calculators;
using RPGGame.Data;
using RPGGame.Items;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>HERO BASE → WEAPON DAMAGE / WEAPON SPEED cadence flats apply in combat calculators.</summary>
    public static class WeaponBaseModCadenceTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== Weapon Base Mod Cadence Tests ===\n");
            int run = 0, passed = 0, failed = 0;
            TestWeaponDamageFlat_AddsToBaseDamage(ref run, ref passed, ref failed);
            TestWeaponSpeedFlat_ShortensAttackTime(ref run, ref passed, ref failed);
            TestBuildModifierBonuses_FromWeaponFields(ref run, ref passed, ref failed);
            TestBase.PrintSummary("Weapon Base Mod Cadence Tests", run, passed, failed);
        }

        private static void TestWeaponDamageFlat_AddsToBaseDamage(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestWeaponDamageFlat_AddsToBaseDamage));
            var hero = new Character("WpnDmgHero", 1);
            hero.Weapon = new WeaponItem("TestBlade", 1, baseDamage: 10, baseAttackSpeed: 1.0);
            var target = new Character("Dummy", 1);

            DamageCalculator.ClearAllCaches();
            hero.Effects.ConsumedWeaponDamageFlat = 10;
            int withMod = DamageCalculator.CalculateDamage(hero, target, action: null, comboAmplifier: 1.0, damageMultiplier: 1.0, rollBonus: 0, roll: 10);
            DamageCalculator.ClearAllCaches();
            hero.Effects.ConsumedWeaponDamageFlat = 0;
            int without = DamageCalculator.CalculateDamage(hero, target, action: null, comboAmplifier: 1.0, damageMultiplier: 1.0, rollBonus: 0, roll: 10);

            int delta = withMod - without;
            TestBase.AssertTrue(delta >= 9 && delta <= 11,
                $"+10 WEAPON_DAMAGE raises damage by ~10 ({without} → {withMod}, delta={delta})",
                ref run, ref passed, ref failed);
        }

        private static void TestWeaponSpeedFlat_ShortensAttackTime(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestWeaponSpeedFlat_ShortensAttackTime));
            var hero = new Character("WpnSpdHero", 1);
            hero.Weapon = new WeaponItem("TestBlade", 1, baseDamage: 10, baseAttackSpeed: 1.0);

            double baseline = SpeedCalculator.CalculateAttackSpeed(hero);
            hero.Effects.ConsumedWeaponSpeedFlat = 1;
            double faster = SpeedCalculator.CalculateAttackSpeed(hero);
            hero.Effects.ConsumedWeaponSpeedFlat = 0;

            TestBase.AssertTrue(faster < baseline,
                $"+1 WEAPON_SPEED shortens attack time ({baseline:0.###} → {faster:0.###})",
                ref run, ref passed, ref failed);
        }

        private static void TestBuildModifierBonuses_FromWeaponFields(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestBuildModifierBonuses_FromWeaponFields));
            var action = new Action
            {
                Name = "SHARPEN",
                Cadence = CadenceKeywords.Turn,
                WeaponDamageMod = "10",
                WeaponSpeedMod = "1"
            };
            var hero = new Character("BuildModsHero", 1);
            hero.Effects.AddModifierBonusesFromAction(action, nextComboSlot: null, useEnemySpreadsheetMods: false, owner: hero);
            var pending = hero.Effects.PeekTurnBonuses();
            TestBase.AssertTrue(pending.Exists(b => b.Type == "WEAPON_DAMAGE" && Math.Abs(b.Value - 10) < 0.01),
                "TURN deposit WEAPON_DAMAGE 10", ref run, ref passed, ref failed);
            TestBase.AssertTrue(pending.Exists(b => b.Type == "WEAPON_SPEED" && Math.Abs(b.Value - 1) < 0.01),
                "TURN deposit WEAPON_SPEED 1", ref run, ref passed, ref failed);
        }
    }
}
