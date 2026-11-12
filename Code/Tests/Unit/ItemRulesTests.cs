using System;
using System.Linq;
using RPGGame;
using RPGGame.Combat.Events;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>The item rule language (CrossWires): conditions, EVERY, HEAL / REFLECT / BUFF, passive WHILE stats, RULE prefixes.</summary>
    public static class ItemRulesTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Item Rules Tests ===\n");
            _run = _passed = _failed = 0;
            MaterialBuildsLoader.Reload();

            TestParseGrammar();
            TestPassiveDamagePerStack();
            TestPassiveHpConditionAndTilt();
            TestPrefixCondition();
            TestEveryCounter();
            TestHealAndReflect();
            TestBuffAndSpentCondition();
            TestRolledValueText();
            TestDataRulesParse();

            TestBase.PrintSummary("Item Rules Tests", _run, _passed, _failed);
        }

        private static Character Hero(string name)
        {
            var hero = new Character(name, 10);
            hero.Equipment.Weapon = new WeaponItem("Blade", 1, 10, 1.0, WeaponType.Sword) { Material = "Bone" };
            hero.Equipment.Body = new ChestItem("Coat", 1, 5) { Material = "Bone" };
            return hero;
        }

        private static void Give(Character hero, string name, string rule, double rolled = 0)
        {
            hero.Equipment.Body!.Modifications.Add(new Modification
            {
                Name = name, Effect = CrossWires.RuleEffect, CrossWire = rule, MinValue = rolled, MaxValue = rolled, RolledValue = rolled
            });
        }

        private static void TestParseGrammar()
        {
            TestBase.SetCurrentTestName(nameof(TestParseGrammar));
            var r = CrossWireRule.Parse("ONCONNECT EVERY 3 IF TARGET bleed>=2 AND HP<50 -> APPLY expose 1");
            TestBase.AssertTrue(r != null && r.Every == 3 && r.Conditions.Count == 2 && r.Verb == "APPLY", "EVERY + two conditions", ref _run, ref _passed, ref _failed);
            var p = CrossWireRule.Parse("WHILE IF PREFIX undead -> DAMAGE +25%");
            TestBase.AssertTrue(p != null && p.IsPassive && p.Amount == 25 && p.Percent && p.Conditions[0].Subject == "prefix", "passive with prefix", ref _run, ref _passed, ref _failed);
            var b = CrossWireRule.Parse("ONKILL -> BUFF HIT 1 FOR dungeon");
            TestBase.AssertTrue(b != null && b.Verb == "BUFF" && b.Scope == "dungeon", "BUFF scope", ref _run, ref _passed, ref _failed);
            var per = CrossWireRule.Parse("WHILE -> DAMAGE +3% PER TARGET bleed");
            TestBase.AssertTrue(per != null && per.PerTargetStatus == "bleed", "PER TARGET", ref _run, ref _passed, ref _failed);
            TestBase.AssertNull(CrossWireRule.Parse("WHILE -> HEAL 5%"), "HEAL is not passive", ref _run, ref _passed, ref _failed);
            TestBase.AssertNull(CrossWireRule.Parse("ONCONNECT -> DAMAGE +5%"), "DAMAGE needs WHILE", ref _run, ref _passed, ref _failed);
            TestBase.AssertNull(CrossWireRule.Parse("ONTAKEHIT -> REFLECT 10"), "REFLECT needs a percent", ref _run, ref _passed, ref _failed);
            TestBase.AssertNull(CrossWireRule.Parse("ONKILL -> BUFF LUCK 1"), "unknown BUFF stat rejected", ref _run, ref _passed, ref _failed);
            var old = CrossWireRule.Parse("ONKILL IF CRISIS>=1 -> MINT TRIUMPH 2");
            TestBase.AssertTrue(old != null && old.IfStore == "CRISIS" && old.IfAtLeast == 1, "cross-wire form still parses", ref _run, ref _passed, ref _failed);
        }

        private static void TestPassiveDamagePerStack()
        {
            TestBase.SetCurrentTestName(nameof(TestPassiveDamagePerStack));
            var hero = Hero("ButcherHero");
            Give(hero, "Butcher's", "WHILE -> DAMAGE +{n}% PER TARGET bleed", 3);
            var foe = new Character("BleedFoe", 5);
            TestBase.AssertEqual(0.0, CrossWires.Passive(hero, "DAMAGE", foe), "no bleed, no bonus", ref _run, ref _passed, ref _failed);
            foe.BleedIntensity = 4;
            TestBase.AssertEqual(12.0, CrossWires.Passive(hero, "DAMAGE", foe), "4 bleed x 3% = 12%", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(12.0, AffixTemplates.OutgoingDamagePercent(hero, null, foe), "feeds outgoing damage", ref _run, ref _passed, ref _failed);
        }

        private static void TestPassiveHpConditionAndTilt()
        {
            TestBase.SetCurrentTestName(nameof(TestPassiveHpConditionAndTilt));
            var hero = Hero("BloodiedHero");
            Give(hero, "Bloodied", "WHILE IF HP<50 -> CRIT +2");
            TestBase.AssertEqual(0.0, CrossWires.Passive(hero, "CRIT"), "full HP: no crit", ref _run, ref _passed, ref _failed);
            hero.TakeDamage(hero.CurrentHealth - hero.GetEffectiveMaxHealth() / 4);
            TestBase.AssertEqual(2.0, CrossWires.Passive(hero, "CRIT"), "below 50%: +2 crit", ref _run, ref _passed, ref _failed);

            var tank = Hero("TowerHero");
            int armorBefore = tank.Health.GetEffectiveArmor();
            Give(tank, "Towering", "WHILE -> ARMOR +{n}%; WHILE -> SPEED -10%", 50);
            TestBase.AssertEqual(-10.0, CrossWires.Passive(tank, "SPEED"), "tilt: 10% slower", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(tank.Health.GetEffectiveArmor() > armorBefore, "tilt: more armor", ref _run, ref _passed, ref _failed);
        }

        private static void TestPrefixCondition()
        {
            TestBase.SetCurrentTestName(nameof(TestPrefixCondition));
            var hero = Hero("SilverHero");
            Give(hero, "Silvered", "WHILE IF PREFIX undead -> DAMAGE +25%");
            var foe = TestDataBuilders.Enemy().WithName("Wolf").Build();
            TestBase.AssertEqual(0.0, CrossWires.Passive(hero, "DAMAGE", foe), "plain enemy: no bonus", ref _run, ref _passed, ref _failed);
            foe.ApplyPrefix("Undead", new[] { "undead", "risen" });
            TestBase.AssertEqual(25.0, CrossWires.Passive(hero, "DAMAGE", foe), "undead: +25%", ref _run, ref _passed, ref _failed);
        }

        private static void TestEveryCounter()
        {
            TestBase.SetCurrentTestName(nameof(TestEveryCounter));
            var hero = Hero("PryHero");
            Give(hero, "Prying", "ONCONNECT EVERY 2 -> APPLY expose 1");
            var foe = new Character("PryFoe", 5);
            HeroEventHub.SetTarget(hero, foe);
            var hit = new CombatEvent(CombatEventType.ActionHit, hero) { Target = foe, Action = new Action { Name = "STAB", Type = ActionType.Attack } };
            MaterialSetController.TryMintFromEvent(hero, hit, hit.Action);
            TestBase.AssertEqual(0, foe.ExposeStacks ?? 0, "1st connect: nothing", ref _run, ref _passed, ref _failed);
            MaterialSetController.TryMintFromEvent(hero, hit, hit.Action);
            TestBase.AssertEqual(1, foe.ExposeStacks ?? 0, "2nd connect: Expose", ref _run, ref _passed, ref _failed);
            MaterialSetController.ResetFightConnects(hero);
            MaterialSetController.TryMintFromEvent(hero, hit, hit.Action);
            TestBase.AssertEqual(1, foe.ExposeStacks ?? 0, "counter restarts each fight", ref _run, ref _passed, ref _failed);
        }

        private static void TestHealAndReflect()
        {
            TestBase.SetCurrentTestName(nameof(TestHealAndReflect));
            var hero = Hero("FangHero");
            Give(hero, "Vampiric", "ONCONNECT -> HEAL {n}%", 10);
            hero.TakeDamage(50);
            int before = hero.CurrentHealth;
            var foe = new Character("FangFoe", 5);
            var hit = new CombatEvent(CombatEventType.ActionHit, hero) { Target = foe, Damage = 40, Action = new Action { Name = "STAB", Type = ActionType.Attack } };
            MaterialSetController.TryMintFromEvent(hero, hit, hit.Action);
            TestBase.AssertEqual(before + 4, hero.CurrentHealth, "heals 10% of 40 damage", ref _run, ref _passed, ref _failed);

            var thorn = Hero("ThornHero");
            Give(thorn, "Thorned", "ONTAKEHIT -> REFLECT 25%");
            var attacker = new Character("ThornFoe", 5);
            int foeHp = attacker.CurrentHealth;
            var taken = new CombatEvent(CombatEventType.ActionHit, attacker) { Target = thorn, Damage = 20, Action = new Action { Name = "BITE", Type = ActionType.Attack } };
            MaterialSetController.TryMintFromEvent(thorn, taken, taken.Action, takeHitOnly: true);
            TestBase.AssertTrue(attacker.CurrentHealth < foeHp, "reflect damages the attacker", ref _run, ref _passed, ref _failed);
        }

        private static void TestBuffAndSpentCondition()
        {
            TestBase.SetCurrentTestName(nameof(TestBuffAndSpentCondition));
            var hero = Hero("LedgerHero");
            Give(hero, "Ledgered", "ONCASH IF SPENT>=10 -> ADD focus 1");
            HeroEventHub.OnCash(hero, "RAGE", 6);
            TestBase.AssertEqual(0, Stores.Get(hero, "focus"), "spending 6: no Focus", ref _run, ref _passed, ref _failed);
            HeroEventHub.OnCash(hero, "RAGE", 12);
            TestBase.AssertEqual(1, Stores.Get(hero, "focus"), "spending 12: +1 Focus", ref _run, ref _passed, ref _failed);

            var jester = Hero("JesterHero");
            Give(jester, "Jester's", "ONKILL -> BUFF HIT 1 FOR dungeon");
            var foe = new Character("JesterFoe", 5);
            var kill = new CombatEvent(CombatEventType.EnemyDied, jester) { Target = foe, Action = new Action { Name = "STAB", Type = ActionType.Attack } };
            MaterialSetController.TryMintFromEvent(jester, kill, kill.Action);
            TestBase.AssertEqual(1.0, jester.DungeonCadenceBuffs.GetModifierPercent("HIT"), "kill: +1 HIT for the dungeon", ref _run, ref _passed, ref _failed);
        }

        private static void TestRolledValueText()
        {
            TestBase.SetCurrentTestName(nameof(TestRolledValueText));
            var mod = new Modification { Name = "Vampiric", Effect = CrossWires.RuleEffect, CrossWire = "ONCONNECT -> HEAL {n}%", RolledValue = 6.4 };
            string? text = CrossWires.DescribeMod(mod);
            TestBase.AssertTrue(text != null && text.Contains("6%"), $"card shows the rolled value ({text})", ref _run, ref _passed, ref _failed);
        }

        private static void TestDataRulesParse()
        {
            TestBase.SetCurrentTestName(nameof(TestDataRulesParse));
            var mods = LootDataCache.Load().Modifications.Where(m => AffixTemplates.Is(m, CrossWires.RuleEffect)).ToList();
            TestBase.AssertTrue(mods.Count >= 25, $"RULE prefixes load ({mods.Count})", ref _run, ref _passed, ref _failed);
            foreach (var m in mods)
            {
                int parts = m.CrossWire!.Split(';').Length;
                int parsed = CrossWires.RulesOf(m, m.Name).Count();
                TestBase.AssertEqual(parts, parsed, $"{m.Name}: every rule parses", ref _run, ref _passed, ref _failed);
            }
        }
    }
}
