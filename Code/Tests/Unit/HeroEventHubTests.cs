using System;
using System.Linq;
using RPGGame;
using RPGGame.Combat.Events;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>Hub events (ONBEAT, ONCASH, ONSTATUSAPPLY, ONSTOREFULL, ONSELFCOST, ONENEMYACT), Listener affixes and action costs.</summary>
    public static class HeroEventHubTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Hero Event Hub Tests ===\n");
            _run = _passed = _failed = 0;
            MaterialBuildsLoader.Reload();

            TestMatches();
            TestBeatClock();
            TestHubRaisesToSubscribers();
            TestListenerSlotFamilies();
            TestListenerMintsOnHubEvent();
            TestListenerNeedsFormedSet();
            TestListenerOnCombatEvent();
            TestStoreFullOnMintThreshold();
            TestActionCostPaysHp();
            TestListenerRowInData();

            TestBase.PrintSummary("Hero Event Hub Tests", _run, _passed, _failed);
        }

        private static Character Hero(string name, string weapon, params string[] armor)
        {
            var hero = new Character(name, 10);
            hero.Equipment.Weapon = new WeaponItem("Blade", 1, 10, 1.0, WeaponType.Sword) { Material = weapon };
            if (armor.Length > 0) hero.Equipment.Head = new HeadItem("Cap", 1, 1) { Material = armor[0] };
            if (armor.Length > 1) hero.Equipment.Body = new ChestItem("Coat", 1, 1) { Material = armor[1] };
            if (armor.Length > 2) hero.Equipment.Legs = new LegsItem("Pants", 1, 1) { Material = armor[2] };
            if (armor.Length > 3) hero.Equipment.Feet = new FeetItem("Boots", 1, 1) { Material = armor[3] };
            return hero;
        }

        private static Modification Listener(string when) =>
            new() { Name = "Listening", Effect = ListenerAffix.EffectName, TriggerWhen = when, RolledValue = 1 };

        private static void TestMatches()
        {
            TestBase.SetCurrentTestName(nameof(TestMatches));
            TestBase.AssertTrue(HeroEventHub.Matches("ONBEAT", "ONBEAT", "4"), "ONBEAT default period 4 fires on turn 4", ref _run, ref _passed, ref _failed);
            TestBase.AssertFalse(HeroEventHub.Matches("ONBEAT", "ONBEAT", "3"), "not on turn 3", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(HeroEventHub.Matches("ONBEAT:3", "ONBEAT", "6"), "ONBEAT:3 on turn 6", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(HeroEventHub.Matches("ON_STATUS_APPLY:bleed", "ONSTATUSAPPLY", "bleed"), "status arg matches", ref _run, ref _passed, ref _failed);
            TestBase.AssertFalse(HeroEventHub.Matches("ONSTATUSAPPLY:bleed", "ONSTATUSAPPLY", "burn"), "other status does not", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(HeroEventHub.Matches("ONCASH", "ONCASH", "RAGE"), "no arg matches any store", ref _run, ref _passed, ref _failed);
            TestBase.AssertFalse(HeroEventHub.Matches("ONCRITICAL", "ONCASH", "RAGE"), "different event", ref _run, ref _passed, ref _failed);
        }

        private static void TestBeatClock()
        {
            TestBase.SetCurrentTestName(nameof(TestBeatClock));
            var hero = Hero("BeatHero", "Bone");
            HeroEventHub.ResetFight(hero);
            for (int i = 0; i < 5; i++)
                HeroEventHub.OnHeroTurnStart(hero);
            TestBase.AssertEqual(5, HeroEventHub.FightTurns(hero), "five turns counted", ref _run, ref _passed, ref _failed);
            MaterialSetController.ResetFightConnects(hero);
            TestBase.AssertEqual(0, HeroEventHub.FightTurns(hero), "new fight resets the beat clock", ref _run, ref _passed, ref _failed);
        }

        private static void TestHubRaisesToSubscribers()
        {
            TestBase.SetCurrentTestName(nameof(TestHubRaisesToSubscribers));
            var hero = Hero("SubHero", "Bone");
            var foe = TestDataBuilders.Enemy().WithName("SubFoe").Build();
            string seen = "";
            Action<Character, string, string?> handler = (h, token, arg) => { if (ReferenceEquals(h, hero)) seen += token + ":" + arg + ";"; };
            HeroEventHub.Raised += handler;
            try
            {
                HeroEventHub.OnStatusesApplied(hero, foe, new[] { "bleed" });
                HeroEventHub.OnEnemyAct(foe, hero);
                HeroEventHub.OnCash(hero, "RAGE", 3);
                HeroEventHub.OnCash(hero, "RAGE", 0);
                HeroEventHub.OnSelfCost(hero, "HP");
            }
            finally
            {
                HeroEventHub.Raised -= handler;
            }
            TestBase.AssertEqual("ONSTATUSAPPLY:bleed;ONENEMYACT:;ONCASH:RAGE;ONSELFCOST:HP;", seen,
                "status, enemy act, cash (not a zero spend), self cost", ref _run, ref _passed, ref _failed);
        }

        private static void TestListenerSlotFamilies()
        {
            TestBase.SetCurrentTestName(nameof(TestListenerSlotFamilies));
            var weapon = new WeaponItem("W", 1, 1, 1.0, WeaponType.Sword);
            var head = new HeadItem("H", 1, 1);
            var feet = new FeetItem("F", 1, 1);
            TestBase.AssertTrue(ListenerAffix.EventsForSlot(weapon).Contains("ONKILL"), "weapon listens to hit events", ref _run, ref _passed, ref _failed);
            TestBase.AssertFalse(ListenerAffix.EventsForSlot(weapon).Contains("ONCRITICAL"), "weapon does not roll dice events", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(ListenerAffix.EventsForSlot(head).Contains("ONMISS"), "head listens to dice events", ref _run, ref _passed, ref _failed);
            TestBase.AssertFalse(ListenerAffix.EventsForSlot(head).Contains("ONNATURALROLL"), "no arg-only events", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(ListenerAffix.EventsForSlot(feet).Contains("ONBEAT"), "feet listen to time (incl. hub ONBEAT)", ref _run, ref _passed, ref _failed);
            var mod = Listener("");
            TestBase.AssertTrue(ListenerAffix.RollEvent(mod, head, new Random(3)) && ListenerAffix.EventsForSlot(head).Contains(mod.TriggerWhen),
                "rolled event comes from the slot family", ref _run, ref _passed, ref _failed);
        }

        private static void TestListenerMintsOnHubEvent()
        {
            TestBase.SetCurrentTestName(nameof(TestListenerMintsOnHubEvent));
            var hero = Hero("ListenHero", "Bone", "Bone", "Bone", "Bone", "Bone");
            hero.Equipment.Feet!.Modifications.Add(Listener("ONBEAT"));
            int perTrigger = MaterialSetController.ComputeMintPerTrigger(hero, MaterialBuildsLoader.FindByMaterial("Bone")!);
            int expected = Math.Max(1, (int)Math.Round(perTrigger * GameConfiguration.Instance.Combat.ListenerMintShare));
            HeroEventHub.Raise(hero, "ONBEAT", "4");
            TestBase.AssertEqual(expected, hero.Effects.GetMaterialKeyword("RAGE"), "Bone boots listening ON BEAT mint RAGE on turn 4", ref _run, ref _passed, ref _failed);
            HeroEventHub.Raise(hero, "ONBEAT", "5");
            TestBase.AssertEqual(expected, hero.Effects.GetMaterialKeyword("RAGE"), "but not on turn 5", ref _run, ref _passed, ref _failed);
        }

        private static void TestListenerNeedsFormedSet()
        {
            TestBase.SetCurrentTestName(nameof(TestListenerNeedsFormedSet));
            var hero = Hero("LoneHero", "Glass", "Bone");
            hero.Equipment.Head!.Modifications.Add(Listener("ONCASH"));
            HeroEventHub.Raise(hero, "ONCASH", "GRAZE");
            TestBase.AssertEqual(0, hero.Effects.GetMaterialKeyword("RAGE"), "one Bone piece: no set, no mint", ref _run, ref _passed, ref _failed);
        }

        private static void TestListenerOnCombatEvent()
        {
            TestBase.SetCurrentTestName(nameof(TestListenerOnCombatEvent));
            var hero = Hero("CritListenHero", "Glass", "Glass", "Glass");
            hero.Equipment.Head!.Modifications.Add(Listener("ONMISS"));
            var foe = TestDataBuilders.Enemy().WithName("MissFoe").Build();
            var miss = new CombatEvent(CombatEventType.ActionMiss, hero) { Target = foe, IsMiss = true, Action = new Action { Name = "STAB", Type = ActionType.Attack } };
            int before = hero.Effects.GetMaterialKeyword("GRAZE");
            MaterialSetController.TryMintFromEvent(hero, miss, miss.Action);
            TestBase.AssertTrue(hero.Effects.GetMaterialKeyword("GRAZE") > before, "Glass helm listening ON MISS mints GRAZE on a miss", ref _run, ref _passed, ref _failed);
        }

        private static void TestStoreFullOnMintThreshold()
        {
            TestBase.SetCurrentTestName(nameof(TestStoreFullOnMintThreshold));
            var hero = Hero("FullHero", "Bone");
            int full = GameConfiguration.Instance.Combat.MintStoreFullThreshold;
            string seen = "";
            Action<Character, string, string?> handler = (h, token, arg) => { if (ReferenceEquals(h, hero) && token == "ONSTOREFULL") seen += arg + ";"; };
            HeroEventHub.Raised += handler;
            try
            {
                MaterialSetController.MintKeyword(hero, "RAGE", full - 1, "test");
                MaterialSetController.MintKeyword(hero, "RAGE", 1, "test");
                MaterialSetController.MintKeyword(hero, "RAGE", 1, "test");
            }
            finally
            {
                HeroEventHub.Raised -= handler;
            }
            TestBase.AssertEqual(full > 0 ? "RAGE;" : "", seen, "ONSTOREFULL once, when the bank crosses the threshold", ref _run, ref _passed, ref _failed);
        }

        private static void TestActionCostPaysHp()
        {
            TestBase.SetCurrentTestName(nameof(TestActionCostPaysHp));
            var hero = Hero("CostHero", "Bone");
            hero.CurrentHealth = hero.GetEffectiveMaxHealth();
            int max = hero.GetEffectiveMaxHealth();
            var oath = new Action { Name = "BLOOD OATH", Cost = "HP 5" };
            int paid = ActionCost.Pay(hero, oath);
            TestBase.AssertEqual((int)Math.Round(max * 0.05), paid, "pays 5% max HP", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(max - paid, hero.CurrentHealth, "HP drops by the cost", ref _run, ref _passed, ref _failed);
            hero.CurrentHealth = 1;
            TestBase.AssertEqual(0, ActionCost.Pay(hero, oath), "never pays the last HP", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0, ActionCost.Pay(hero, new Action { Name = "FREE" }), "no cost, nothing paid", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("HP 5", ActionLoader.GetActionData("BLOOD OATH")?.Cost ?? "", "BLOOD OATH loads its cost", ref _run, ref _passed, ref _failed);
        }

        private static void TestListenerRowInData()
        {
            TestBase.SetCurrentTestName(nameof(TestListenerRowInData));
            var cache = LootDataCache.Load();
            TestBase.AssertTrue(cache.Modifications.Any(ListenerAffix.IsListener), "Modifications.json has a Listening prefix", ref _run, ref _passed, ref _failed);
        }
    }
}
