using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    /// <summary>
    /// Step 11 action cut (Documentation/ACTION_CUT_2026-11-22.md): retired cards still load but never roll; the kept
    /// pool fits the D5.2 budget; enemy kits are 11 templates and the merged names still resolve.
    /// </summary>
    public static class ActionCutTests
    {
        private static int _run, _passed, _failed;

        private static readonly string[] RetiredStatusIds = { "poison", "weaken", "slow", "stun", "brittle", "acid", "fortify", "confuse" };

        public static void RunAllTests()
        {
            Console.WriteLine("=== ActionCut Tests ===\n");
            _run = _passed = _failed = 0;

            var all = ActionLoader.GetAllActionData();
            var rollable = all.Where(a => GearActionPools.CanRollOnGear(a) && GameDataTagHelper.IsRollableOnHeroGear(a.Tags)).ToList();

            // Retired rows still load (saves, skill nodes, tests) but leave every roll.
            var venom = ActionLoader.GetAction("TAUNT");
            TestBase.AssertNotNull(venom, "a retired card (TAUNT) still loads", ref _run, ref _passed, ref _failed);
            var tauntData = ActionLoader.GetActionData("TAUNT");
            TestBase.AssertTrue(tauntData != null && GameDataTagHelper.IsRetired(tauntData.Tags) && !GearActionPools.CanRollOnGear(tauntData),
                "TAUNT is retired and never rolls on gear", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(GameDataTagHelper.IsGrantableOnHeroGearByName("TAUNT"),
                "gear that already grants a retired card keeps it", ref _run, ref _passed, ref _failed);
            TestBase.AssertFalse(GameDataTagHelper.IsRollableOnHeroGearByName("TAUNT"), "retired by name is not rollable", ref _run, ref _passed, ref _failed);
            var cache = LootDataCache.Load();
            TestBase.AssertFalse(cache.ActionBonuses.Any(b => GameDataTagHelper.IsRetired(ActionLoader.GetActionData(b.Name)?.Tags)),
                "the loot action pool holds no retired card", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(RPGGame.Data.ItemGeneration.BaseModifierRoller.ActionsFor(null).All(a => !GameDataTagHelper.IsRetired(a.Tags)),
                "base modifiers pick only kept cards", ref _run, ref _passed, ref _failed);

            // Budget (D5.2): 10 rollable cards per class (plus 2 starters), 2 per hybrid, about 10 neutral.
            foreach (string cls in new[] { "barbarian", "warrior", "rogue", "wizard" })
            {
                int n = rollable.Count(a => Has(a, "class") && Has(a, cls) && !Has(a, "hybrid"));
                TestBase.AssertEqual(10, n, $"{cls} keeps 10 rollable cards", ref _run, ref _passed, ref _failed);
            }
            foreach (string hybrid in new[] { "ravager", "warbrute", "duelist", "spellblade", "hexblade", "ragecaller" })
                TestBase.AssertEqual(2, rollable.Count(a => Has(a, "hybrid") && Has(a, hybrid)), $"{hybrid} keeps 2 rollable cards", ref _run, ref _passed, ref _failed);
            int neutral = rollable.Count(a => GearActionPools.IsNeutral(a.Tags) && !Has(a, "enemy"));
            TestBase.AssertTrue(neutral >= 8 && neutral <= 12, $"about 10 neutral cards ({neutral})", ref _run, ref _passed, ref _failed);

            // Kept cards: an authored role, no retired status in the verb, the step 10 cards stay.
            var badVerb = rollable.Where(a => RetiredStatusIds.Any(s => (a.Verb ?? "").Split(' ').Any(w => w.Equals(s, StringComparison.OrdinalIgnoreCase)))).Select(a => a.Name).ToList();
            TestBase.AssertTrue(badVerb.Count == 0, $"no kept card applies a retired status ({string.Join(", ", badVerb)})", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(rollable.All(a => ActionRoles.IsRole(a.Role)), "every kept card has a role", ref _run, ref _passed, ref _failed);
            foreach (string name in new[] { "JAB", "BLUDGEON", "SHOULDER CHECK", "SHATTERING BLOW", "LAST OATH", "SNAPPING EDGE", "GLASS NOVA" })
                TestBase.AssertTrue(rollable.Any(a => a.Name == name), $"{name} is kept", ref _run, ref _passed, ref _failed);
            var retort = ActionLoader.GetAction("SWORN RETORT");
            TestBase.AssertTrue(retort != null && VerbPresets.GetSpec(retort) is { Kind: VerbKind.Cash, Store: "harden" },
                "SWORN RETORT cashes the Warrior's Guard", ref _run, ref _passed, ref _failed);

            // Enemy kits: 11 templates; merged template names still resolve to their new kit.
            EnemyActionCatalog.Reload();
            TestBase.AssertEqual(11, EnemyActionCatalog.TemplateNames.Count, "11 enemy kit templates", ref _run, ref _passed, ref _failed);
            foreach (var (old, kept) in new[] { ("ENEMY VENOM", "ENEMY REND"), ("ENEMY CURSE", "ENEMY SUNDER"), ("ENEMY PIERCE", "ENEMY SUNDER"),
                         ("ENEMY CONCUSS", "ENEMY ENSNARE"), ("ENEMY MEND", "ENEMY DRAIN") })
                TestBase.AssertEqual(kept, EnemyActionCatalog.ResolveTemplateName(old), $"{old} loads as {kept}", ref _run, ref _passed, ref _failed);
            var orphans = EnemyActionCatalog.Aliases.Where(kv => EnemyActionCatalog.ResolveTemplateName(kv.Key) == null).Select(kv => kv.Key).ToList();
            TestBase.AssertTrue(orphans.Count == 0, $"every kit alias resolves ({string.Join(", ", orphans.Take(5))})", ref _run, ref _passed, ref _failed);

            TestBase.PrintSummary("ActionCut Tests", _run, _passed, _failed);
        }

        private static bool Has(ActionData a, string tag) => GameDataTagHelper.HasTag(a.Tags, tag);
    }
}
