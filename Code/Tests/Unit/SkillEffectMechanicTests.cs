using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Actions.Conditional;
using RPGGame.Combat;
using RPGGame.Combat.Events;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Verifies each live SkillTrees.json customEffectId maps to working mechanics
    /// (effect text is the acceptance contract; runtime key is customEffectId).
    /// </summary>
    public static class SkillEffectMechanicTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== SkillEffectMechanic Tests ===\n");
            _testsRun = _testsPassed = _testsFailed = 0;

            EnsureTreesLoaded();
            TestAliasRoots();
            TestAgesAndPrefers();
            TestEmptyFuryAndStreaks();
            TestLootMarkets();
            TestRites();
            TestRollBanks();
            TestCadence();
            TestCombatSpecials();
            TestActionUnlocks();

            TestBase.PrintSummary("SkillEffectMechanic Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void EnsureTreesLoaded()
        {
            var trees = SkillTreesConfig.TryLoadFromGameDataFile();
            TestBase.AssertTrue(trees != null && trees.Trees.Count == 4,
                "SkillTrees.json loads four trees",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            if (trees != null)
                GameConfiguration.Instance.SkillTrees = trees;
        }

        private static Character MakeHero(string name, WeaponType weaponType, bool grantRoots = true)
        {
            var character = TestDataBuilders.Character().WithName(name).Build();
            if (grantRoots)
            {
                character.Progression.BarbarianPoints = 50;
                character.Progression.WarriorPoints = 50;
                character.Progression.RoguePoints = 50;
                character.Progression.WizardPoints = 50;
                character.Progression.EnsureSkillTreeRootsGranted();
            }

            var weapon = TestDataBuilders.Weapon()
                .WithName(name + "Weapon")
                .WithWeaponType(weaponType)
                .Build();
            switch (weaponType)
            {
                case WeaponType.Mace:
                    weapon.Material = "Bone";
                    weapon.Tags.Add("bone");
                    break;
                case WeaponType.Sword:
                    weapon.Material = "Bronze";
                    weapon.Tags.Add("bronze");
                    break;
                case WeaponType.Dagger:
                    weapon.Material = "Glass";
                    weapon.Tags.Add("glass");
                    break;
                case WeaponType.Wand:
                    weapon.Material = "Willow";
                    weapon.Tags.Add("willow");
                    break;
            }
            character.Equipment.Weapon = weapon;
            return character;
        }

        private static void SetRank(Character c, string nodeId, int rank) =>
            c.Progression.LearnedSkillRanks[nodeId] = rank;

        private static void PublishHit(Character hero, Action? action = null, bool crit = false, int naturalRoll = 0, bool isCombo = false, Actor? target = null)
        {
            SkillEffectRouter.Instance.RefreshForCharacter(hero);
            hero.Effects.ConsumedDamageModPercent = 0;
            hero.Effects.ConsumedAmpModPercent = 0;
            hero.Effects.ConsumedMultiHitMod = 0;
            CombatEventBus.Instance.Publish(new CombatEvent(
                crit ? CombatEventType.ActionCritical : CombatEventType.ActionHit, hero)
            {
                Action = action ?? new Action { Name = "TEST_SWING", Type = ActionType.Attack },
                Damage = 10,
                IsCritical = crit,
                IsCombo = isCombo,
                NaturalRollValue = naturalRoll,
                Target = target
            });
        }

        private static void TestAliasRoots()
        {
            Console.WriteLine("--- Class material alias roots ---");
            var withRoot = MakeHero("AliasOn", WeaponType.Mace);
            int with = ClassMaterialTagAlias.CountEquippedClassTags(withRoot, ClassMaterialTagAlias.Barbarian);
            TestBase.AssertTrue(with >= 1, "Bone counts as Barbarian when tribe_metal learned",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var noRoot = MakeHero("AliasOff", WeaponType.Mace, grantRoots: false);
            noRoot.Progression.LearnedSkillRanks.Clear();
            int without = ClassMaterialTagAlias.CountEquippedClassTags(noRoot, ClassMaterialTagAlias.Barbarian);
            TestBase.AssertEqual(0, without, "Bone does not count as Barbarian without tribe_metal",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(
                ClassMaterialTagAlias.ItemCountsAsTag(withRoot, withRoot.Equipment.Weapon, "barbarian"),
                "ItemCountsAsTag aliases Bone→barbarian with root",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestAgesAndPrefers()
        {
            Console.WriteLine("--- Ages and quality prefers ---");
            var barb = MakeHero("AgeB", WeaponType.Mace);
            SetRank(barb, "b-age1", 1);
            SkillEffectRouter.Instance.ResetFightState();
            PublishHit(barb);
            TestBase.AssertEqual(8.0, barb.Effects.ConsumedDamageModPercent,
                "first_bronze_age +8% per Barbarian item",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var war = MakeHero("AgeW", WeaponType.Sword);
            SetRank(war, "w-age1", 1);
            SkillEffectRouter.Instance.ResetFightState();
            war.Effects.ConsumedSpeedModPercent = 0;
            PublishHit(war);
            TestBase.AssertEqual(8.0, war.Effects.ConsumedSpeedModPercent,
                "first_formation +8% speed per Warrior item",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var rogue = MakeHero("AgeR", WeaponType.Dagger);
            SetRank(rogue, "r-age1", 2);
            SkillEffectRouter.Instance.ResetFightState();
            PublishHit(rogue);
            TestBase.AssertEqual(16.0, rogue.Effects.ConsumedDamageModPercent,
                "first_cut rank 2 = 16% with 1 Rogue item",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var wiz = MakeHero("AgeZ", WeaponType.Wand);
            SetRank(wiz, "z-age1", 1);
            SkillEffectRouter.Instance.ResetFightState();
            PublishHit(wiz);
            TestBase.AssertEqual(8.0, wiz.Effects.ConsumedAmpModPercent,
                "first_glyph +8% AMP per Wizard item",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEmptyFuryAndStreaks()
        {
            Console.WriteLine("--- Empty Fury / streaks / slot / wand ---");
            var fury = MakeHero("Fury", WeaponType.Mace);
            SetRank(fury, "b-empty", 1);
            while (fury.GetComboActions().Count > 0)
                fury.RemoveFromCombo(fury.GetComboActions()[^1]);
            SkillEffectRouter.Instance.ResetFightState();
            PublishHit(fury);
            TestBase.AssertTrue(fury.Stats.TempStrengthBonus >= 25, "empty_fury grants STR for empty slots",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var agi = MakeHero("StreakA", WeaponType.Sword);
            SetRank(agi, "w-streak", 1);
            SkillEffectRouter.Instance.ResetFightState();
            PublishHit(agi);
            PublishHit(agi);
            TestBase.AssertEqual(10, agi.Stats.TempAgilityBonus, "consecutive_agi stacks +5 per hit",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var tec = MakeHero("StreakT", WeaponType.Dagger);
            SetRank(tec, "r-streak", 1);
            SkillEffectRouter.Instance.ResetFightState();
            PublishHit(tec);
            TestBase.AssertEqual(5, tec.Stats.TempTechniqueBonus, "consecutive_tec +5 on connect",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var slot = MakeHero("SlotInt", WeaponType.Wand);
            SetRank(slot, "z-slotInt", 1);
            slot.Effects.ComboStep = 2;
            SkillEffectRouter.Instance.ResetFightState();
            PublishHit(slot);
            TestBase.AssertEqual(15, slot.Stats.TempIntelligenceBonus, "slot_int +5×slot3",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var wand = MakeHero("WandFocus", WeaponType.Wand);
            SetRank(wand, "z-comboInt", 2);
            SkillEffectRouter.Instance.ResetFightState();
            PublishHit(wand, isCombo: true);
            TestBase.AssertEqual(10, wand.Stats.TempIntelligenceBonus, "wand_combo_int +5×rank on combo",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestLootMarkets()
        {
            Console.WriteLine("--- Loot market bias ---");
            var hero = MakeHero("Market", WeaponType.Mace);
            SetRank(hero, "b-loot", 1);
            var biased = ItemMaterialRules.GetMarketBiasedMaterials(hero);
            TestBase.AssertTrue(biased != null && biased.Any(m =>
                    string.Equals(m, "Bone", StringComparison.OrdinalIgnoreCase)),
                "blood_market biases Bone/Steel/Damascus",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var mods = new List<Modification>
            {
                new() { Name = "Bone", PrefixCategory = "MATERIAL", ItemRank = "Common" },
                new() { Name = "Wood", PrefixCategory = "MATERIAL", ItemRank = "Common" },
                new() { Name = "Leather", PrefixCategory = "MATERIAL", ItemRank = "Common" },
                new() { Name = "Cloth", PrefixCategory = "MATERIAL", ItemRank = "Common" }
            };
            var rng = new Random(42);
            int boneHits = 0;
            const int trials = 200;
            for (int i = 0; i < trials; i++)
            {
                string pick = ItemMaterialRules.PickNonWeaponMaterial(mods, "Common", rng, biased);
                if (string.Equals(pick, "Bone", StringComparison.OrdinalIgnoreCase))
                    boneHits++;
            }
            // Unbiased ~25%; biased weight 3 vs 2 → ~3/9 ≈ 33%+
            TestBase.AssertTrue(boneHits > trials / 4,
                $"blood_market pick shifts toward Bone (got {boneHits}/{trials})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRites()
        {
            Console.WriteLine("--- Rite passives on learn ---");
            var hero = MakeHero("Rite", WeaponType.Mace);
            // Need path through puberty prereqs: b-might, b-lvl2 then b-puberty
            hero.Progression.BarbarianPoints = 100;
            hero.Progression.EnsureSkillTreeRootsGranted();
            hero.Progression.TryLearnSkillNode("b-might", requirePrimaryPath: false);
            hero.Progression.TryLearnSkillNode("b-lvl2", requirePrimaryPath: false);
            int strBefore = hero.Stats.Strength;
            // Apply rite via service helper (progression already ranked for prereq path)
            var learn = hero.Progression.TryLearnSkillNode("b-puberty", requirePrimaryPath: false);
            if (learn == CharacterProgression.LearnSkillResult.Success)
                SkillTreeService.ApplyRiteBonusOnFirstLearn(hero, "b-puberty");
            TestBase.AssertEqual(strBefore + 15, hero.Stats.Strength, "puberty +15 STR",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRollBanks()
        {
            Console.WriteLine("--- Roll→next-turn banks ---");
            var hide = MakeHero("Hide", WeaponType.Mace);
            SetRank(hide, "b-lvl2", 1);
            SkillEffectRouter.Instance.RefreshForCharacter(hide);
            SkillEffectRouter.Instance.ResetFightState();
            PublishHit(hide, naturalRoll: 12);
            SkillEffectRouter.Instance.ActivatePendingIronHideArmor(hide);
            int armor = SkillEffectRouter.Instance.GetSkillArmorBonus(hide);
            TestBase.AssertEqual(12, armor, "iron_hide_roll banks natural roll into armor",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var dmg = MakeHero("RollDmg", WeaponType.Dagger);
            SetRank(dmg, "r-lvl2", 1);
            SkillEffectRouter.Instance.RefreshForCharacter(dmg);
            SkillEffectRouter.Instance.ResetFightState();
            PublishHit(dmg, naturalRoll: 9);
            // Next swing consumes bank into pending DAMAGE_MOD
            PublishHit(dmg, naturalRoll: 1);
            var pending = dmg.Effects.PeekPendingActionBonusesNextHeroRoll();
            bool hasDmg = pending != null && pending.Any(b =>
                string.Equals(b.Type, "DAMAGE_MOD", StringComparison.OrdinalIgnoreCase) && b.Value >= 9);
            TestBase.AssertTrue(hasDmg,
                "roll_damage banks natural roll for next swing",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCadence()
        {
            Console.WriteLine("--- Cadence combo ease ---");
            var hero = MakeHero("Cadence", WeaponType.Mace);
            // 4 Barbarian-tagged pieces → +2 combo ease
            void TagArmor(Item? item)
            {
                if (item == null) return;
                item.Material = "Steel";
                item.Tags.Add("steel");
            }
            hero.Equipment.Head = TestDataBuilders.Armor().WithName("H").Build();
            hero.Equipment.Body = TestDataBuilders.Armor().WithName("B").Build();
            hero.Equipment.Legs = TestDataBuilders.Armor().WithName("L").Build();
            TagArmor(hero.Equipment.Head);
            TagArmor(hero.Equipment.Body);
            TagArmor(hero.Equipment.Legs);
            // Weapon already Bone
            SetRank(hero, "b-combo", 1);
            int tags = ClassMaterialTagAlias.CountEquippedClassTags(hero, ClassMaterialTagAlias.Barbarian);
            TestBase.AssertTrue(tags >= 4, $"expected ≥4 Barbarian tags, got {tags}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var tm = new ThresholdManager();
            int before = tm.GetComboThreshold(hero);
            SkillEffectRouter.Instance.ApplyCadenceThresholds(hero, tm);
            int after = tm.GetComboThreshold(hero);
            TestBase.AssertEqual(before - tags / 2, after, "heavy_cadence eases combo by tags/2",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCombatSpecials()
        {
            Console.WriteLine("--- Combat specials ---");
            var stunTarget = TestDataBuilders.Enemy().WithName("Dummy").Build();
            var mace = MakeHero("CritStun", WeaponType.Mace);
            SetRank(mace, "b-stun", 1);
            SkillEffectRouter.Instance.RefreshForCharacter(mace);
            SkillEffectRouter.Instance.ResetFightState();
            PublishHit(mace, crit: true, target: stunTarget);
            TestBase.AssertTrue(stunTarget.IsStunned && stunTarget.StunTurnsRemaining >= 1,
                "concussive_crit stuns on Mace crit",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var rogue = MakeHero("Multi", WeaponType.Dagger);
            SetRank(rogue, "r-precision", 1);
            var rogueAct = new Action
            {
                Name = "STAB",
                Type = ActionType.Attack,
                Tags = new List<string> { "rogue" }
            };
            SkillEffectRouter.Instance.ResetFightState();
            PublishHit(rogue, action: rogueAct);
            TestBase.AssertEqual(1.0, rogue.Effects.ConsumedMultiHitMod,
                "precision_multihit +1 on Rogue-tagged action",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var avenger = MakeHero("Avenge", WeaponType.Sword);
            SetRank(avenger, "w-avenge", 1);
            SkillEffectRouter.Instance.RefreshForCharacter(avenger);
            SkillEffectRouter.Instance.ResetFightState();
            PublishHit(avenger, action: new Action { Name = "AVENGE", Type = ActionType.Attack, Tags = new List<string> { "finisher" } });
            TestBase.AssertTrue(CombatTriggerContext.HasFightActionTag(avenger, "warrior"),
                "avenge grants fight-scoped warrior action tag",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var chaos = MakeHero("Chaos", WeaponType.Wand);
            SetRank(chaos, "z-chaos", 1);
            var strip = CombatTriggerContext.GetOrCreateStripState(chaos);
            strip.SkippedSlotsPassed = 2;
            strip.DisableSlot(0);
            SkillEffectRouter.Instance.RefreshForCharacter(chaos);
            SkillEffectRouter.Instance.ResetFightState();
            // ResetFightState clears fight tags but strip state is separate — re-seed skip count
            strip = CombatTriggerContext.GetOrCreateStripState(chaos);
            strip.SkippedSlotsPassed = 2;
            strip.DisableSlot(0);
            PublishHit(chaos, action: new Action { Name = "CONCENTRATED CHAOS", Type = ActionType.Attack });
            TestBase.AssertEqual(105.0, chaos.Effects.ConsumedDamageModPercent,
                "concentrated_chaos +35% × (2 skip + 1 disable)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var dagger = MakeHero("CritMiss", WeaponType.Dagger);
            SetRank(dagger, "r-critmiss", 1);
            SkillEffectRouter.Instance.RefreshForCharacter(dagger);
            SkillEffectRouter.Instance.ResetFightState();
            TestBase.AssertTrue(SkillEffectRouter.Instance.TryConsumeDaggerCritMissReroll(dagger),
                "dagger_critmiss_reroll consumes once",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!SkillEffectRouter.Instance.TryConsumeDaggerCritMissReroll(dagger),
                "dagger_critmiss_reroll only once per fight",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestActionUnlocks()
        {
            Console.WriteLine("--- Action unlocks ---");
            void AssertUnlock(string nodeId, string actionName, WeaponType wt)
            {
                var hero = MakeHero("Unlock" + actionName.Replace(" ", ""), wt);
                hero.Progression.BarbarianPoints = 100;
                hero.Progression.WarriorPoints = 100;
                hero.Progression.RoguePoints = 100;
                hero.Progression.WizardPoints = 100;
                hero.Progression.EnsureSkillTreeRootsGranted();
                // Directly mark learned and rebuild pool
                SetRank(hero, nodeId, 1);
                hero.Actions.AddClassActions(hero, hero.Progression, wt);
                bool has = hero.ActionPool.Any(a =>
                    string.Equals(a.action.Name, actionName, StringComparison.OrdinalIgnoreCase));
                TestBase.AssertTrue(has, $"{actionName} unlocked from {nodeId}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            AssertUnlock("b-might", "MIGHT", WeaponType.Mace);
            AssertUnlock("b-bludgeon", "BLUDGEON", WeaponType.Mace);
            AssertUnlock("w-cunning", "CUNNING", WeaponType.Sword);
            AssertUnlock("w-avenge", "AVENGE", WeaponType.Sword);
            AssertUnlock("r-concentrate", "CONCENTRATE", WeaponType.Dagger);
            AssertUnlock("z-readbook", "READ BOOK", WeaponType.Wand);
            AssertUnlock("z-chaos", "CONCENTRATED CHAOS", WeaponType.Wand);

            try
            {
                var might = ActionLoader.GetAction("MIGHT");
                TestBase.AssertTrue(might != null, "MIGHT exists in Actions.json",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                var bludgeon = ActionLoader.GetAction("BLUDGEON");
                TestBase.AssertTrue(bludgeon != null, "BLUDGEON exists in Actions.json",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, "ActionLoader unlock check: " + ex.Message,
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }
    }
}
