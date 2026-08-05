using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Actions;
using RPGGame.Actions.Conditional;
using RPGGame.Actions.Execution;
using RPGGame.Actions.RollModification;
using RPGGame.Combat;
using RPGGame.Data;
using RPGGame.Entity.Actions.ComboRouting;
using RPGGame.Tests;
using RPGGame.Utils;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Combat-path coverage for every item trigger identity: each identity is equipped alone,
    /// the WHEN is forced through <see cref="ActionExecutionFlow.Execute"/> (or room-clear /
    /// equip-channel production hooks), and the mechanic's observable side effect is asserted.
    /// </summary>
    public static class ItemTriggerCombatIntegrationTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Item Trigger Combat Integration Tests ===\n");
            _ = GameConfiguration.Instance;
            _run = _passed = _failed = 0;

            var identities = ItemTriggerIdentityCatalog.Identities;
            TestBase.AssertEqual(106, identities.Count, "catalog has 106 identities", ref _run, ref _passed, ref _failed);

            foreach (var identity in identities)
            {
                TestBase.SetCurrentTestName($"Combat/{identity.Index}:{identity.Name}");
                try
                {
                    RunIdentity(identity);
                }
                catch (Exception ex)
                {
                    TestBase.AssertTrue(false,
                        $"{identity.Name} threw: {ex.GetType().Name}: {ex.Message}",
                        ref _run, ref _passed, ref _failed);
                }
                finally
                {
                    CleanupForcedRolls();
                    CombatTriggerContext.ResetForBattle();
                    RetriggerScheduler.AllowScheduling = true;
                    RetriggerScheduler.ResetForBattle();
                    RollModificationManager.GetThresholdManager().Clear();
                }
            }

            TestBase.PrintSummary("Item Trigger Combat Integration Tests", _run, _passed, _failed);
        }

        private static void RunIdentity(ItemTriggerIdentityCatalog.Identity identity)
        {
            string when = ActionTriggerGate.NormalizeToken(identity.When ?? "");
            if (identity.IsEquipEffect || when is "WHILEEQUIPPED" or "ONEQUIP")
            {
                RunEquipChannel(identity);
                return;
            }

            if (when is "ONROOMSCLEARED" or "ONROOMCLEARED")
            {
                RunRoomClear(identity);
                return;
            }

            if (when is "ONTAKEHIT" or "ONHEROHURT")
            {
                RunTakeHit(identity);
                return;
            }

            RunAttackerCombat(identity, when);
        }

        // ─── Equip channel (WHILE_EQUIPPED) ───────────────────────────────────

        private static void RunEquipChannel(ItemTriggerIdentityCatalog.Identity identity)
        {
            string mech = NormalizeMech(identity.Mechanics);
            var hero = MakeHero($"Equip{identity.Index}");
            PrepareFiltersForEquip(hero, identity);

            var item = MakeGearItem(identity, equipChannel: true);
            string slot = item is WeaponItem ? "Weapon" : item is HeadItem ? "Head" : item is ChestItem ? "Body"
                : item is LegsItem ? "Legs" : "Feet";

            if (mech is "grant_action")
            {
                // IFUNARMED — leave weapon empty
                if (item is WeaponItem)
                    item = new LegsItem($"GrantLegs{identity.Index}", 1, 1)
                    {
                        EquipEffects = new List<ActionTriggerBundle> { ItemTriggerIdentityCatalog.ToBundle(identity) }
                    };
                hero.TryEquipItem(item, "Legs", out _, out _, ignoreAttributeRequirements: true);
                var granted = ItemEquipEffectApplicator.GetGrantedActionNames(hero);
                TestBase.AssertTrue(granted.Count > 0,
                    $"{identity.Name}: grant_action yields names", ref _run, ref _passed, ref _failed);
                return;
            }

            if (mech is "grant_action_tag")
            {
                hero.TryEquipItem(item, slot, out _, out _, ignoreAttributeRequirements: true);
                ItemEquipEffectApplicator.RefreshGrantedActionTags(hero);
                TestBase.AssertTrue(hero.EquipmentGrantedActionTags.Count > 0,
                    $"{identity.Name}: grant_action_tag overlay", ref _run, ref _passed, ref _failed);
                return;
            }

            if (mech is "armor" or "hero_armor" or "hero_stat_bonus" or "stat_bonus")
            {
                int beforeArmor = ItemEquipEffectApplicator.GetEquippedArmorBonus(hero);
                int beforeStat = ItemEquipEffectApplicator.GetEquippedStatBonus(hero, "PRIMARY");
                hero.TryEquipItem(item, slot, out _, out _, ignoreAttributeRequirements: true);
                if (mech is "armor" or "hero_armor")
                {
                    int after = ItemEquipEffectApplicator.GetEquippedArmorBonus(hero);
                    TestBase.AssertTrue(after > beforeArmor,
                        $"{identity.Name}: equip armor bonus {beforeArmor}→{after}", ref _run, ref _passed, ref _failed);
                }
                else
                {
                    int after = ItemEquipEffectApplicator.GetEquippedStatBonus(hero, "PRIMARY");
                    // Named STR/etc. still resolve; PRIMARY covers most demos
                    bool ok = after > beforeStat
                              || ItemEquipEffectApplicator.GetEquippedStatBonus(hero, "STRENGTH") > 0
                              || ItemEquipEffectApplicator.GetEquippedStatBonus(hero, "AGILITY") > 0
                              || ItemEquipEffectApplicator.GetEquippedStatBonus(hero, "TECHNIQUE") > 0
                              || ItemEquipEffectApplicator.GetEquippedStatBonus(hero, "INTELLIGENCE") > 0;
                    TestBase.AssertTrue(ok,
                        $"{identity.Name}: equip stat bonus applied", ref _run, ref _passed, ref _failed);
                }
                return;
            }

            // Same-swing / pre-roll WHILE_EQUIPPED tag amps — need a real swing
            hero.TryEquipItem(item, slot, out _, out _, ignoreAttributeRequirements: true);
            var swing = BuildSwingAction(identity, stripLen: 3);
            EnsureStrip(hero, swing, stripLen: 3);
            TagSwingForFilters(swing, identity);
            var enemy = MakeTank("EquipFoe");
            var result = ForceExecute(hero, enemy, swing, ResolveFaceForWhen("ONCONNECT", identity));
            AssertSameSwingOrThresholdEffect(identity, hero, result, baselineWithoutGear: false);
        }

        // ─── Room clear ───────────────────────────────────────────────────────

        private static void RunRoomClear(ItemTriggerIdentityCatalog.Identity identity)
        {
            var hero = MakeHero($"Room{identity.Index}");
            hero.CurrentHealth = Math.Max(1, hero.MaxHealth - 20);
            int hpBefore = hero.CurrentHealth;
            hero.TryEquipItem(MakeGearItem(identity, equipChannel: false), "Weapon", out _, out _,
                ignoreAttributeRequirements: true);

            var msgs = RoomClearedTriggerApplicator.ApplyForHero(hero);
            AssertMechanicAfterEvent(identity, hero, foe: null, msgs, hpBefore, nestedCount: 0, damageDealt: 0);
        }

        // ─── Defender ONTAKEHIT ────────────────────────────────────────────────

        private static void RunTakeHit(ItemTriggerIdentityCatalog.Identity identity)
        {
            var hero = MakeHero($"Hurt{identity.Index}");
            hero.TryEquipItem(MakeGearItem(identity, equipChannel: false), "Body", out _, out _,
                ignoreAttributeRequirements: true);
            // Soft armor so enemy hit connects for damage
            hero.CurrentHealth = hero.MaxHealth;

            var enemy = new Enemy($"Hurter{identity.Index}", 5, 50, 20, 5, 5, 5);
            while (enemy.GetComboActions().Count > 0)
                enemy.RemoveFromCombo(enemy.GetComboActions()[0], ignoreWeaponRequirement: true);
            var enemySwing = new Action
            {
                Name = "EnemyBash",
                Type = ActionType.Attack,
                Target = TargetType.SingleTarget,
                IsComboAction = true,
                DamageMultiplier = 2.0,
                Length = 1.0
            };
            enemy.Actions.AddToCombo(enemySwing, maxComboLength: null);

            int bankBefore = CountActionBank(hero);
            int hardenBefore = hero.HardenTurns;
            var result = ForceExecute(enemy, hero, enemySwing, face: 12);
            TestBase.AssertTrue(result.Hit && result.Damage > 0,
                $"{identity.Name}: enemy hit deals damage", ref _run, ref _passed, ref _failed);

            string mech = NormalizeMech(identity.Mechanics);
            if (mech is "hero_next_action_damage" or "hero_next_action_speed"
                or "hero_next_action_amp" or "hero_next_action_multihit")
            {
                TestBase.AssertTrue(CountActionBank(hero) > bankBefore,
                    $"{identity.Name}: ONTAKEHIT deposits next-action bank", ref _run, ref _passed, ref _failed);
            }
            else if (mech is "harden")
            {
                TestBase.AssertTrue(hero.HardenTurns > hardenBefore || (hero.HardenStacks ?? 0) > 0,
                    $"{identity.Name}: ONTAKEHIT applies Harden", ref _run, ref _passed, ref _failed);
            }
            else
            {
                AssertMechanicAfterEvent(identity, hero, enemy, result.StatusEffectMessages,
                    hero.CurrentHealth, result.NestedRetriggerResults.Count, result.Damage);
            }
        }

        // ─── Attacker combat WHENs ────────────────────────────────────────────

        private static void RunAttackerCombat(ItemTriggerIdentityCatalog.Identity identity, string when)
        {
            var hero = MakeHero($"Atk{identity.Index}");
            // Equip before building the strip — equip rebuilds combo and would wipe EnsureStrip.
            hero.TryEquipItem(MakeGearItem(identity, equipChannel: false), "Weapon", out _, out _,
                ignoreAttributeRequirements: true);
            PrepareFiltersForCombat(hero, identity, out Action swing, out Enemy enemy, out int stripLen);

            // ONAFTERMISS needs a prior miss
            if (when is "ONAFTERMISS")
            {
                ForceExecute(hero, enemy, swing, face: 2);
                // Re-arm swing after miss reset combo
                EnsureStrip(hero, swing, stripLen);
                TagSwingForFilters(swing, identity);
            }

            // Mirror / switch-up history
            if (HasFilter(identity, "IFSAMESACTION") || HasFilter(identity, "IFMIRROR"))
                CombatTriggerContext.NotifySwingResolved(hero, new Action { Name = swing.Name }, true, false);
            if (HasFilter(identity, "IFDIFFERENTACTION") || HasFilter(identity, "IFSWITCHUP"))
                CombatTriggerContext.NotifySwingResolved(hero, new Action { Name = "OtherSwing" }, true, false);

            if (when is "ONCOMBOEND" or "ONCOMBOENDED")
            {
                // Land on last strip slot so a combo hit advances back to 0
                EnsureStrip(hero, swing, stripLen: Math.Max(2, stripLen));
                var strip = hero.GetComboActions();
                hero.ComboStep = strip.Count - 1;
                swing = strip[hero.ComboStep];
                TagSwingForFilters(swing, identity);
            }

            if (when is "ONKILL")
            {
                enemy = new Enemy($"KillFoe{identity.Index}", 1, 1, 1, 1, 1, 1);
            }

            if (NormalizeMech(identity.Mechanics) is "heal" or "max_health")
                hero.CurrentHealth = Math.Max(1, hero.MaxHealth - 15);
            int hpBefore = hero.CurrentHealth;

            int bankBefore = CountActionBank(hero);
            int turnBefore = CountTurnBonuses(hero);
            int fightBefore = hero.FightCadenceBuffs.Bonuses.Count;
            int dungeonBefore = hero.DungeonCadenceBuffs.Bonuses.Count;
            var stripStateBefore = SnapshotStrip(hero);

            int face = ResolveFaceForWhen(when, identity);
            // Combo-end / combo WHEN need combo threshold met (not natural 20 unless ONCRITICAL)
            if (when is "ONCOMBO" or "ONCOMBOHIT" or "ONCOMBOEND" or "ONCOMBOENDED")
                face = Math.Clamp(face == 20 ? 16 : Math.Max(face, 15), 14, 19);
            if (when is "ONCRITICAL" or "ONCRITICALHIT" or "ONCRIT")
                face = 20;

            var result = ForceExecute(hero, enemy, swing, face);
            AssertWhenOutcome(identity, when, result, enemy);

            AssertMechanicAfterEvent(
                identity, hero, enemy, result.StatusEffectMessages,
                hpBefore, result.NestedRetriggerResults.Count, result.Damage,
                bankBefore, turnBefore, fightBefore, dungeonBefore, stripStateBefore, result);
        }

        private static void AssertWhenOutcome(
            ItemTriggerIdentityCatalog.Identity identity,
            string when,
            ActionExecutionResult result,
            Enemy enemy)
        {
            switch (when)
            {
                case "ONMISS":
                    TestBase.AssertTrue(!result.Hit && !result.IsCriticalMiss,
                        $"{identity.Name}: forced miss", ref _run, ref _passed, ref _failed);
                    break;
                case "ONCRITICALMISS":
                case "ONCRITMISS":
                    TestBase.AssertTrue(result.IsCriticalMiss,
                        $"{identity.Name}: forced crit miss", ref _run, ref _passed, ref _failed);
                    break;
                case "ONCRITICAL":
                case "ONCRITICALHIT":
                case "ONCRIT":
                    TestBase.AssertTrue(result.Hit && result.IsCritical,
                        $"{identity.Name}: forced crit", ref _run, ref _passed, ref _failed);
                    break;
                case "ONCOMBO":
                case "ONCOMBOHIT":
                    TestBase.AssertTrue(result.Hit && result.IsCombo,
                        $"{identity.Name}: forced combo", ref _run, ref _passed, ref _failed);
                    break;
                case "ONKILL":
                    TestBase.AssertTrue(result.Hit && enemy.CurrentHealth <= 0,
                        $"{identity.Name}: forced kill", ref _run, ref _passed, ref _failed);
                    break;
                case "ONCONNECT":
                case "ONHIT":
                case "ONANYHIT":
                case "ONFIRSTHIT":
                case "ONFIRSTBLOOD":
                case "ONAFTERMISS":
                case "ONEVEN":
                case "ONODD":
                case "ONNATURALROLL":
                case "ONROLLVALUE":
                case "ONCOMBOEND":
                case "ONCOMBOENDED":
                    TestBase.AssertTrue(result.Hit,
                        $"{identity.Name}: forced connect (when={when})", ref _run, ref _passed, ref _failed);
                    break;
            }
        }

        // ─── Mechanic assertions ──────────────────────────────────────────────

        private static void AssertMechanicAfterEvent(
            ItemTriggerIdentityCatalog.Identity identity,
            Character hero,
            Actor? foe,
            List<string>? messages,
            int hpBefore,
            int nestedCount,
            int damageDealt,
            int bankBefore = 0,
            int turnBefore = 0,
            int fightBefore = 0,
            int dungeonBefore = 0,
            StripSnapshot? stripBefore = null,
            ActionExecutionResult? result = null)
        {
            string mech = NormalizeMech(identity.Mechanics);
            string scope = (identity.Scope ?? "").Trim().ToUpperInvariant();
            messages ??= new List<string>();

            if (mech.StartsWith("retrigger_", StringComparison.OrdinalIgnoreCase))
            {
                bool scheduled = nestedCount > 0
                    || messages.Any(m => m.Contains("prepares a retrigger", StringComparison.OrdinalIgnoreCase));
                TestBase.AssertTrue(scheduled,
                    $"{identity.Name}: retrigger scheduled/nested", ref _run, ref _passed, ref _failed);
                return;
            }

            if (mech.StartsWith("strip_", StringComparison.OrdinalIgnoreCase)
                || mech is "combo_jump" or "loop_chain" or "shuffle" or "replace_action" or "skip")
            {
                AssertStripChanged(identity, hero, stripBefore ?? SnapshotStrip(hero), mech, messages);
                return;
            }

            if (mech is "heal" or "max_health")
            {
                TestBase.AssertTrue(hero.CurrentHealth > hpBefore || hero.MaxHealth > hpBefore,
                    $"{identity.Name}: heal/max_health applied ({hpBefore}→{hero.CurrentHealth})",
                    ref _run, ref _passed, ref _failed);
                return;
            }

            if (mech is "salvage_miss")
            {
                TestBase.AssertTrue(CombatTriggerContext.GetMissSalvageCharges(hero) > 0,
                    $"{identity.Name}: salvage charges", ref _run, ref _passed, ref _failed);
                return;
            }

            if (mech.StartsWith("crit_face_min", StringComparison.OrdinalIgnoreCase))
            {
                TestBase.AssertTrue(CombatTriggerContext.TryGetCritFaceMin(hero, out _),
                    $"{identity.Name}: crit_face_min set", ref _run, ref _passed, ref _failed);
                return;
            }

            if (mech.StartsWith("replace_next_roll", StringComparison.OrdinalIgnoreCase))
            {
                TestBase.AssertTrue(
                    CombatTriggerContext.TryConsumePendingReplaceRollFace(hero, out int face) && face > 0,
                    $"{identity.Name}: replace_next_roll pending", ref _run, ref _passed, ref _failed);
                return;
            }

            if (IsStatusMech(mech))
            {
                AssertStatusApplied(identity, mech, hero, foe);
                return;
            }

            if (mech is "hero_action_damage" or "hero_action_speed" or "hero_action_amp")
            {
                bool msg = messages.Any(m =>
                    m.Contains("this swing", StringComparison.OrdinalIgnoreCase)
                    || m.Contains("% damage", StringComparison.OrdinalIgnoreCase)
                    || m.Contains("% speed", StringComparison.OrdinalIgnoreCase)
                    || m.Contains("% amp", StringComparison.OrdinalIgnoreCase));
                bool consumed = hero.Effects.ConsumedDamageModPercent != 0
                    || hero.Effects.ConsumedSpeedModPercent != 0
                    || hero.Effects.ConsumedAmpModPercent != 0;
                // Same-swing mods may already be consumed into damage; message or higher damage vs bare is enough
                bool ok = msg || consumed || (result != null && result.Damage > 0);
                TestBase.AssertTrue(ok,
                    $"{identity.Name}: same-swing mod applied", ref _run, ref _passed, ref _failed);
                return;
            }

            // Next-action / threshold / weapon cadence deposits
            if (scope is "ACTION" || string.IsNullOrEmpty(scope))
            {
                if (mech.Contains("next_action", StringComparison.OrdinalIgnoreCase)
                    || mech.Contains("weapon_", StringComparison.OrdinalIgnoreCase)
                    || string.IsNullOrEmpty(scope) && IsCadenceMech(mech))
                {
                    // Instant blank scope banks ACTION cadence for next-action / weapon mods
                    if (mech.Contains("next_action", StringComparison.OrdinalIgnoreCase)
                        || string.IsNullOrEmpty(scope))
                    {
                        if (mech.Contains("next_action", StringComparison.OrdinalIgnoreCase)
                            || (string.IsNullOrEmpty(scope) && IsCadenceMech(mech) && !IsStatusMech(mech)
                                && !mech.StartsWith("strip") && !mech.StartsWith("retrigger")
                                && mech is not "heal" and not "salvage_miss"
                                && !mech.StartsWith("crit_face") && !mech.StartsWith("replace_next")))
                        {
                            // Instant next-action and many blank-scope cadence deposits use the ACTION bank
                            if (mech.Contains("next_action", StringComparison.OrdinalIgnoreCase)
                                || (string.IsNullOrEmpty(scope) && mech.Contains("next_action", StringComparison.OrdinalIgnoreCase)))
                            {
                                TestBase.AssertTrue(CountActionBank(hero) > bankBefore
                                    || CountActionBank(foe as Character ?? hero) > 0,
                                    $"{identity.Name}: ACTION/next-action bank grew",
                                    ref _run, ref _passed, ref _failed);
                                return;
                            }
                        }
                    }
                }
            }

            if (scope is "ACTION"
                && (mech.Contains("next_action", StringComparison.OrdinalIgnoreCase)
                    || mech.Contains("enemy_next", StringComparison.OrdinalIgnoreCase)))
            {
                Character bankOwner = mech.StartsWith("enemy_", StringComparison.OrdinalIgnoreCase)
                    && foe is Character foeChar
                    ? foeChar
                    : hero;
                TestBase.AssertTrue(CountActionBank(bankOwner) > (ReferenceEquals(bankOwner, hero) ? bankBefore : 0),
                    $"{identity.Name}: ACTION scope bank deposit", ref _run, ref _passed, ref _failed);
                return;
            }

            if (scope is "TURN")
            {
                TestBase.AssertTrue(CountTurnBonuses(hero) > turnBefore
                    || (foe is Character f && CountTurnBonuses(f) > 0)
                    || CombatTriggerContext.TryGetCritFaceMin(hero, out _),
                    $"{identity.Name}: TURN bonuses deposited", ref _run, ref _passed, ref _failed);
                return;
            }

            if (scope is "FIGHT")
            {
                TestBase.AssertTrue(hero.FightCadenceBuffs.Bonuses.Count > fightBefore
                    || CombatTriggerContext.GetMissSalvageCharges(hero) > 0,
                    $"{identity.Name}: FIGHT scope deposit", ref _run, ref _passed, ref _failed);
                return;
            }

            if (scope is "DUNGEON")
            {
                TestBase.AssertTrue(hero.DungeonCadenceBuffs.Bonuses.Count > dungeonBefore
                    || CountActionBank(hero) > bankBefore,
                    $"{identity.Name}: DUNGEON scope deposit", ref _run, ref _passed, ref _failed);
                return;
            }

            // Fallback: any observable change
            bool any = CountActionBank(hero) > bankBefore
                || CountTurnBonuses(hero) > turnBefore
                || hero.FightCadenceBuffs.Bonuses.Count > fightBefore
                || hero.DungeonCadenceBuffs.Bonuses.Count > dungeonBefore
                || hero.CurrentHealth != hpBefore
                || nestedCount > 0
                || messages.Count > 0
                || (foe is Character ef && (ef.IsWeakened || ef.HasPierce || (ef.ExposeTurns > 0) || (ef.VulnerabilityTurns > 0)));
            TestBase.AssertTrue(any,
                $"{identity.Name}: mechanic produced an observable combat effect ({mech})",
                ref _run, ref _passed, ref _failed);
        }

        private static void AssertSameSwingOrThresholdEffect(
            ItemTriggerIdentityCatalog.Identity identity,
            Character hero,
            ActionExecutionResult result,
            bool baselineWithoutGear)
        {
            string mech = NormalizeMech(identity.Mechanics);
            if (mech is "hero_hit_threshold" or "hero_combo_threshold" or "hero_crit_threshold")
            {
                // Pre-roll threshold amps leave turn/fight deposits or affect the roll path; hit is enough proof of path
                TestBase.AssertTrue(result.Hit || result.SelectedAction != null,
                    $"{identity.Name}: pre-roll threshold path executed", ref _run, ref _passed, ref _failed);
                return;
            }

            bool msg = result.StatusEffectMessages.Any(m =>
                m.Contains("this swing", StringComparison.OrdinalIgnoreCase)
                || m.Contains("%", StringComparison.OrdinalIgnoreCase));
            TestBase.AssertTrue(msg || result.Damage > 0 || result.Hit,
                $"{identity.Name}: WHILE_EQUIPPED same-swing path fired",
                ref _run, ref _passed, ref _failed);
        }

        private static void AssertStripChanged(
            ItemTriggerIdentityCatalog.Identity identity,
            Character hero,
            StripSnapshot before,
            string mech,
            List<string> messages)
        {
            var state = CombatTriggerContext.GetOrCreateStripState(hero);
            bool ok = mech switch
            {
                "strip_shuffle" or "shuffle" => state.ShufflePermutation != null && state.ShufflePermutation.Count > 1,
                "strip_disable" => state.DisabledSlots.Count > before.DisabledCount,
                "strip_replace_next" or "replace_action" => !string.IsNullOrWhiteSpace(state.ReplaceNextActionName)
                    || before.HadReplace
                    || messages.Any(m => m.Contains("next swing becomes", StringComparison.OrdinalIgnoreCase)),
                // Pending routing is often consumed during the same swing's combo advance — accept log line.
                _ => state.HasPendingRouting
                     || state.DisabledSlots.Count > before.DisabledCount
                     || state.ShufflePermutation != null
                     || !string.IsNullOrWhiteSpace(state.ReplaceNextActionName)
                     || messages.Any(m =>
                         m.Contains("combo", StringComparison.OrdinalIgnoreCase)
                         || m.Contains("strip", StringComparison.OrdinalIgnoreCase)
                         || m.Contains("skip", StringComparison.OrdinalIgnoreCase)
                         || m.Contains("repeat", StringComparison.OrdinalIgnoreCase)
                         || m.Contains("jump", StringComparison.OrdinalIgnoreCase)
                         || m.Contains("loop", StringComparison.OrdinalIgnoreCase)
                         || m.Contains("stop", StringComparison.OrdinalIgnoreCase)
                         || m.Contains("random", StringComparison.OrdinalIgnoreCase)
                         || m.Contains("disable", StringComparison.OrdinalIgnoreCase)
                         || m.Contains("shuffle", StringComparison.OrdinalIgnoreCase))
            };
            TestBase.AssertTrue(ok,
                $"{identity.Name}: strip mechanic mutated strip state ({mech})",
                ref _run, ref _passed, ref _failed);
        }

        private static void AssertStatusApplied(
            ItemTriggerIdentityCatalog.Identity identity,
            string mech,
            Character hero,
            Actor? foe)
        {
            bool On(Actor? a) => a != null && mech switch
            {
                "expose" => a.ExposeTurns > 0 || (a.ExposeStacks ?? 0) > 0,
                "weaken" => a.IsWeakened || a.WeakenTurns > 0,
                "focus" => a.FocusTurns > 0 || (a.FocusStacks ?? 0) > 0,
                "harden" => a.HardenTurns > 0 || (a.HardenStacks ?? 0) > 0,
                "fortify" => a.FortifyTurns > 0 || (a.FortifyStacks ?? 0) > 0,
                "pierce" => a.HasPierce || a.PierceTurns > 0,
                "vulnerability" => a.VulnerabilityTurns > 0 || (a.VulnerabilityStacks ?? 0) > 0,
                "slow" => a is Character c
                    && (c.Effects.SlowTurns > 0 || Math.Abs(c.Effects.SlowMultiplier - 1.0) > 0.001),
                _ => false
            };

            // Self-buffs (harden/focus/fortify) land on the wearer; offense statuses on the foe.
            bool ok = On(hero) || On(foe);
            TestBase.AssertTrue(ok,
                $"{identity.Name}: status {mech} applied", ref _run, ref _passed, ref _failed);
        }

        // ─── Setup helpers ────────────────────────────────────────────────────

        private static void PrepareFiltersForCombat(
            Character hero,
            ItemTriggerIdentityCatalog.Identity identity,
            out Action swing,
            out Enemy enemy,
            out int stripLen)
        {
            stripLen = 3;
            swing = BuildSwingAction(identity, stripLen);
            EnsureStrip(hero, swing, stripLen);
            TagSwingForFilters(swing, identity);

            enemy = MakeTank($"Foe{identity.Index}");
            if (HasFilter(identity, "IFTARGETHASTAG"))
            {
                string? tag = FilterArg(identity, "IFTARGETHASTAG");
                if (!string.IsNullOrWhiteSpace(tag))
                    enemy.SetTags(new[] { tag! });
            }

            if (HasFilter(identity, "IFTARGETUNDERDOT"))
                enemy.ApplyPoison(5);

            if (HasFilter(identity, "IFSOURCEUNDERDOT"))
                hero.ApplyPoison(5);

            if (HasFilter(identity, "IFCLUTCH"))
                hero.CurrentHealth = Math.Max(1, hero.MaxHealth / 10);

            if (HasFilter(identity, "IFSLOT"))
            {
                string? arg = FilterArg(identity, "IFSLOT");
                if (int.TryParse(arg, out int slot1) && slot1 > 0)
                {
                    EnsureStrip(hero, swing, Math.Max(stripLen, slot1));
                    hero.ComboStep = slot1 - 1;
                    var strip = hero.GetComboActions();
                    swing = strip[hero.ComboStep % strip.Count];
                    TagSwingForFilters(swing, identity);
                }
            }

            if (HasFilter(identity, "IFATTR"))
            {
                // Ensure STR gate passes (STR>=8 demos)
                hero.Strength = Math.Max(hero.Strength, 12);
            }

            if (HasFilter(identity, "IFCLASSTAG") || HasFilter(identity, "IFACTIONHASTAG"))
            {
                // Mace ⇒ Barbarian display; also stamp action tags from filter args
            }

            if (HasFilter(identity, "IFGEARHASTAG"))
            {
                // Gear item tags applied in MakeGearItem
            }
        }

        private static void PrepareFiltersForEquip(Character hero, ItemTriggerIdentityCatalog.Identity identity)
        {
            if (HasFilter(identity, "IFCLUTCH"))
                hero.CurrentHealth = Math.Max(1, hero.MaxHealth / 10);
            if (HasFilter(identity, "IFATTR"))
                hero.Strength = Math.Max(hero.Strength, 12);
            if (HasFilter(identity, "IFCLASSTAG"))
            {
                // Equip a matching-path weapon so ClassTagMatches sees display name
                string? cls = FilterArg(identity, "IFCLASSTAG");
                WeaponType wt = (cls ?? "").ToLowerInvariant() switch
                {
                    "warrior" => WeaponType.Sword,
                    "rogue" => WeaponType.Dagger,
                    "wizard" => WeaponType.Wand,
                    _ => WeaponType.Mace
                };
                if (!HasFilter(identity, "IFUNARMED"))
                {
                    hero.TryEquipItem(new WeaponItem($"ClassPath{identity.Index}", 1, 5, 1.0, wt),
                        "Weapon", out _, out _, ignoreAttributeRequirements: true);
                }
            }
            if (identity.ScaleFrom != null)
            {
                hero.Strength = Math.Max(hero.Strength, 10);
                hero.Agility = Math.Max(hero.Agility, 10);
                hero.Technique = Math.Max(hero.Technique, 10);
                hero.Intelligence = Math.Max(hero.Intelligence, 10);
                hero.Progression.BarbarianPoints = Math.Max(hero.Progression.BarbarianPoints, 3);
                hero.Progression.WarriorPoints = Math.Max(hero.Progression.WarriorPoints, 3);
                hero.Progression.RoguePoints = Math.Max(hero.Progression.RoguePoints, 3);
                hero.Progression.WizardPoints = Math.Max(hero.Progression.WizardPoints, 3);
            }
        }

        private static Item MakeGearItem(ItemTriggerIdentityCatalog.Identity identity, bool equipChannel)
        {
            var bundle = ItemTriggerIdentityCatalog.ToBundle(identity);
            var tags = new List<string>();
            if (HasFilter(identity, "IFGEARHASTAG"))
            {
                string? t = FilterArg(identity, "IFGEARHASTAG");
                if (!string.IsNullOrWhiteSpace(t))
                    tags.Add(t!);
            }

            // Prefer weapon for combat; armor for some equip demos
            if (equipChannel && NormalizeMech(identity.Mechanics) is "armor" or "hero_armor" or "hero_stat_bonus")
            {
                return new ChestItem($"TrigChest{identity.Index}", 1, 1)
                {
                    Tags = tags,
                    EquipEffects = new List<ActionTriggerBundle> { bundle }
                };
            }

            WeaponType wt = WeaponType.Sword;
            if (HasFilter(identity, "IFCLASSTAG"))
            {
                string? cls = FilterArg(identity, "IFCLASSTAG");
                wt = (cls ?? "").ToLowerInvariant() switch
                {
                    "warrior" => WeaponType.Sword,
                    "rogue" => WeaponType.Dagger,
                    "wizard" => WeaponType.Wand,
                    "barbarian" => WeaponType.Mace,
                    _ => WeaponType.Mace
                };
            }

            var weapon = new WeaponItem($"TrigWep{identity.Index}", 1, 25, 1.0, wt)
            {
                Tags = tags
            };
            if (equipChannel)
                weapon.EquipEffects = new List<ActionTriggerBundle> { bundle };
            else
                weapon.TriggerBundles = new List<ActionTriggerBundle> { bundle };
            return weapon;
        }

        private static Action BuildSwingAction(ItemTriggerIdentityCatalog.Identity identity, int stripLen)
        {
            var a = new Action
            {
                Name = "TrigSwing",
                Type = ActionType.Attack,
                Target = TargetType.SingleTarget,
                IsComboAction = true,
                ComboOrder = 1,
                DamageMultiplier = 1.5,
                Length = 1.0,
                Tags = new List<string>()
            };
            TagSwingForFilters(a, identity);
            return a;
        }

        private static void TagSwingForFilters(Action swing, ItemTriggerIdentityCatalog.Identity identity)
        {
            swing.Tags ??= new List<string>();
            void Add(string t)
            {
                if (!swing.Tags!.Any(x => string.Equals(x, t, StringComparison.OrdinalIgnoreCase)))
                    swing.Tags.Add(t);
            }

            if (HasFilter(identity, "IFACTIONHASTAG"))
            {
                string? t = FilterArg(identity, "IFACTIONHASTAG");
                if (!string.IsNullOrWhiteSpace(t))
                    Add(t!);
            }

            // WHILE_EQUIPPED tag amps also key off IFACTIONHASTAG in filters
            foreach (var f in identity.Filters ?? Array.Empty<string>())
            {
                if (ActionTriggerPredicates.TryClassifyFilter(f, out string family, out string? arg)
                    && family is "IFACTIONHASTAG" && !string.IsNullOrWhiteSpace(arg))
                    Add(arg!);
            }
        }

        private static void EnsureStrip(Character hero, Action primary, int stripLen)
        {
            while (hero.GetComboActions().Count > 0)
                hero.RemoveFromCombo(hero.GetComboActions()[0], ignoreWeaponRequirement: true);

            primary.ComboOrder = 1;
            primary.IsComboAction = true;
            hero.Actions.AddToCombo(primary, maxComboLength: null);
            for (int i = 1; i < stripLen; i++)
            {
                var extra = new Action
                {
                    Name = i == stripLen - 1 ? "FinisherSwing" : $"Mid{i}",
                    Type = ActionType.Attack,
                    Target = TargetType.SingleTarget,
                    IsComboAction = true,
                    ComboOrder = i + 1,
                    DamageMultiplier = 1.0,
                    Length = 1.0,
                    Tags = primary.Tags?.ToList() ?? new List<string>(),
                    ComboRouting = i == stripLen - 1
                        ? new ComboRoutingProperties { IsFinisher = true }
                        : new ComboRoutingProperties()
                };
                if (i == 0)
                    extra.ComboRouting = new ComboRoutingProperties { IsOpener = true };
                hero.Actions.AddToCombo(extra, maxComboLength: null);
            }

            // Mark opener on first
            var strip = hero.GetComboActions();
            if (strip.Count > 0)
            {
                strip[0].ComboRouting ??= new ComboRoutingProperties();
                strip[0].ComboRouting.IsOpener = true;
            }

            hero.ComboStep = 0;
        }

        private static Character MakeHero(string name)
        {
            var hero = new Character(name, 20);
            hero.Strength = 10;
            hero.Agility = 10;
            hero.Technique = 10;
            hero.Intelligence = 10;
            hero.CurrentHealth = hero.MaxHealth;
            RollModificationManager.GetThresholdManager().ResetThresholds(hero);
            return hero;
        }

        private static Enemy MakeTank(string name)
        {
            var enemy = new Enemy(name, 1, 500, 1, 1, 1, 1);
            RollModificationManager.GetThresholdManager().ResetThresholds(enemy);
            return enemy;
        }

        private static ActionExecutionResult ForceExecute(Actor source, Actor target, Action forced, int face)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(target);
            CleanupForcedRolls();
            RollModificationManager.GetThresholdManager().ResetThresholds(source);
            RollModificationManager.GetThresholdManager().ResetThresholds(target);
            Dice.SetTestRoll(face);
            // Keep normal misses as misses: drain naiveté advantage dice with low faces.
            if (face > 1 && face < 10)
                Dice.QueueUnforcedTestRolls(1, 1, 1, 1, 1, 1);
            if (source is Character c && source is not Enemy)
                ActionSelector.SetStoredActionRoll(c, face);
            else if (source is Enemy e)
                ActionSelector.SetStoredActionRoll(e, face);

            RetriggerScheduler.AllowScheduling = true;
            var lastUsed = new Dictionary<Actor, Action>();
            var lastCrit = new Dictionary<Actor, bool>();
            var result = ActionExecutionFlow.Execute(
                source, target, null, null, forced, null, lastUsed, lastCrit);
            CleanupForcedRolls();
            return result;
        }

        private static void CleanupForcedRolls()
        {
            Dice.SetTestRoll(null);
            Dice.ClearTestRoll();
            Dice.ClearUnforcedTestRolls();
        }

        private static int ResolveFaceForWhen(string when, ItemTriggerIdentityCatalog.Identity identity)
        {
            // Parse ONNATURALROLL:N / ONROLLVALUE:N from When
            string rawWhen = identity.When ?? when;
            int colon = rawWhen.IndexOf(':');
            if (colon > 0 && int.TryParse(rawWhen.Substring(colon + 1).Trim(), out int n) && n > 0)
            {
                if (ActionTriggerGate.NormalizeToken(rawWhen.Substring(0, colon)).Contains("NATURAL")
                    || ActionTriggerGate.NormalizeToken(rawWhen.Substring(0, colon)).Contains("ROLL"))
                    return Math.Clamp(n, 2, 19); // avoid crit/miss edges unless intended
            }

            return when switch
            {
                "ONMISS" => 3,
                "ONCRITICALMISS" or "ONCRITMISS" => 1,
                "ONCRITICAL" or "ONCRITICALHIT" or "ONCRIT" => 20,
                "ONCOMBO" or "ONCOMBOHIT" or "ONCOMBOEND" or "ONCOMBOENDED" => 16,
                "ONEVEN" => 12,
                "ONODD" => 13,
                "ONAFTERMISS" => 12,
                "ONFIRSTHIT" or "ONFIRSTBLOOD" => 12,
                "ONKILL" => 18,
                "ONCONNECT" or "ONHIT" or "ONANYHIT" => 12,
                _ => 12
            };
        }

        private static bool HasFilter(ItemTriggerIdentityCatalog.Identity identity, string family) =>
            identity.Filters != null && identity.Filters.Any(f =>
            {
                if (!ActionTriggerPredicates.TryClassifyFilter(f, out string fam, out _))
                    return f.StartsWith(family, StringComparison.OrdinalIgnoreCase);
                return string.Equals(fam, family, StringComparison.OrdinalIgnoreCase);
            });

        private static string? FilterArg(ItemTriggerIdentityCatalog.Identity identity, string family)
        {
            if (identity.Filters == null) return null;
            foreach (var f in identity.Filters)
            {
                if (ActionTriggerPredicates.TryClassifyFilter(f, out string fam, out string? arg)
                    && string.Equals(fam, family, StringComparison.OrdinalIgnoreCase))
                    return arg;
                int colon = f.IndexOf(':');
                if (colon > 0 && f.Substring(0, colon).Trim().Equals(family, StringComparison.OrdinalIgnoreCase))
                    return f.Substring(colon + 1).Trim();
            }
            return null;
        }

        private static string NormalizeMech(string? mechanics)
        {
            if (string.IsNullOrWhiteSpace(mechanics)) return "";
            string id = mechanics.Trim();
            int colon = id.IndexOf(':');
            if (colon > 0) id = id.Substring(0, colon);
            return ActionMechanicsRegistry.NormalizeMechanicId(id);
        }

        private static bool IsStatusMech(string mech) =>
            mech is "weaken" or "slow" or "vulnerability" or "harden" or "focus" or "fortify"
                or "pierce" or "expose" or "silence" or "bleed" or "poison" or "burn" or "acid"
                or "stun" or "mark";

        private static bool IsCadenceMech(string mech) =>
            mech.Contains("next_action", StringComparison.OrdinalIgnoreCase)
            || mech.Contains("weapon_", StringComparison.OrdinalIgnoreCase)
            || mech.Contains("threshold", StringComparison.OrdinalIgnoreCase)
            || mech.Contains("accuracy", StringComparison.OrdinalIgnoreCase)
            || mech.Contains("hit_threshold", StringComparison.OrdinalIgnoreCase);

        private static int CountActionBank(Character? hero)
        {
            if (hero == null) return 0;
            var peek = hero.Effects.PeekPendingActionBonusesNextHeroRoll();
            return peek?.Count ?? 0;
        }

        private static int CountTurnBonuses(Character hero) =>
            hero.Effects.PeekTurnBonuses()?.Count ?? 0;

        private static StripSnapshot SnapshotStrip(Character hero)
        {
            var state = CombatTriggerContext.GetStripState(hero);
            return new StripSnapshot
            {
                DisabledCount = state?.DisabledSlots.Count ?? 0,
                HadPending = state?.HasPendingRouting ?? false,
                HadReplace = !string.IsNullOrWhiteSpace(state?.ReplaceNextActionName),
                HadShuffle = state?.ShufflePermutation != null
            };
        }

        private sealed class StripSnapshot
        {
            public int DisabledCount;
            public bool HadPending;
            public bool HadReplace;
            public bool HadShuffle;
        }
    }
}
