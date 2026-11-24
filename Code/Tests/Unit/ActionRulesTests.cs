using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Actions.Conditional;
using RPGGame.Combat.Events;
using RPGGame.Combat.Rules;
using RPGGame.Data;

namespace RPGGame.Tests.Unit
{
    /// <summary>Action trigger bundles as action-owned rules (one rule engine, step 4) and the strip grammar.</summary>
    public static class ActionRulesTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== ActionRules Tests ===\n");
            _run = _passed = _failed = 0;

            TestEveryLiveBundleConverts();
            TestGrammar();
            TestBerserkerSwingBeforeDamage();
            TestRoomClearHealsEveryClear();
            TestFiresOnlyOnOwnEvent();
            TestRecordedWithActionOwner();
            TestCardHearsHubEventDuringItsSwing();
            TestHubCountAsRejected();
            TestMintIsAdd();

            TestBase.PrintSummary("ActionRules Tests", _run, _passed, _failed);
        }

        private static Action WithBundle(string name, string when, string mechanics, string count = "1", string scope = "",
            double? value = null, string? scaleFrom = null) => new()
        {
            Name = name,
            Type = ActionType.Attack,
            Advanced = new AdvancedMechanicsProperties(),
            Triggers = new ConditionalTriggerProperties
            {
                Bundles = new List<ActionTriggerBundle>
                {
                    new() { When = when, Count = count, Scope = scope, Mechanics = mechanics, Value = value, ScaleFrom = scaleFrom }
                }
            }
        };

        private static void TestEveryLiveBundleConverts()
        {
            var actions = ActionLoader.GetAllActions().ToList();
            foreach (string enemy in new[] { "ENEMY VENOM", "ENEMY SCORCH", "ENEMY REND", "ENEMY CONCUSS" })
                if (RPGGame.Data.EnemyActionCatalog.TryCreate(enemy) is { } a)
                    actions.Add(a);
            var withBundles = actions.Where(a => a.Triggers?.Bundles?.Any(b => b != null && b.IsEnabled) == true).ToList();
            TestBase.AssertTrue(withBundles.Count >= 25, $"live actions with trigger bundles found ({withBundles.Count})",
                ref _run, ref _passed, ref _failed);
            var missing = withBundles.SelectMany(a => ActionRules.Unsupported(a).Select(m => $"{a.Name}:{m}")).ToList();
            TestBase.AssertEqual("", string.Join(", ", missing), "every live bundle mechanic has a rule form", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(withBundles.All(a => ActionRules.For(a).Count > 0), "every bundle action owns rules",
                ref _run, ref _passed, ref _failed);
        }

        private static void TestGrammar()
        {
            CrossWireRule? P(string s) => CrossWireRule.Parse(s, "t");
            TestBase.AssertEqual("strip_jump", P("ONCONNECT -> STRIP jump 3")?.Store ?? "", "STRIP jump 3 parses", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("strip_replace_next:STAB", P("ONCONNECT -> STRIP replace_next:STAB")?.Store ?? "",
                "STRIP keeps its argument", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("retrigger_slot:2", P("ONCONNECT -> RETRIGGER slot:2")?.Store ?? "", "RETRIGGER slot:2 parses",
                ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(P("ONCONNECT -> STRIP sideways") == null, "an unknown strip op is rejected", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(P("ONCONNECT -> CRITFACE 19")?.Amount == 19, "CRITFACE 19 parses", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(P("ONCONNECT -> CRITFACE 1") == null, "CRITFACE below 2 is rejected", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(P("ONCRITICAL -> HEAL 5 HP") is { Flat: true, Amount: 5 }, "HEAL 5 HP is a flat heal",
                ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(P("ONCRITICAL -> HEAL 5") is { Flat: false }, "HEAL 5 stays % max HP", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(P("ONCONNECT -> SWING DAMAGE 1% PER SELF MISSINGHP") is { Verb: "SWING", Store: "DAMAGE" },
                "SWING DAMAGE n% PER SELF MISSINGHP parses", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(P("ONCONNECT -> SWING CRIT 5%") == null, "SWING takes DAMAGE / SPEED / AMP only", ref _run, ref _passed, ref _failed);

            // A worn piece's flat heal.
            var hero = TestDataBuilders.Character().WithName("FlatHealer").Build();
            hero.Equipment.Head = new HeadItem("HealHelm", 1, 1) { ImplicitRule = "ONSELFCOST -> HEAL 7 HP" };
            RuleBook.Invalidate(hero);
            hero.CurrentHealth = hero.GetEffectiveMaxHealth() - 20;
            int before = hero.CurrentHealth;
            HeroEventHub.Raise(hero, "ONSELFCOST");
            TestBase.AssertEqual(before + 7, hero.CurrentHealth, "an item's HEAL 7 HP heals 7", ref _run, ref _passed, ref _failed);
        }

        private static void TestBerserkerSwingBeforeDamage()
        {
            var action = WithBundle("BERSERK TEST", "ONCONNECT", "hero_action_damage", value: 1, scaleFrom: "MISSINGHP");
            var rule = ActionRules.For(action).Single();
            TestBase.AssertTrue(rule is { Verb: "SWING", Store: "DAMAGE" } && rule.Per?.Arg == "MISSINGHP",
                "hero_action_damage × MISSINGHP converts to SWING DAMAGE PER SELF MISSINGHP", ref _run, ref _passed, ref _failed);
            var hero = new Character("Berserk", 1);
            var foe = new Enemy("Goblin", 1, 100, 10, 5, 5, 5);
            hero.CurrentHealth = hero.MaxHealth / 2;
            int missing = ItemTriggerMagnitude.MissingHealthPercent(hero);
            var hit = new CombatEvent(CombatEventType.ActionHit, hero) { Target = foe, Action = action };
            TestBase.AssertTrue(!ActionRules.Dispatch(action, hit, hero, foe, new List<string>()),
                "the post-hit pass leaves SWING rules alone", ref _run, ref _passed, ref _failed);
            ActionRules.Dispatch(action, hit, hero, foe, new List<string>(), swingOnly: true);
            TestBase.AssertTrue(Math.Abs(hero.Effects.ConsumedDamageModPercent - missing) < 0.001,
                $"the pre-damage pass adds 1% per 1% HP missing ({hero.Effects.ConsumedDamageModPercent} vs {missing})",
                ref _run, ref _passed, ref _failed);
        }

        private static void TestRoomClearHealsEveryClear()
        {
            var action = WithBundle("CLEAR HEAL TEST", "ONROOMSCLEARED", "heal");
            TestBase.AssertEqual("ONROOMSCLEARED", ActionRules.For(action).Single().When,
                "a count of 1 means every clear (it used to mean the first clear only)", ref _run, ref _passed, ref _failed);
            var third = WithBundle("THIRD CLEAR TEST", "ONROOMSCLEARED", "heal", count: "3");
            TestBase.AssertEqual("ONROOMSCLEARED:3", ActionRules.For(third).Single().When, "a count above 1 is the Nth clear",
                ref _run, ref _passed, ref _failed);
            var natural = WithBundle("NAT TEST", "ONNATURALROLL", "heal", count: "7");
            TestBase.AssertEqual("ONNATURALROLL:7", ActionRules.For(natural).Single().When, "a roll-face count is the face",
                ref _run, ref _passed, ref _failed);
            var exact = WithBundle("EXACT TEST", "ONROLLVALUE", "heal", count: "15");
            TestBase.AssertEqual("ONROLLVALUE:15", ActionRules.For(exact).Single().When, "a roll-total count is the total",
                ref _run, ref _passed, ref _failed);
        }

        private static void TestFiresOnlyOnOwnEvent()
        {
            var action = WithBundle("MISS STOP TEST", "ONMISS", "strip_stop");
            var hero = new Character("Stopper", 1);
            var foe = new Enemy("Goblin", 1, 100, 10, 5, 5, 5);
            var hit = new CombatEvent(CombatEventType.ActionHit, hero) { Target = foe, Action = action };
            TestBase.AssertTrue(!ActionRules.Dispatch(action, hit, hero, foe, new List<string>()),
                "an ONMISS card rule skips a hit", ref _run, ref _passed, ref _failed);
        }

        /// <summary>A card's hub-event rule (ONCASH, ONSTATUSAPPLY, ...) fires when the event is raised during its own swing,
        /// and not on another swing.</summary>
        private static void TestCardHearsHubEventDuringItsSwing()
        {
            var card = WithBundle("CASH EAR TEST", "ONCASH", "replace_next_roll:11");
            var hero = new Character("Earful", 1);
            CombatTriggerContext.TryConsumePendingReplaceRollFace(hero, out _);
            HeroEventHub.Raise(hero, "ONCASH", "RAGE");
            TestBase.AssertFalse(CombatTriggerContext.TryConsumePendingReplaceRollFace(hero, out _),
                "outside the card's swing its ONCASH rule stays quiet", ref _run, ref _passed, ref _failed);
            using (ActionRules.BeginSwing(hero, card))
                HeroEventHub.Raise(hero, "ONCASH", "RAGE");
            bool fired = CombatTriggerContext.TryConsumePendingReplaceRollFace(hero, out int face);
            TestBase.AssertTrue(fired && face == 11, "a cash-in during the card's swing fires its ONCASH rule", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(ActionRules.Swinging(hero) == null, "the swing marker clears after the swing", ref _run, ref _passed, ref _failed);
        }

        private static void TestHubCountAsRejected()
        {
            TestBase.AssertNull(CrossWireRule.Parse("ONCASH -> COUNTAS ONTAKEHIT"), "COUNTAS from a hub event is rejected",
                ref _run, ref _passed, ref _failed);
            TestBase.AssertNull(CrossWireRule.Parse("ONMISS -> COUNTAS ONCASH"), "COUNTAS into a hub event is rejected",
                ref _run, ref _passed, ref _failed);
            TestBase.AssertNotNull(CrossWireRule.Parse("ONMISS -> COUNTAS ONTAKEHIT"), "combat-event aliases still parse",
                ref _run, ref _passed, ref _failed);
        }

        /// <summary>MINT is ADD; adding to a material store mints it (MINTBONUS applies on every path).</summary>
        private static void TestMintIsAdd()
        {
            var old = CrossWireRule.Parse("ONKILL -> MINT RAGE 2");
            TestBase.AssertTrue(old != null && old.Verb == "ADD" && old.Store == "RAGE", "MINT reads as ADD", ref _run, ref _passed, ref _failed);
            var hero = new Character("Minter", 1);
            int before = hero.Effects.GetMaterialKeyword("RAGE");
            int added = Stores.Add(hero, "RAGE", 2);
            TestBase.AssertEqual(before + added, hero.Effects.GetMaterialKeyword("RAGE"), "a store add lands in the material bank",
                ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(added >= 2, "and reports what landed (MINTBONUS included)", ref _run, ref _passed, ref _failed);
        }

        private static void TestRecordedWithActionOwner()
        {
            var action = WithBundle("TRACE TEST", "ONCONNECT", "replace_next_roll:9");
            var hero = new Character("Tracer", 1);
            var foe = new Enemy("Goblin", 1, 100, 10, 5, 5, 5);
            RuleEngine.ClearTrace();
            var hit = new CombatEvent(CombatEventType.ActionHit, hero) { Target = foe, Action = action };
            ActionRules.Dispatch(action, hit, hero, foe, new List<string>());
            TestBase.AssertTrue(RuleEngine.Trace.Any(t => t.Owner == "TRACE TEST"), "the fire is recorded with the card as owner",
                ref _run, ref _passed, ref _failed);
            CombatTriggerContext.TryConsumePendingReplaceRollFace(hero, out _);
        }
    }
}
