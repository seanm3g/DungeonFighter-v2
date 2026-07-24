using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Actions.Conditional;
using RPGGame.Actions.Execution;
using RPGGame.Combat.Events;
using RPGGame.Data;
using RPGGame.Entity.Services;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    public static class EquippedItemTriggerTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Equipped Item Trigger Tests ===\n");
            _run = _passed = _failed = 0;

            TestCatalogHasWaveTwoIdentities();
            TestKillReloadSetsPendingFaceWhenEquipped();
            TestUnequippedDoesNotFire();
            TestThresholdUsesTurnNotActionBank();
            TestNextActionDamageUsesActionScope();
            TestCritFaceMinUsesTurnScope();
            TestRoomClearHealFromItem();
            TestCatalogStampCoverage();
            TestItemGeneratorCopiesBundles();
            TestSwingSubjectTagFilterOnItemProc();
            TestMirrorSameActionFilterOnItemProc();
            TestEvenOddWhenTokens();
            TestIfSlotFilter();
            TestGoldSetArmorEquipEffect();
            TestGrantActionTagOverlay();
            TestUnarmedGrantPunchEquipEffect();
            TestSameSwingMirrorDamage();
            TestScaleFromArmorMagnitude();
            TestWhileEquippedTagAmpSameSwing();
            TestOnTakeHitDefenderPath();
            TestIfAttrFilter();

            TestBase.PrintSummary("Equipped Item Trigger Tests", _run, _passed, _failed);
        }

        private static void TestCatalogHasWaveTwoIdentities()
        {
            TestBase.SetCurrentTestName(nameof(TestCatalogHasWaveTwoIdentities));
            TestBase.AssertEqual(106, ItemTriggerIdentityCatalog.Count, "catalog count", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(106, ItemTriggerIdentityCatalog.Identities.Count, "identity list length", ref _run, ref _passed, ref _failed);
            for (int i = 0; i < 106; i++)
                TestBase.AssertEqual(i, ItemTriggerIdentityCatalog.Get(i).Index, $"identity index {i}", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("STR", ItemTriggerIdentityCatalog.Get(81).ScaleFrom, "StrCleave scaleFrom", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(ItemTriggerIdentityCatalog.Get(66).IsEquipEffect, "SwiftSchool equip", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("ONTAKEHIT", ItemTriggerIdentityCatalog.Get(101).When, "HurtPride when", ref _run, ref _passed, ref _failed);
        }

        private static void TestKillReloadSetsPendingFaceWhenEquipped()
        {
            TestBase.SetCurrentTestName(nameof(TestKillReloadSetsPendingFaceWhenEquipped));
            CombatTriggerContext.ResetForBattle();
            var hero = new Character("ItemTrigHero", 10);
            var foe = new Character("Foe", 5);
            var weapon = new WeaponItem("KillReloadBlade", 1, 5, 1.0, WeaponType.Sword)
            {
                TriggerBundles = new List<ActionTriggerBundle>
                {
                    ItemTriggerIdentityCatalog.ToBundle(ItemTriggerIdentityCatalog.Get(23))
                }
            };
            hero.TryEquipItem(weapon, "Weapon", out _, out _, ignoreAttributeRequirements: true);

            var kill = new CombatEvent(CombatEventType.EnemyDied, hero)
            {
                Target = foe,
                Action = new Action { Name = "Swing" },
                RollValue = 18
            };
            var msgs = new List<string>();
            bool applied = EquippedItemTriggerApplicator.ApplyFromAttacker(hero, foe, kill, msgs);
            TestBase.AssertTrue(applied, "kill reload applied", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(
                CombatTriggerContext.TryConsumePendingReplaceRollFace(hero, out int face) && face == 20,
                "pending replace face is 20", ref _run, ref _passed, ref _failed);
            CombatTriggerContext.ResetForBattle();
        }

        private static void TestUnequippedDoesNotFire()
        {
            TestBase.SetCurrentTestName(nameof(TestUnequippedDoesNotFire));
            CombatTriggerContext.ResetForBattle();
            var hero = new Character("BareHero", 10);
            var foe = new Character("Foe", 5);
            var kill = new CombatEvent(CombatEventType.EnemyDied, hero)
            {
                Target = foe,
                Action = new Action { Name = "Swing" }
            };
            var msgs = new List<string>();
            bool applied = EquippedItemTriggerApplicator.ApplyFromAttacker(hero, foe, kill, msgs);
            TestBase.AssertTrue(!applied, "no gear → no procs", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(
                !CombatTriggerContext.TryConsumePendingReplaceRollFace(hero, out _),
                "no pending replace face", ref _run, ref _passed, ref _failed);
            CombatTriggerContext.ResetForBattle();
        }

        private static void TestThresholdUsesTurnNotActionBank()
        {
            TestBase.SetCurrentTestName(nameof(TestThresholdUsesTurnNotActionBank));
            CombatTriggerContext.ResetForBattle();
            var hero = new Character("ThreshHero", 10);
            var foe = new Character("Foe", 5);
            hero.TryEquipItem(new HeadItem("AccBand", 1, 0)
            {
                TriggerBundles = new List<ActionTriggerBundle>
                {
                    ItemTriggerIdentityCatalog.ToBundle(ItemTriggerIdentityCatalog.Get(12))
                }
            }, "Head", out _, out _, ignoreAttributeRequirements: true);

            int beforePending = CountPendingActionCadence(hero);
            var connect = new CombatEvent(CombatEventType.ActionHit, hero)
            {
                Target = foe,
                Action = new Action { Name = "Hit" },
                RollValue = 14,
                IsMiss = false
            };
            EquippedItemTriggerApplicator.ApplyFromAttacker(hero, foe, connect, new List<string>());
            int afterPending = CountPendingActionCadence(hero);
            TestBase.AssertEqual(beforePending, afterPending, "ACC TURN does not grow ACTION bank", ref _run, ref _passed, ref _failed);
            var turn = hero.Effects.PeekTurnBonuses();
            bool hasAcc = turn != null && turn.Any(b =>
                string.Equals(b.Type, "ACCURACY", StringComparison.OrdinalIgnoreCase) && b.Value != 0);
            TestBase.AssertTrue(hasAcc, "TURN bonuses include ACCURACY", ref _run, ref _passed, ref _failed);
            CombatTriggerContext.ResetForBattle();
        }

        private static void TestNextActionDamageUsesActionScope()
        {
            TestBase.SetCurrentTestName(nameof(TestNextActionDamageUsesActionScope));
            CombatTriggerContext.ResetForBattle();
            var hero = new Character("DmgHero", 10);
            var foe = new Character("Foe", 5);
            var weapon = new WeaponItem("WoundBlade", 1, 5, 1.0, WeaponType.Sword)
            {
                TriggerBundles = new List<ActionTriggerBundle>
                {
                    ItemTriggerIdentityCatalog.ToBundle(ItemTriggerIdentityCatalog.Get(0))
                }
            };
            hero.TryEquipItem(weapon, "Weapon", out _, out _, ignoreAttributeRequirements: true);

            int before = CountPendingActionCadence(hero);
            var connect = new CombatEvent(CombatEventType.ActionHit, hero)
            {
                Target = foe,
                Action = new Action { Name = "Hit" },
                RollValue = 12
            };
            EquippedItemTriggerApplicator.ApplyFromAttacker(hero, foe, connect, new List<string>());
            int after = CountPendingActionCadence(hero);
            TestBase.AssertTrue(after > before, "ACTION scope deposits next-action damage bank", ref _run, ref _passed, ref _failed);
            CombatTriggerContext.ResetForBattle();
        }

        private static void TestCritFaceMinUsesTurnScope()
        {
            TestBase.SetCurrentTestName(nameof(TestCritFaceMinUsesTurnScope));
            CombatTriggerContext.ResetForBattle();
            var hero = new Character("CritFaceHero", 10);
            var foe = new Character("Foe", 5);
            var boots = new FeetItem("CritFaceBoots", 1, 1)
            {
                TriggerBundles = new List<ActionTriggerBundle>
                {
                    ItemTriggerIdentityCatalog.ToBundle(ItemTriggerIdentityCatalog.Get(20))
                }
            };
            hero.TryEquipItem(boots, "Feet", out _, out _, ignoreAttributeRequirements: true);

            int beforeAction = CountPendingActionCadence(hero);
            var connect = new CombatEvent(CombatEventType.ActionHit, hero)
            {
                Target = foe,
                Action = new Action { Name = "Hit" },
                RollValue = 16
            };
            EquippedItemTriggerApplicator.ApplyFromAttacker(hero, foe, connect, new List<string>());
            TestBase.AssertEqual(beforeAction, CountPendingActionCadence(hero),
                "crit_face_min TURN does not use ACTION bank", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(
                CombatTriggerContext.TryGetCritFaceMin(hero, out int min) && min == 19,
                "crit face min is 19", ref _run, ref _passed, ref _failed);
            CombatTriggerContext.ResetForBattle();
        }

        private static void TestRoomClearHealFromItem()
        {
            TestBase.SetCurrentTestName(nameof(TestRoomClearHealFromItem));
            CombatTriggerContext.ResetForBattle();
            var hero = new Character("RoomHealHero", 10);
            hero.CurrentHealth = Math.Max(1, hero.MaxHealth - 10);
            int hpBefore = hero.CurrentHealth;
            var chest = new ChestItem("RoomSipPlate", 1, 2)
            {
                TriggerBundles = new List<ActionTriggerBundle>
                {
                    ItemTriggerIdentityCatalog.ToBundle(ItemTriggerIdentityCatalog.Get(53))
                }
            };
            hero.TryEquipItem(chest, "Body", out _, out _, ignoreAttributeRequirements: true);

            var msgs = RoomClearedTriggerApplicator.ApplyForHero(hero);
            TestBase.AssertTrue(
                hero.CurrentHealth > hpBefore || msgs.Any(m => m.Contains("heals", StringComparison.OrdinalIgnoreCase)),
                "room clear item heal applied", ref _run, ref _passed, ref _failed);
            CombatTriggerContext.ResetForBattle();
        }

        private static void TestCatalogStampCoverage()
        {
            TestBase.SetCurrentTestName(nameof(TestCatalogStampCoverage));
            var cache = LootDataCache.Load();
            var weapons = cache.WeaponData;
            var armor = cache.ArmorData;
            TestBase.AssertTrue(weapons.Count > 0, "weapons loaded", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(armor.Count > 0, "armor loaded", ref _run, ref _passed, ref _failed);

            int idx = 0;
            foreach (var w in weapons)
            {
                AssertCatalogIdentityOnWeaponData(w, idx);
                idx++;
            }

            foreach (var a in armor)
            {
                AssertCatalogIdentityOnArmorData(a, idx);
                idx++;
            }
        }

        private static void AssertCatalogIdentityOnWeaponData(WeaponData w, int catalogIndex)
        {
            var identity = ItemTriggerIdentityCatalog.Get(catalogIndex);
            if (!string.IsNullOrWhiteSpace(w.TriggerName))
            {
                TestBase.AssertEqual(identity.Name, w.TriggerName!.Trim(),
                    $"weapon {w.Name} triggerName", ref _run, ref _passed, ref _failed);
                var item = ItemGenerator.GenerateWeaponItem(w);
                AssertCatalogIdentityOnItem(item.TriggerBundles, item.EquipEffects, $"weapon {w.Name}", catalogIndex);
                return;
            }

            AssertCatalogIdentityOnItem(w.TriggerBundles, w.EquipEffects, $"weapon {w.Name}", catalogIndex);
        }

        private static void AssertCatalogIdentityOnArmorData(ArmorData a, int catalogIndex)
        {
            var identity = ItemTriggerIdentityCatalog.Get(catalogIndex);
            if (!string.IsNullOrWhiteSpace(a.TriggerName))
            {
                TestBase.AssertEqual(identity.Name, a.TriggerName!.Trim(),
                    $"armor {a.Name} triggerName", ref _run, ref _passed, ref _failed);
                var item = ItemGenerator.GenerateArmorItem(a);
                AssertCatalogIdentityOnItem(item.TriggerBundles, item.EquipEffects, $"armor {a.Name}", catalogIndex);
                return;
            }

            AssertCatalogIdentityOnItem(a.TriggerBundles, a.EquipEffects, $"armor {a.Name}", catalogIndex);
        }

        private static void AssertCatalogIdentityOnItem(
            List<ActionTriggerBundle>? combatBundles,
            List<ActionTriggerBundle>? equipBundles,
            string label,
            int catalogIndex)
        {
            var identity = ItemTriggerIdentityCatalog.Get(catalogIndex);
            var bundles = identity.IsEquipEffect ? equipBundles : combatBundles;
            TestBase.AssertTrue(bundles != null && bundles.Count == 1,
                $"{label} has exactly one {(identity.IsEquipEffect ? "equip" : "trigger")} bundle",
                ref _run, ref _passed, ref _failed);
            if (bundles == null || bundles.Count == 0)
                return;
            var b = bundles[0];
            TestBase.AssertTrue(b.IsEnabled, $"{label} bundle enabled", ref _run, ref _passed, ref _failed);
            var expected = ItemTriggerIdentityCatalog.ToBundle(identity);
            TestBase.AssertEqual(expected.Mechanics, b.Mechanics,
                $"{label} mechanics match identity {catalogIndex % ItemTriggerIdentityCatalog.Count}",
                ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(ItemTriggerIdentityCatalog.IsKnownMechanic(b.Mechanics),
                $"{label} mechanic is known", ref _run, ref _passed, ref _failed);
        }

        private static void AssertSingleKnownBundle(List<ActionTriggerBundle>? bundles, string label, int catalogIndex)
        {
            AssertCatalogIdentityOnItem(bundles, null, label, catalogIndex);
        }

        private static void TestItemGeneratorCopiesBundles()
        {
            TestBase.SetCurrentTestName(nameof(TestItemGeneratorCopiesBundles));
            var data = new WeaponData
            {
                Name = "CopyTest",
                Type = "Sword",
                BaseDamage = 3,
                AttackSpeed = 1,
                Tier = 1,
                TriggerBundles = new List<ActionTriggerBundle>
                {
                    ItemTriggerIdentityCatalog.ToBundle(ItemTriggerIdentityCatalog.Get(3))
                }
            };
            var item = ItemGenerator.GenerateWeaponItem(data);
            TestBase.AssertEqual(1, item.TriggerBundles.Count, "copied bundle count", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("hero_next_action_amp", item.TriggerBundles[0].Mechanics,
                "copied mechanics", ref _run, ref _passed, ref _failed);
            item.TriggerBundles[0].Mechanics = "changed";
            TestBase.AssertEqual("hero_next_action_amp", data.TriggerBundles![0].Mechanics,
                "deep clone", ref _run, ref _passed, ref _failed);

            var named = new WeaponData
            {
                Name = "NamedTrig",
                Type = "Sword",
                BaseDamage = 3,
                AttackSpeed = 1,
                Tier = 1,
                TriggerName = ItemTriggerIdentityCatalog.Get(0).Name
            };
            var namedItem = ItemGenerator.GenerateWeaponItem(named);
            TestBase.AssertEqual(1, namedItem.TriggerBundles.Count, "triggerName resolve count", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("hero_next_action_damage", namedItem.TriggerBundles[0].Mechanics,
                "triggerName resolve mechanics", ref _run, ref _passed, ref _failed);
        }

        private static void TestSwingSubjectTagFilterOnItemProc()
        {
            TestBase.SetCurrentTestName(nameof(TestSwingSubjectTagFilterOnItemProc));
            CombatTriggerContext.ResetForBattle();
            var hero = new Character("TagHero", 10);
            var foe = new Character("Foe", 5);
            hero.TryEquipItem(new WeaponItem("BarbRingBlade", 1, 5, 1.0, WeaponType.Mace)
            {
                TriggerBundles = new List<ActionTriggerBundle>
                {
                    ItemTriggerIdentityCatalog.ToBundle(ItemTriggerIdentityCatalog.Get(61))
                }
            }, "Weapon", out _, out _, ignoreAttributeRequirements: true);

            int before = CountPendingActionCadence(hero);
            var swing = new Action { Name = "SLAM", Tags = new List<string> { "barbarian" } };
            var connect = new CombatEvent(CombatEventType.ActionHit, hero)
            {
                Target = foe,
                Action = swing,
                RollValue = 12
            };
            EquippedItemTriggerApplicator.ApplyFromAttacker(hero, foe, connect, new List<string>());
            TestBase.AssertTrue(CountPendingActionCadence(hero) > before,
                "IFACTIONHASTAG uses swing tags on item proc", ref _run, ref _passed, ref _failed);

            var bare = new Character("NoTag", 10);
            bare.TryEquipItem(new WeaponItem("BarbRingBlade2", 1, 5, 1.0, WeaponType.Mace)
            {
                TriggerBundles = new List<ActionTriggerBundle>
                {
                    ItemTriggerIdentityCatalog.ToBundle(ItemTriggerIdentityCatalog.Get(61))
                }
            }, "Weapon", out _, out _, ignoreAttributeRequirements: true);
            int before2 = CountPendingActionCadence(bare);
            EquippedItemTriggerApplicator.ApplyFromAttacker(bare, foe, new CombatEvent(CombatEventType.ActionHit, bare)
            {
                Target = foe,
                Action = new Action { Name = "STRIKE", Tags = new List<string> { "warrior" } },
                RollValue = 12
            }, new List<string>());
            TestBase.AssertEqual(before2, CountPendingActionCadence(bare),
                "non-matching swing tag does not fire", ref _run, ref _passed, ref _failed);
            CombatTriggerContext.ResetForBattle();
        }

        private static void TestMirrorSameActionFilterOnItemProc()
        {
            TestBase.SetCurrentTestName(nameof(TestMirrorSameActionFilterOnItemProc));
            CombatTriggerContext.ResetForBattle();
            var hero = new Character("MirrorHero", 10);
            var foe = new Character("Foe", 5);
            hero.TryEquipItem(new HeadItem("MirrorBand", 1, 0)
            {
                TriggerBundles = new List<ActionTriggerBundle>
                {
                    ItemTriggerIdentityCatalog.ToBundle(ItemTriggerIdentityCatalog.Get(64))
                }
            }, "Head", out _, out _, ignoreAttributeRequirements: true);

            CombatTriggerContext.NotifySwingResolved(hero, new Action { Name = "SLAM" }, connected: true, missed: false);
            var msgs = new List<string>();
            bool applied = EquippedItemTriggerApplicator.ApplySameSwingDamageMods(
                hero, foe, new Action { Name = "SLAM" }, 14, 14, false, false, msgs);
            TestBase.AssertTrue(applied && hero.Effects.ConsumedDamageModPercent >= 100,
                "IFSAMESACTION uses swing name for item mirror", ref _run, ref _passed, ref _failed);
            CombatTriggerContext.ResetForBattle();
        }

        private static void TestEvenOddWhenTokens()
        {
            TestBase.SetCurrentTestName(nameof(TestEvenOddWhenTokens));
            var swing = new Action { Name = "Hit" };
            var evenEvt = new CombatEvent(CombatEventType.ActionHit, new Character("E", 1))
            {
                Action = swing,
                NaturalRollValue = 8
            };
            var oddEvt = new CombatEvent(CombatEventType.ActionHit, new Character("O", 1))
            {
                Action = swing,
                NaturalRollValue = 7
            };
            TestBase.AssertTrue(ActionTriggerGate.MatchesConditionToken("ONEVEN", swing, evenEvt),
                "ONEVEN on 8", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!ActionTriggerGate.MatchesConditionToken("ONEVEN", swing, oddEvt),
                "ONEVEN rejects 7", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(ActionTriggerGate.MatchesConditionToken("ONODD", swing, oddEvt),
                "ONODD on 7", ref _run, ref _passed, ref _failed);
        }

        private static void TestIfSlotFilter()
        {
            TestBase.SetCurrentTestName(nameof(TestIfSlotFilter));
            CombatTriggerContext.ResetForBattle();
            var hero = new Character("SlotHero", 10);
            var foe = new Character("Foe", 5);
            var a1 = new Action { Name = "A1", IsComboAction = true };
            var a2 = new Action { Name = "A2", IsComboAction = true };
            var a3 = new Action { Name = "A3", IsComboAction = true };
            hero.AddAction(a1, 1.0);
            hero.AddAction(a2, 1.0);
            hero.AddAction(a3, 1.0);
            // Equip first — gear change resets strip; build combo afterward.
            hero.TryEquipItem(new FeetItem("SlotBoots", 1, 1)
            {
                TriggerBundles = new List<ActionTriggerBundle>
                {
                    ItemTriggerIdentityCatalog.ToBundle(ItemTriggerIdentityCatalog.Get(63))
                }
            }, "Feet", out _, out _, ignoreAttributeRequirements: true);
            hero.Actions.AddToCombo(a1, 8);
            hero.Actions.AddToCombo(a2, 8);
            hero.Actions.AddToCombo(a3, 8);
            TestBase.AssertEqual(3, hero.GetComboActions().Count, "combo length 3", ref _run, ref _passed, ref _failed);
            hero.ComboStep = 2; // 1-based slot 3

            var carrier = EquippedItemTriggerApplicator.BuildCarrierAction(hero.Feet!);
            var connect = new CombatEvent(CombatEventType.ActionHit, hero)
            {
                Target = foe,
                Action = a3,
                RollValue = 12
            };
            bool pass = ActionTriggerGate.PassesNonOutcomeFilters(carrier, connect);
            TestBase.AssertTrue(pass, "IFSLOT:3 passes at ComboStep 2", ref _run, ref _passed, ref _failed);
            hero.ComboStep = 0;
            bool fail = ActionTriggerGate.PassesNonOutcomeFilters(carrier, connect);
            TestBase.AssertTrue(!fail, "IFSLOT:3 fails at ComboStep 0", ref _run, ref _passed, ref _failed);
            CombatTriggerContext.ResetForBattle();
        }

        private static void TestGoldSetArmorEquipEffect()
        {
            TestBase.SetCurrentTestName(nameof(TestGoldSetArmorEquipEffect));
            var hero = new Character("GoldHero", 10);
            var chest = new ChestItem("GoldPlate", 1, 2)
            {
                Tags = new List<string> { "gold" },
                EquipEffects = new List<ActionTriggerBundle>
                {
                    ItemTriggerIdentityCatalog.ToBundle(ItemTriggerIdentityCatalog.Get(58))
                }
            };
            hero.TryEquipItem(chest, "Body", out _, out _, ignoreAttributeRequirements: true);
            int armor = hero.Equipment.GetTotalArmor(hero);
            // Intrinsic 2 + equip effect +2 when gold tagged
            TestBase.AssertTrue(armor >= 4, "gold set armor +2 while equipped", ref _run, ref _passed, ref _failed);
        }

        private static void TestGrantActionTagOverlay()
        {
            TestBase.SetCurrentTestName(nameof(TestGrantActionTagOverlay));
            var hero = new Character("TagOverlay", 10);
            hero.TryEquipItem(new HeadItem("BarbHelm", 1, 0)
            {
                EquipEffects = new List<ActionTriggerBundle>
                {
                    ItemTriggerIdentityCatalog.ToBundle(ItemTriggerIdentityCatalog.Get(62))
                }
            }, "Head", out _, out _, ignoreAttributeRequirements: true);
            ItemEquipEffectApplicator.RefreshGrantedActionTags(hero);
            TestBase.AssertTrue(
                hero.EquipmentGrantedActionTags.Contains("barbarian"),
                "grant_action_tag overlay", ref _run, ref _passed, ref _failed);
            var swing = new Action { Name = "STRIKE", Tags = new List<string>() };
            TestBase.AssertTrue(
                ActionTriggerPredicates.SubjectHasTag(hero, swing, "barbarian"),
                "overlay counts for SubjectHasTag", ref _run, ref _passed, ref _failed);
        }

        private static void TestUnarmedGrantPunchEquipEffect()
        {
            TestBase.SetCurrentTestName(nameof(TestUnarmedGrantPunchEquipEffect));
            var hero = new Character("UnarmedGrant", 10);
            // No weapon
            hero.TryEquipItem(new LegsItem("BareLegs", 1, 1)
            {
                EquipEffects = new List<ActionTriggerBundle>
                {
                    ItemTriggerIdentityCatalog.ToBundle(ItemTriggerIdentityCatalog.Get(65))
                }
            }, "Legs", out _, out _, ignoreAttributeRequirements: true);
            var granted = ItemEquipEffectApplicator.GetGrantedActionNames(hero);
            TestBase.AssertTrue(
                granted.Any(n => string.Equals(n, GameConstants.TrainingGroundTutorialActionName,
                    StringComparison.OrdinalIgnoreCase)),
                "IFUNARMED grant_action lists PUNCH HARD", ref _run, ref _passed, ref _failed);
        }

        private static void TestSameSwingMirrorDamage()
        {
            TestBase.SetCurrentTestName(nameof(TestSameSwingMirrorDamage));
            CombatTriggerContext.ResetForBattle();
            var hero = new Character("SameSwing", 10);
            var foe = new Character("Foe", 5);
            hero.Effects.ConsumedDamageModPercent = 0;
            hero.TryEquipItem(new WeaponItem("EchoBlade", 1, 5, 1.0, WeaponType.Sword)
            {
                TriggerBundles = new List<ActionTriggerBundle>
                {
                    ItemTriggerIdentityCatalog.ToBundle(ItemTriggerIdentityCatalog.Get(64))
                }
            }, "Weapon", out _, out _, ignoreAttributeRequirements: true);
            CombatTriggerContext.NotifySwingResolved(hero, new Action { Name = "STRIKE" }, connected: true, missed: false);
            EquippedItemTriggerApplicator.ApplySameSwingDamageMods(
                hero, foe, new Action { Name = "STRIKE" }, 15, 15, false, false, new List<string>());
            TestBase.AssertEqual(100, (int)hero.Effects.ConsumedDamageModPercent,
                "mirror adds 100% same-swing damage", ref _run, ref _passed, ref _failed);
            CombatTriggerContext.ResetForBattle();
        }

        private static void TestScaleFromArmorMagnitude()
        {
            TestBase.SetCurrentTestName(nameof(TestScaleFromArmorMagnitude));
            var hero = new Character("ScaleArmor", 10);
            hero.Level = 5;
            var chest = new ChestItem("LevelMail", 1, 0)
            {
                EquipEffects = new List<ActionTriggerBundle>
                {
                    ItemTriggerIdentityCatalog.ToBundle(ItemTriggerIdentityCatalog.Get(87))
                }
            };
            hero.TryEquipItem(chest, "Body", out _, out _, ignoreAttributeRequirements: true);
            int armor = ItemEquipEffectApplicator.GetEquippedArmorBonus(hero);
            TestBase.AssertEqual(5, armor, "LevelLadderArmor = 1 × level 5", ref _run, ref _passed, ref _failed);
        }

        private static void TestWhileEquippedTagAmpSameSwing()
        {
            TestBase.SetCurrentTestName(nameof(TestWhileEquippedTagAmpSameSwing));
            var hero = new Character("TagAmp", 10);
            var foe = new Character("Foe", 5);
            hero.Effects.ConsumedDamageModPercent = 0;
            hero.TryEquipItem(new WeaponItem("BludgeonClub", 1, 5, 1.0, WeaponType.Mace)
            {
                EquipEffects = new List<ActionTriggerBundle>
                {
                    ItemTriggerIdentityCatalog.ToBundle(ItemTriggerIdentityCatalog.Get(67))
                }
            }, "Weapon", out _, out _, ignoreAttributeRequirements: true);

            EquippedItemTriggerApplicator.ApplySameSwingDamageMods(
                hero, foe, new Action { Name = "SMASH", Tags = new List<string> { "bludgeon" } },
                12, 12, false, false, new List<string>());
            TestBase.AssertEqual(12, (int)hero.Effects.ConsumedDamageModPercent,
                "BludgeonChorus +12% when tagged", ref _run, ref _passed, ref _failed);

            hero.Effects.ConsumedDamageModPercent = 0;
            EquippedItemTriggerApplicator.ApplySameSwingDamageMods(
                hero, foe, new Action { Name = "POKE", Tags = new List<string> { "swift" } },
                12, 12, false, false, new List<string>());
            TestBase.AssertEqual(0, (int)hero.Effects.ConsumedDamageModPercent,
                "BludgeonChorus skips non-bludgeon", ref _run, ref _passed, ref _failed);
        }

        private static void TestOnTakeHitDefenderPath()
        {
            TestBase.SetCurrentTestName(nameof(TestOnTakeHitDefenderPath));
            CombatTriggerContext.ResetForBattle();
            var hero = new Character("HurtPrideHero", 10);
            var foe = new Character("Foe", 5);
            hero.TryEquipItem(new ChestItem("SpiteMail", 1, 1)
            {
                TriggerBundles = new List<ActionTriggerBundle>
                {
                    ItemTriggerIdentityCatalog.ToBundle(ItemTriggerIdentityCatalog.Get(101))
                }
            }, "Body", out _, out _, ignoreAttributeRequirements: true);

            var hit = new CombatEvent(CombatEventType.ActionHit, foe)
            {
                Target = hero,
                Action = new Action { Name = "BITE" },
                IsMiss = false
            };
            var msgs = new List<string>();
            bool applied = EquippedItemTriggerApplicator.ApplyFromDefender(hero, foe, hit, msgs);
            TestBase.AssertTrue(applied, "ONTAKEHIT applied from defender", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(CountPendingActionCadence(hero) > 0,
                "HurtPride deposits next-action damage", ref _run, ref _passed, ref _failed);

            // Attacker connect should not fire ONTAKEHIT on attacker's gear
            var attackerWeapon = new WeaponItem("NoTake", 1, 5, 1.0, WeaponType.Sword)
            {
                TriggerBundles = new List<ActionTriggerBundle>
                {
                    ItemTriggerIdentityCatalog.ToBundle(ItemTriggerIdentityCatalog.Get(101))
                }
            };
            var attacker = new Character("Atk", 10);
            attacker.TryEquipItem(attackerWeapon, "Weapon", out _, out _, ignoreAttributeRequirements: true);
            var connect = new CombatEvent(CombatEventType.ActionHit, attacker)
            {
                Target = foe,
                Action = new Action { Name = "SWING" },
                IsMiss = false
            };
            bool attackerFire = EquippedItemTriggerApplicator.ApplyFromAttacker(attacker, foe, connect, new List<string>());
            TestBase.AssertTrue(!attackerFire || CountPendingActionCadence(attacker) == 0,
                "ONTAKEHIT does not fire via attacker connect path", ref _run, ref _passed, ref _failed);
            CombatTriggerContext.ResetForBattle();
        }

        private static void TestIfAttrFilter()
        {
            TestBase.SetCurrentTestName(nameof(TestIfAttrFilter));
            var weak = new Character("Weak", 10);
            weak.Strength = 3;
            var strong = new Character("Strong", 10);
            strong.Strength = 12;

            var bundle = new ActionTriggerBundle
            {
                When = "WHILE_EQUIPPED",
                Count = "1",
                Mechanics = "armor",
                Value = 5,
                Filters = new List<string> { "IFATTR:STR>=8" }
            };
            var weakItem = new ChestItem("GateMail", 1, 0) { EquipEffects = new List<ActionTriggerBundle> { bundle } };
            var strongItem = new ChestItem("GateMail2", 1, 0)
            {
                EquipEffects = new List<ActionTriggerBundle>
                {
                    new ActionTriggerBundle
                    {
                        When = "WHILE_EQUIPPED",
                        Count = "1",
                        Mechanics = "armor",
                        Value = 5,
                        Filters = new List<string> { "IFATTR:STR>=8" }
                    }
                }
            };
            weak.TryEquipItem(weakItem, "Body", out _, out _, ignoreAttributeRequirements: true);
            strong.TryEquipItem(strongItem, "Body", out _, out _, ignoreAttributeRequirements: true);
            TestBase.AssertEqual(0, ItemEquipEffectApplicator.GetEquippedArmorBonus(weak),
                "IFATTR blocks weak STR", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(5, ItemEquipEffectApplicator.GetEquippedArmorBonus(strong),
                "IFATTR passes strong STR", ref _run, ref _passed, ref _failed);
        }

        private static int CountPendingActionCadence(Character hero)
        {
            var peek = hero.Effects.PeekPendingActionBonusesNextHeroRoll();
            return peek?.Count ?? 0;
        }
    }
}
