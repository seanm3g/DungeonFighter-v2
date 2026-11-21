using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Actions.Conditional;
using RPGGame.Combat.Events;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// The class-tree nodes that ran custom router code until 2026-11-18 and are rules now: Markets (DROP), Cadence
    /// (COMBO PER TAGS), Synthesis (MINTBONUS), Level 2 - Barbarian (ARMOR PER LASTROLL), Snake Eyes (Loaded + NEXTROLL),
    /// Second Wind (HEAL, ADD PER focus, SPEND), Second Chance (salvage) and Dagger Mastery (PER EXTRAHITS); plus the
    /// nodes that were cut.
    /// </summary>
    public static class CustomNodeRulesTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== CustomNodeRules Tests ===\n");
            _run = _passed = _failed = 0;
            var trees = SkillTreesConfig.TryLoadFromGameDataFile();
            if (trees != null)
                GameConfiguration.Instance.SkillTrees = trees;

            TestEveryConvertedNodeParses();
            TestCutNodesAreGone();
            TestNewGrammarParses();
            TestMarketDrop();
            TestCadenceCombo();
            TestIronHideArmor();
            TestSnakeEyes();
            TestSecondWind();
            TestSecondChance();
            TestComboEndReadsStepReached();
            TestDaggerMastery();

            TestBase.PrintSummary("CustomNodeRules Tests", _run, _passed, _failed);
        }

        private static readonly string[] Converted =
        {
            "b-loot", "w-loot", "r-loot", "z-loot", "b-combo", "w-combo", "r-combo", "z-combo",
            "b-synth", "w-synth", "r-synth", "z-synth", "b-lvl2", "r-snake", "w-wind", "r-critmiss", "r-mastery"
        };

        private static Character Hero(string name, WeaponType weapon, params string[] nodes)
        {
            var hero = TestDataBuilders.Character().WithName(name).Build();
            hero.Progression.RoguePoints = hero.Progression.BarbarianPoints = hero.Progression.WarriorPoints = 50;
            hero.Equipment.Weapon = TestDataBuilders.Weapon().WithName(name + "Weapon").WithWeaponType(weapon).Build();
            foreach (string id in nodes)
                hero.Progression.LearnedSkillRanks[id] = 1;
            CrossWires.ResetFight(hero);
            return hero;
        }

        private static CombatEvent Swing(Character hero, Actor foe, CombatEventType type, int roll, Action? action = null) =>
            new(type, hero)
            {
                Target = foe,
                Action = action ?? new Action { Name = "TEST_SWING", Type = ActionType.Attack },
                NaturalRollValue = roll,
                IsMiss = type == CombatEventType.ActionMiss
            };

        private static void Fire(Character hero, CombatEvent evt) => MaterialSetController.TryMintFromEvent(hero, evt, evt.Action);

        private static void TestEveryConvertedNodeParses()
        {
            foreach (string id in Converted)
            {
                var node = GameConfiguration.Instance.SkillTrees.GetNode(id);
                var parts = (node?.CrossWire ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries);
                bool ok = node != null && node.CustomEffectId == "rule" && parts.Length > 0
                    && parts.All(p => CrossWireRule.Parse(p.Trim(), node.Name) != null);
                TestBase.AssertTrue(ok, $"{id} is a rule node whose rules all parse", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(node == null || !RPGGame.UI.RuleText.HasTemplateBraces(RPGGame.UI.RuleText.SkillNode(node)),
                    $"{id} card text has no template braces", ref _run, ref _passed, ref _failed);
            }
        }

        private static void TestCutNodesAreGone()
        {
            foreach (string id in new[] { "z-archmage", "w-flow", "r-debt", "z-memory", "z-equation", "b-alloy" })
                TestBase.AssertTrue(GameConfiguration.Instance.SkillTrees.GetNode(id) == null, $"{id} is cut",
                    ref _run, ref _passed, ref _failed);
        }

        private static void TestNewGrammarParses()
        {
            TestBase.AssertTrue(CrossWireRule.Parse("ONKILL -> SPEND RAGE all")?.Amount >= CrossWireRule.SpendAllAmount,
                "SPEND store all parses", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(CrossWireRule.Parse("ONKILL -> SPEND RAGE 3")?.Amount == 3, "SPEND store n parses",
                ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(CrossWireRule.Parse("ONKILL -> NEXTROLL 21") == null, "NEXTROLL rejects faces past 20",
                ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(CrossWireRule.Parse("WHILE -> SPEND RAGE 1") == null, "SPEND is triggered only",
                ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(CrossWireRule.Parse("WHILE -> DROP iron 10%") is { Verb: "DROP", Store: "iron" },
                "DROP names a material too", ref _run, ref _passed, ref _failed);
        }

        private static void TestMarketDrop()
        {
            var hero = Hero("DropHero", WeaponType.Mace, "b-loot");
            TestBase.AssertTrue(Math.Abs(CrossWires.DropChances(hero).GetValueOrDefault("barbarian") - 0.5) < 1e-9,
                "Blood Market: 50% of armor drops are Barbarian materials", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0, CrossWires.DropChances(Hero("NoDrop", WeaponType.Mace)).Count, "no DROP rule, no bias",
                ref _run, ref _passed, ref _failed);
        }

        private static void TestCadenceCombo()
        {
            var hero = Hero("CadenceHero", WeaponType.Dagger);
            for (int i = 0; i < 4; i++)
            {
                var a = TestDataBuilders.CreateMockAction("ROGUE_" + i);
                a.Tags = new List<string> { "rogue" };
                hero.AddAction(a, 1.0);
                hero.Actions.AddToCombo(a);
            }
            double before = CrossWires.Passive(hero, "COMBO");
            hero.Progression.LearnedSkillRanks["r-combo"] = 1;
            int tags = ClassTags.Count(hero, ClassTags.Rogue);
            TestBase.AssertEqual(tags / 2, (int)Math.Round(CrossWires.Passive(hero, "COMBO") - before),
                "Soft Cadence: +1 Combo per 2 Rogue tags", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(tags / 2, ClassCadence.RuleSteps(hero), "the threshold tooltip reads the same steps",
                ref _run, ref _passed, ref _failed);
        }

        private static void TestIronHideArmor()
        {
            var hero = Hero("HideHero", WeaponType.Mace, "b-lvl2");
            var foe = new Character("HideFoe", 5);
            int before = hero.GetMaxArmor();
            Fire(hero, Swing(hero, foe, CombatEventType.ActionHit, 14));
            TestBase.AssertEqual(before + 14, hero.GetMaxArmor(), "Level 2 - Barbarian: armor = last natural roll",
                ref _run, ref _passed, ref _failed);
            Fire(hero, Swing(hero, foe, CombatEventType.ActionMiss, 3));
            TestBase.AssertEqual(before + 3, hero.GetMaxArmor(), "the next roll replaces it",
                ref _run, ref _passed, ref _failed);
        }

        private static void TestSnakeEyes()
        {
            var hero = Hero("SnakeHero", WeaponType.Dagger, "r-snake");
            var foe = new Character("SnakeFoe", 5);
            Fire(hero, Swing(hero, foe, CombatEventType.ActionMiss, 2));
            TestBase.AssertEqual(1, Stores.Get(hero, "loaded"), "a natural 2 adds Loaded", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!CombatTriggerContext.TryConsumePendingReplaceRollFace(hero, out _), "one Loaded: no forced roll yet",
                ref _run, ref _passed, ref _failed);
            Fire(hero, Swing(hero, foe, CombatEventType.ActionHit, 12));
            TestBase.AssertTrue(CombatTriggerContext.TryConsumePendingReplaceRollFace(hero, out int face) && face == 20,
                "at 2 Loaded the next roll is a 20", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0, Stores.Get(hero, "loaded"), "Loaded is spent", ref _run, ref _passed, ref _failed);
        }

        private static void TestSecondWind()
        {
            var hero = Hero("WindHero", WeaponType.Sword, "w-wind");
            var foe = new Character("WindFoe", 5);
            hero.CurrentHealth = hero.GetEffectiveMaxHealth() / 2;
            int hp = hero.CurrentHealth;
            Stores.Add(hero, "focus", 3);
            int fortify = Stores.Get(hero, "fortify");
            Fire(hero, new CombatEvent(CombatEventType.ComboEnded, hero) { Target = foe, Action = new Action { Name = "END", Type = ActionType.Attack } });
            TestBase.AssertTrue(hero.CurrentHealth > hp, "Second Wind heals when the combo ends", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(fortify + 3, Stores.Get(hero, "fortify"), "Focus becomes Fortify", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0, Stores.Get(hero, "focus"), "Focus is spent", ref _run, ref _passed, ref _failed);
        }

        private static void TestComboEndReadsStepReached()
        {
            // ComboEnded fires after the step resets to 0; STEP must read the step the chain reached (Empty Fury).
            var hero = Hero("EndHero", WeaponType.Mace);
            var foe = new Character("EndFoe", 5);
            var rule = CrossWireRule.Parse("ONCOMBOEND IF STEP>=3 -> MINT RAGE 1", "test")!;
            hero.ComboStep = 0;
            var ended = new CombatEvent(CombatEventType.ComboEnded, hero) { Target = foe, ComboStepReached = 4 };
            TestBase.AssertTrue(CrossWires.ConditionsHold(hero, foe, rule, 0, ended), "a chain that reached step 4 passes STEP>=3",
                ref _run, ref _passed, ref _failed);
            var shortEnd = new CombatEvent(CombatEventType.ComboEnded, hero) { Target = foe, ComboStepReached = 2 };
            TestBase.AssertTrue(!CrossWires.ConditionsHold(hero, foe, rule, 0, shortEnd), "a chain that reached step 2 fails STEP>=3",
                ref _run, ref _passed, ref _failed);
        }

        private static void TestSecondChance()
        {
            var hero = Hero("ChanceHero", WeaponType.Dagger, "r-critmiss");
            var foe = new Character("ChanceFoe", 5);
            int before = Stores.Get(hero, "salvage");
            var miss = Swing(hero, foe, CombatEventType.ActionMiss, 1);
            miss.IsCriticalMiss = true;
            Fire(hero, miss);
            TestBase.AssertEqual(before + 1, Stores.Get(hero, "salvage"), "a Dagger crit miss banks one miss salvage",
                ref _run, ref _passed, ref _failed);
            Stores.SpendAll(hero, "salvage");
        }

        private static void TestDaggerMastery()
        {
            var hero = Hero("MasteryHero", WeaponType.Dagger, "r-mastery");
            var foe = new Character("MasteryFoe", 5);
            var flurry = new Action { Name = "TEST_FLURRY", Type = ActionType.Attack };
            flurry.Advanced.MultiHitCount = 3;
            TestBase.AssertEqual(2, CrossWires.ExtraHits(hero, flurry), "a 3-hit action has 2 extra hits", ref _run, ref _passed, ref _failed);
            int bleed = 0;
            for (int i = 0; i < 20; i++)
            {
                foe.BleedIntensity = 0;
                Fire(hero, Swing(hero, foe, CombatEventType.ActionHit, 10, flurry));
                bleed += foe.BleedIntensity;
            }
            TestBase.AssertTrue(bleed > 0, "Dagger Mastery: multi-hit Dagger actions add Bleed", ref _run, ref _passed, ref _failed);
            foe.BleedIntensity = 0;
            Fire(hero, Swing(hero, foe, CombatEventType.ActionHit, 10));
            TestBase.AssertEqual(0, foe.BleedIntensity, "single-hit actions add none", ref _run, ref _passed, ref _failed);
        }
    }
}
