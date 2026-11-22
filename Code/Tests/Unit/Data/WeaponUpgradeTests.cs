using System;
using System.Linq;
using RPGGame.LootReveal;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    /// <summary>Weapons keep their class starter and are judged on hits per second (MCP bot check, 2026-11-21).</summary>
    public static class WeaponUpgradeTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== WeaponUpgrade Tests ===\n");
            _run = _passed = _failed = 0;

            var plainMace = new WeaponItem("Test Pounder", 1, 10, 1.3, WeaponType.Mace) { Rarity = "Uncommon" };
            var names = GearActionNames.Resolve(plainMace);
            TestBase.AssertTrue(names.Contains("SLAM") && names.Contains("WIND UP"),
                $"any mace carries the Barbarian starters ({string.Join(", ", names)})", ref _run, ref _passed, ref _failed);
            var dagger = new WeaponItem("Test Shiv", 1, 6, 0.8, WeaponType.Dagger);
            var daggerNames = GearActionNames.Resolve(dagger);
            TestBase.AssertTrue(daggerNames.Contains("STAB") && daggerNames.Contains("CUT") && !daggerNames.Contains("WIND UP"),
                $"a dagger carries the Rogue starters, not the Barbarian ones ({string.Join(", ", daggerNames)})", ref _run, ref _passed, ref _failed);

            var hero = TestDataBuilders.CreateTestCharacter("Smasher", 5);
            var starter = new WeaponItem("Test Log", 1, 6, 1.35, WeaponType.Mace);
            hero.TryEquipItem(starter, "weapon", out _, out _, ignoreAttributeRequirements: true);
            var better = new WeaponItem("Test Maul", 2, 20, 1.35, WeaponType.Mace);
            var entry = LootRevealBuilder.Build(hero, better);
            TestBase.AssertTrue(entry.Verdict == LootVerdict.Upgrade && entry.Suggested == LootChoice.Equip,
                $"a much stronger mace is an upgrade ({entry.Verdict}: {entry.VerdictLine})", ref _run, ref _passed, ref _failed);
            var worse = new WeaponItem("Test Twig", 1, 2, 1.35, WeaponType.Mace);
            var worseEntry = LootRevealBuilder.Build(hero, worse);
            TestBase.AssertTrue(worseEntry.Verdict != LootVerdict.Upgrade, $"a weaker mace is not ({worseEntry.Verdict})", ref _run, ref _passed, ref _failed);

            TestBase.PrintSummary("WeaponUpgrade Tests", _run, _passed, _failed);
        }
    }
}
