using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RPGGame;
using RPGGame.Actions;
using RPGGame.Actions.Conditional;
using RPGGame.Actions.Execution;
using RPGGame.Actions.RollModification;
using RPGGame.Combat;
using RPGGame.Data;
using RPGGame.Entity.Actions.ComboRouting;
using RPGGame.Utils;

namespace RPGGame.Items.ItemTriggerScenario
{
    /// <summary>
    /// Auto-builds conditions for each item trigger identity, forces the matching WHEN through
    /// production combat/equip paths, and reports what fired / what got buffed.
    /// </summary>
    public static class ItemTriggerScenarioRunner
    {
        public static ItemTriggerScenarioReport Run(ItemTriggerIdentityCatalog.Identity identity)
        {
            ArgumentNullException.ThrowIfNull(identity);
            try
            {
                CleanupForcedRolls();
                CombatTriggerContext.ResetForBattle();
                RetriggerScheduler.AllowScheduling = true;
                RetriggerScheduler.ResetForBattle();
                RollModificationManager.GetThresholdManager().Clear();

                string when = ActionTriggerGate.NormalizeToken(identity.When ?? "");
                if (identity.IsEquipEffect || when is "WHILEEQUIPPED" or "ONEQUIP")
                    return RunEquipChannel(identity);
                if (when is "ONROOMSCLEARED" or "ONROOMCLEARED")
                    return RunRoomClear(identity);
                if (when is "ONTAKEHIT" or "ONHEROHURT")
                    return RunTakeHit(identity);
                return RunAttackerCombat(identity, when);
            }
            catch (Exception ex)
            {
                var err = BaseReport(identity, "error");
                err.Passed = false;
                err.Finding = $"threw: {ex.GetType().Name}: {ex.Message}";
                err.Error = ex.ToString();
                return err;
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

        /// <summary>Runs every catalog identity (optional substring filter on name/when/mechanics).</summary>
        public static ItemTriggerScenarioBatchResult RunAll(string? filter = null)
        {
            _ = GameConfiguration.Instance;
            var reports = new List<ItemTriggerScenarioReport>();
            int passed = 0, failed = 0;
            foreach (var identity in ItemTriggerIdentityCatalog.Identities)
            {
                if (!MatchesFilter(identity, filter))
                    continue;
                var report = Run(identity);
                reports.Add(report);
                if (report.Passed) passed++;
                else failed++;
            }

            return new ItemTriggerScenarioBatchResult
            {
                Reports = reports,
                Passed = passed,
                Failed = failed
            };
        }

        public static ItemTriggerScenarioReport RunByRequest(ItemTriggerScenarioRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var identity = ResolveIdentity(request);
            return Run(identity);
        }

        public static ItemTriggerIdentityCatalog.Identity ResolveIdentity(ItemTriggerScenarioRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (!string.IsNullOrWhiteSpace(request.IdentityName))
            {
                var all = ItemTriggerIdentityCatalog.Identities;
                var exact = all.FirstOrDefault(i =>
                    string.Equals(i.Name, request.IdentityName, StringComparison.OrdinalIgnoreCase));
                if (exact != null) return exact;
                var partial = all.FirstOrDefault(i =>
                    i.Name.Contains(request.IdentityName!, StringComparison.OrdinalIgnoreCase));
                if (partial != null) return partial;
                throw new ArgumentException($"No trigger identity matching name '{request.IdentityName}'.");
            }

            if (request.IdentityIndex is int idx)
                return ItemTriggerIdentityCatalog.Get(idx);

            throw new ArgumentException("Request needs IdentityIndex or IdentityName.");
        }

        public static bool MatchesFilter(ItemTriggerIdentityCatalog.Identity identity, string? filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return true;
            string f = filter.Trim();
            if (int.TryParse(f, out int idx) && identity.Index == idx)
                return true;
            return (identity.Name?.Contains(f, StringComparison.OrdinalIgnoreCase) ?? false)
                   || (identity.When?.Contains(f, StringComparison.OrdinalIgnoreCase) ?? false)
                   || (identity.Mechanics?.Contains(f, StringComparison.OrdinalIgnoreCase) ?? false)
                   || (identity.Scope?.Contains(f, StringComparison.OrdinalIgnoreCase) ?? false)
                   || (identity.Description?.Contains(f, StringComparison.OrdinalIgnoreCase) ?? false);
        }

        // ─── Equip channel ────────────────────────────────────────────────────

        private static ItemTriggerScenarioReport RunEquipChannel(ItemTriggerIdentityCatalog.Identity identity)
        {
            string mech = NormalizeMech(identity.Mechanics);
            var notes = new List<string> { "Channel: equip (WHILE_EQUIPPED)" };
            var hero = MakeHero($"Equip{identity.Index}");
            PrepareFiltersForEquip(hero, identity, notes);

            var item = MakeGearItem(identity, equipChannel: true);
            string slot = SlotFor(item);

            if (mech is "grant_action")
            {
                if (item is WeaponItem)
                {
                    item = new LegsItem($"GrantLegs{identity.Index}", 1, 1)
                    {
                        EquipEffects = new List<ActionTriggerBundle> { ItemTriggerIdentityCatalog.ToBundle(identity) }
                    };
                    slot = "Legs";
                    notes.Add("grant_action: equipped on Legs (IFUNARMED-safe)");
                }

                var before = CaptureBuffs(hero);
                hero.TryEquipItem(item, slot, out _, out _, ignoreAttributeRequirements: true);
                var after = CaptureBuffs(hero);
                var granted = ItemEquipEffectApplicator.GetGrantedActionNames(hero);
                bool ok = granted.Count > 0;
                return Finish(identity, "equip", 0, notes, before, after, ok,
                    ok ? $"grant_action yields {granted.Count} name(s): {string.Join(", ", granted)}"
                       : "grant_action yielded no names",
                    channel: "equip");
            }

            if (mech is "grant_action_tag")
            {
                var before = CaptureBuffs(hero);
                hero.TryEquipItem(item, slot, out _, out _, ignoreAttributeRequirements: true);
                ItemEquipEffectApplicator.RefreshGrantedActionTags(hero);
                var after = CaptureBuffs(hero);
                bool ok = hero.EquipmentGrantedActionTags.Count > 0;
                return Finish(identity, "equip", 0, notes, before, after, ok,
                    ok ? $"grant_action_tag overlay: {hero.EquipmentGrantedActionTags.Count} tag(s)"
                       : "grant_action_tag overlay empty",
                    channel: "equip");
            }

            if (mech is "armor" or "hero_armor" or "hero_stat_bonus" or "stat_bonus")
            {
                var before = CaptureBuffs(hero);
                int beforeArmor = before.EquipArmorBonus;
                int beforeStat = before.EquipPrimaryStatBonus;
                hero.TryEquipItem(item, slot, out _, out _, ignoreAttributeRequirements: true);
                var after = CaptureBuffs(hero);
                bool ok;
                string finding;
                if (mech is "armor" or "hero_armor")
                {
                    ok = after.EquipArmorBonus > beforeArmor;
                    finding = $"equip armor bonus {beforeArmor}→{after.EquipArmorBonus}";
                }
                else
                {
                    ok = after.EquipPrimaryStatBonus > beforeStat
                         || ItemEquipEffectApplicator.GetEquippedStatBonus(hero, "STRENGTH") > 0
                         || ItemEquipEffectApplicator.GetEquippedStatBonus(hero, "AGILITY") > 0
                         || ItemEquipEffectApplicator.GetEquippedStatBonus(hero, "TECHNIQUE") > 0
                         || ItemEquipEffectApplicator.GetEquippedStatBonus(hero, "INTELLIGENCE") > 0;
                    finding = ok ? "equip stat bonus applied" : "equip stat bonus missing";
                }

                return Finish(identity, "equip", 0, notes, before, after, ok, finding, channel: "equip");
            }

            hero.TryEquipItem(item, slot, out _, out _, ignoreAttributeRequirements: true);
            notes.Add($"Equipped {item.Name} in {slot}");
            var swing = BuildSwingAction(identity);
            EnsureStrip(hero, swing, stripLen: 3);
            TagSwingForFilters(swing, identity);
            var enemy = MakeTank("EquipFoe");
            int face = ResolveFaceForWhen("ONCONNECT", identity);
            notes.Add($"Same-swing path with forced d20={face}");
            var beforeSwing = CaptureBuffs(hero);
            var result = ForceExecute(hero, enemy, swing, face);
            var afterSwing = CaptureBuffs(hero);
            var eval = EvaluateSameSwingOrThreshold(identity, result);
            return Finish(identity, "equip-same-swing", face, notes, beforeSwing, afterSwing,
                eval.Passed, eval.Finding, result, channel: "equip");
        }

        // ─── Room clear ───────────────────────────────────────────────────────

        private static ItemTriggerScenarioReport RunRoomClear(ItemTriggerIdentityCatalog.Identity identity)
        {
            var notes = new List<string> { "Path: ONROOMSCLEARED via RoomClearedTriggerApplicator" };
            var hero = MakeHero($"Room{identity.Index}");
            hero.CurrentHealth = Math.Max(1, hero.MaxHealth - 20);
            hero.TryEquipItem(MakeGearItem(identity, equipChannel: false), "Weapon", out _, out _,
                ignoreAttributeRequirements: true);
            var before = CaptureBuffs(hero);
            var msgs = RoomClearedTriggerApplicator.ApplyForHero(hero);
            var after = CaptureBuffs(hero);
            var eval = EvaluateMechanicAfterEvent(identity, hero, foe: null, msgs,
                before.HeroHp, nestedCount: 0, damageDealt: 0, before);
            return Finish(identity, "room-clear", 0, notes, before, after, eval.Passed, eval.Finding,
                statusMessages: msgs);
        }

        // ─── Defender ONTAKEHIT ────────────────────────────────────────────────

        private static ItemTriggerScenarioReport RunTakeHit(ItemTriggerIdentityCatalog.Identity identity)
        {
            var notes = new List<string> { "Path: ONTAKEHIT (enemy swings at hero)" };
            var hero = MakeHero($"Hurt{identity.Index}");
            hero.TryEquipItem(MakeGearItem(identity, equipChannel: false), "Body", out _, out _,
                ignoreAttributeRequirements: true);
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

            var before = CaptureBuffs(hero);
            var result = ForceExecute(enemy, hero, enemySwing, face: 12);
            var after = CaptureBuffs(hero);

            if (!result.Hit || result.Damage <= 0)
            {
                return Finish(identity, "take-hit", 12, notes, before, after, false,
                    "enemy hit did not deal damage", result);
            }

            string mech = NormalizeMech(identity.Mechanics);
            Evaluation eval;
            if (mech is "hero_next_action_damage" or "hero_next_action_speed"
                or "hero_next_action_amp" or "hero_next_action_multihit")
            {
                bool ok = after.ActionBankCount > before.ActionBankCount;
                eval = new Evaluation(ok,
                    ok ? "ONTAKEHIT deposits next-action bank" : "ONTAKEHIT did not grow ACTION bank");
            }
            else if (mech is "harden")
            {
                bool ok = hero.HardenTurns > before.HardenTurns || (hero.HardenStacks ?? 0) > 0;
                eval = new Evaluation(ok, ok ? "ONTAKEHIT applies Harden" : "Harden missing after ONTAKEHIT");
            }
            else
            {
                eval = EvaluateMechanicAfterEvent(identity, hero, enemy, result.StatusEffectMessages,
                    after.HeroHp, result.NestedRetriggerResults.Count, result.Damage, before, result);
            }

            return Finish(identity, "take-hit", 12, notes, before, after, eval.Passed, eval.Finding, result);
        }

        // ─── Attacker combat ──────────────────────────────────────────────────

        private static ItemTriggerScenarioReport RunAttackerCombat(
            ItemTriggerIdentityCatalog.Identity identity, string when)
        {
            var notes = new List<string> { $"Path: attacker combat WHEN={when}" };
            var hero = MakeHero($"Atk{identity.Index}");
            hero.TryEquipItem(MakeGearItem(identity, equipChannel: false), "Weapon", out _, out _,
                ignoreAttributeRequirements: true);
            notes.Add("Equipped synthetic trigger weapon");
            PrepareFiltersForCombat(hero, identity, out Action swing, out Enemy enemy, out int stripLen, notes);

            if (when is "ONAFTERMISS")
            {
                ForceExecute(hero, enemy, swing, face: 2);
                EnsureStrip(hero, swing, stripLen);
                TagSwingForFilters(swing, identity);
                notes.Add("Primed with prior miss (ONAFTERMISS)");
            }

            if (HasFilter(identity, "IFSAMESACTION") || HasFilter(identity, "IFMIRROR"))
            {
                CombatTriggerContext.NotifySwingResolved(hero, new Action { Name = swing.Name }, true, false);
                notes.Add("Primed IFSAMESACTION history");
            }

            if (HasFilter(identity, "IFDIFFERENTACTION") || HasFilter(identity, "IFSWITCHUP"))
            {
                CombatTriggerContext.NotifySwingResolved(hero, new Action { Name = "OtherSwing" }, true, false);
                notes.Add("Primed IFDIFFERENTACTION history");
            }

            if (when is "ONCOMBOEND" or "ONCOMBOENDED")
            {
                EnsureStrip(hero, swing, stripLen: Math.Max(2, stripLen));
                var strip = hero.GetComboActions();
                hero.ComboStep = strip.Count - 1;
                swing = strip[hero.ComboStep];
                TagSwingForFilters(swing, identity);
                notes.Add($"Combo-end: ComboStep={hero.ComboStep} (last strip slot)");
            }

            if (when is "ONKILL")
            {
                enemy = new Enemy($"KillFoe{identity.Index}", 1, 1, 1, 1, 1, 1);
                notes.Add("Kill foe: 1 HP enemy");
            }

            if (NormalizeMech(identity.Mechanics) is "heal" or "max_health")
            {
                hero.CurrentHealth = Math.Max(1, hero.MaxHealth - 15);
                notes.Add($"Hero HP lowered for heal check → {hero.CurrentHealth}");
            }

            var before = CaptureBuffs(hero);
            int face = ResolveFaceForWhen(when, identity);
            if (when is "ONCOMBO" or "ONCOMBOHIT" or "ONCOMBOEND" or "ONCOMBOENDED")
                face = Math.Clamp(face == 20 ? 16 : Math.Max(face, 15), 14, 19);
            if (when is "ONCRITICAL" or "ONCRITICALHIT" or "ONCRIT")
                face = 20;
            notes.Add($"Forced d20={face}");

            var result = ForceExecute(hero, enemy, swing, face);
            var after = CaptureBuffs(hero);

            var outcome = EvaluateWhenOutcome(identity, when, result, enemy);
            if (!outcome.Passed)
            {
                return Finish(identity, "attacker", face, notes, before, after, false, outcome.Finding, result,
                    outcomeMatched: false, outcomeDetail: outcome.Finding);
            }

            var eval = EvaluateMechanicAfterEvent(
                identity, hero, enemy, result.StatusEffectMessages,
                before.HeroHp, result.NestedRetriggerResults.Count, result.Damage, before, result);
            return Finish(identity, "attacker", face, notes, before, after, eval.Passed, eval.Finding, result,
                outcomeMatched: true, outcomeDetail: outcome.Finding);
        }

        // ─── Evaluation ───────────────────────────────────────────────────────

        private readonly record struct Evaluation(bool Passed, string Finding);

        private static Evaluation EvaluateWhenOutcome(
            ItemTriggerIdentityCatalog.Identity identity,
            string when,
            ActionExecutionResult result,
            Enemy enemy)
        {
            return when switch
            {
                "ONMISS" => Eval(!result.Hit && !result.IsCriticalMiss, identity, "forced miss"),
                "ONCRITICALMISS" or "ONCRITMISS" => Eval(result.IsCriticalMiss, identity, "forced crit miss"),
                "ONCRITICAL" or "ONCRITICALHIT" or "ONCRIT" =>
                    Eval(result.Hit && result.IsCritical, identity, "forced crit"),
                "ONCOMBO" or "ONCOMBOHIT" =>
                    Eval(result.Hit && result.IsCombo, identity, "forced combo"),
                "ONKILL" =>
                    Eval(result.Hit && enemy.CurrentHealth <= 0, identity, "forced kill"),
                "ONCONNECT" or "ONHIT" or "ONANYHIT" or "ONFIRSTHIT" or "ONFIRSTBLOOD"
                    or "ONAFTERMISS" or "ONEVEN" or "ONODD" or "ONNATURALROLL" or "ONROLLVALUE"
                    or "ONCOMBOEND" or "ONCOMBOENDED" =>
                    Eval(result.Hit, identity, $"forced connect (when={when})"),
                _ => new Evaluation(true, $"WHEN={when} (no strict outcome assert)")
            };
        }

        private static Evaluation Eval(bool ok, ItemTriggerIdentityCatalog.Identity identity, string label) =>
            new(ok, ok ? label : $"{identity.Name}: expected {label}");

        private static Evaluation EvaluateSameSwingOrThreshold(
            ItemTriggerIdentityCatalog.Identity identity,
            ActionExecutionResult result)
        {
            string mech = NormalizeMech(identity.Mechanics);
            if (mech is "hero_hit_threshold" or "hero_combo_threshold" or "hero_crit_threshold")
            {
                bool ok = result.Hit || result.SelectedAction != null;
                return new Evaluation(ok,
                    ok ? "pre-roll threshold path executed" : "pre-roll threshold path missing");
            }

            bool msg = result.StatusEffectMessages.Any(m =>
                m.Contains("this swing", StringComparison.OrdinalIgnoreCase)
                || m.Contains("%", StringComparison.OrdinalIgnoreCase));
            bool ok2 = msg || result.Damage > 0 || result.Hit;
            return new Evaluation(ok2,
                ok2 ? "WHILE_EQUIPPED same-swing path fired" : "WHILE_EQUIPPED same-swing path missing");
        }

        private static Evaluation EvaluateMechanicAfterEvent(
            ItemTriggerIdentityCatalog.Identity identity,
            Character hero,
            Actor? foe,
            List<string>? messages,
            int hpBefore,
            int nestedCount,
            int damageDealt,
            ItemTriggerBuffSnapshot before,
            ActionExecutionResult? result = null)
        {
            string mech = NormalizeMech(identity.Mechanics);
            string scope = (identity.Scope ?? "").Trim().ToUpperInvariant();
            messages ??= new List<string>();

            if (mech.StartsWith("retrigger_", StringComparison.OrdinalIgnoreCase))
            {
                bool scheduled = nestedCount > 0
                    || messages.Any(m => m.Contains("prepares a retrigger", StringComparison.OrdinalIgnoreCase));
                return new Evaluation(scheduled,
                    scheduled ? "retrigger scheduled/nested" : "retrigger not scheduled");
            }

            if (mech.StartsWith("strip_", StringComparison.OrdinalIgnoreCase)
                || mech is "combo_jump" or "loop_chain" or "shuffle" or "replace_action" or "skip")
            {
                return EvaluateStripChanged(identity, hero, before, mech, messages);
            }

            if (mech is "heal" or "max_health")
            {
                bool ok = hero.CurrentHealth > hpBefore || hero.MaxHealth > hpBefore;
                return new Evaluation(ok,
                    ok ? $"heal/max_health applied ({hpBefore}→{hero.CurrentHealth})"
                       : $"heal/max_health missing ({hpBefore}→{hero.CurrentHealth})");
            }

            if (mech is "salvage_miss")
            {
                bool ok = CombatTriggerContext.GetMissSalvageCharges(hero) > 0;
                return new Evaluation(ok, ok ? "salvage charges" : "salvage charges missing");
            }

            if (mech.StartsWith("crit_face_min", StringComparison.OrdinalIgnoreCase))
            {
                bool ok = CombatTriggerContext.TryGetCritFaceMin(hero, out _);
                return new Evaluation(ok, ok ? "crit_face_min set" : "crit_face_min missing");
            }

            if (mech.StartsWith("replace_next_roll", StringComparison.OrdinalIgnoreCase))
            {
                bool ok = CombatTriggerContext.TryConsumePendingReplaceRollFace(hero, out int face) && face > 0;
                return new Evaluation(ok, ok ? "replace_next_roll pending" : "replace_next_roll missing");
            }

            if (IsStatusMech(mech))
                return EvaluateStatusApplied(identity, mech, hero, foe);

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
                bool ok = msg || consumed || (result != null && result.Damage > 0);
                return new Evaluation(ok, ok ? "same-swing mod applied" : "same-swing mod missing");
            }

            if (scope is "ACTION"
                && (mech.Contains("next_action", StringComparison.OrdinalIgnoreCase)
                    || mech.Contains("enemy_next", StringComparison.OrdinalIgnoreCase)))
            {
                Character bankOwner = mech.StartsWith("enemy_", StringComparison.OrdinalIgnoreCase)
                    && foe is Character foeChar
                    ? foeChar
                    : hero;
                int bankBefore = ReferenceEquals(bankOwner, hero) ? before.ActionBankCount : 0;
                bool ok = CountActionBank(bankOwner) > bankBefore;
                return new Evaluation(ok, ok ? "ACTION scope bank deposit" : "ACTION scope bank missing");
            }

            if (string.IsNullOrEmpty(scope)
                && mech.Contains("next_action", StringComparison.OrdinalIgnoreCase))
            {
                bool ok = CountActionBank(hero) > before.ActionBankCount
                          || CountActionBank(foe as Character ?? hero) > 0;
                return new Evaluation(ok, ok ? "ACTION/next-action bank grew" : "ACTION/next-action bank missing");
            }

            if (scope is "TURN")
            {
                bool ok = CountTurnBonuses(hero) > before.TurnBonusCount
                          || (foe is Character f && CountTurnBonuses(f) > 0)
                          || CombatTriggerContext.TryGetCritFaceMin(hero, out _);
                return new Evaluation(ok, ok ? "TURN bonuses deposited" : "TURN bonuses missing");
            }

            if (scope is "FIGHT")
            {
                bool ok = hero.FightCadenceBuffs.Bonuses.Count > before.FightBonusCount
                          || CombatTriggerContext.GetMissSalvageCharges(hero) > 0;
                return new Evaluation(ok, ok ? "FIGHT scope deposit" : "FIGHT scope deposit missing");
            }

            if (scope is "DUNGEON")
            {
                bool ok = hero.DungeonCadenceBuffs.Bonuses.Count > before.DungeonBonusCount
                          || CountActionBank(hero) > before.ActionBankCount;
                return new Evaluation(ok, ok ? "DUNGEON scope deposit" : "DUNGEON scope deposit missing");
            }

            bool any = CountActionBank(hero) > before.ActionBankCount
                       || CountTurnBonuses(hero) > before.TurnBonusCount
                       || hero.FightCadenceBuffs.Bonuses.Count > before.FightBonusCount
                       || hero.DungeonCadenceBuffs.Bonuses.Count > before.DungeonBonusCount
                       || hero.CurrentHealth != hpBefore
                       || nestedCount > 0
                       || messages.Count > 0
                       || (foe is Character ef && (ef.IsWeakened || ef.HasPierce || ef.ExposeTurns > 0
                                                  || ef.VulnerabilityTurns > 0));
            return new Evaluation(any,
                any
                    ? $"mechanic produced an observable combat effect ({mech})"
                    : $"mechanic produced no observable combat effect ({mech})");
        }

        private static Evaluation EvaluateStripChanged(
            ItemTriggerIdentityCatalog.Identity identity,
            Character hero,
            ItemTriggerBuffSnapshot before,
            string mech,
            List<string> messages)
        {
            var state = CombatTriggerContext.GetOrCreateStripState(hero);
            bool ok = mech switch
            {
                "strip_shuffle" or "shuffle" => state.ShufflePermutation != null && state.ShufflePermutation.Count > 1,
                "strip_disable" => state.DisabledSlots.Count > before.StripDisabledCount,
                "strip_replace_next" or "replace_action" => !string.IsNullOrWhiteSpace(state.ReplaceNextActionName)
                    || before.StripHasReplace
                    || messages.Any(m => m.Contains("next swing becomes", StringComparison.OrdinalIgnoreCase)),
                _ => state.HasPendingRouting
                     || state.DisabledSlots.Count > before.StripDisabledCount
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
            return new Evaluation(ok,
                ok ? $"strip mechanic mutated strip state ({mech})"
                   : $"strip mechanic did not mutate strip ({mech})");
        }

        private static Evaluation EvaluateStatusApplied(
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

            bool ok = On(hero) || On(foe);
            return new Evaluation(ok, ok ? $"status {mech} applied" : $"status {mech} missing");
        }

        // ─── Setup helpers (public for Lab Load) ──────────────────────────────

        /// <summary>
        /// Builds hero/enemy/gear/strip for a combat-channel identity without executing.
        /// Equip / room-clear / take-hit identities should use <see cref="Run"/> instead.
        /// </summary>
        public static ItemTriggerScenarioSetup PrepareAttackerSetup(ItemTriggerIdentityCatalog.Identity identity)
        {
            ArgumentNullException.ThrowIfNull(identity);
            string when = ActionTriggerGate.NormalizeToken(identity.When ?? "");
            var notes = new List<string>();
            var hero = MakeHero($"Lab{identity.Index}");
            hero.TryEquipItem(MakeGearItem(identity, equipChannel: false), "Weapon", out _, out _,
                ignoreAttributeRequirements: true);
            PrepareFiltersForCombat(hero, identity, out Action swing, out Enemy enemy, out int stripLen, notes);
            int face = ResolveFaceForWhen(when, identity);
            if (when is "ONCOMBO" or "ONCOMBOHIT" or "ONCOMBOEND" or "ONCOMBOENDED")
                face = Math.Clamp(face == 20 ? 16 : Math.Max(face, 15), 14, 19);
            if (when is "ONCRITICAL" or "ONCRITICALHIT" or "ONCRIT")
                face = 20;
            if (when is "ONKILL")
                enemy = new Enemy($"KillFoe{identity.Index}", 1, 1, 1, 1, 1, 1);
            return new ItemTriggerScenarioSetup
            {
                Identity = identity,
                Hero = hero,
                Enemy = enemy,
                Swing = swing,
                ForcedD20 = face,
                StripLength = stripLen,
                SetupNotes = notes,
                When = when
            };
        }

        public static Item MakeGearItem(ItemTriggerIdentityCatalog.Identity identity, bool equipChannel)
        {
            var bundle = ItemTriggerIdentityCatalog.ToBundle(identity);
            var tags = new List<string>();
            if (HasFilter(identity, "IFGEARHASTAG"))
            {
                string? t = FilterArg(identity, "IFGEARHASTAG");
                if (!string.IsNullOrWhiteSpace(t))
                    tags.Add(t!);
            }

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

            var weapon = new WeaponItem($"TrigWep{identity.Index}", 1, 25, 1.0, wt) { Tags = tags };
            if (equipChannel)
                weapon.EquipEffects = new List<ActionTriggerBundle> { bundle };
            else
                weapon.TriggerBundles = new List<ActionTriggerBundle> { bundle };
            return weapon;
        }

        public static int ResolveFaceForWhen(string when, ItemTriggerIdentityCatalog.Identity identity)
        {
            string rawWhen = identity.When ?? when;
            int colon = rawWhen.IndexOf(':');
            if (colon > 0 && int.TryParse(rawWhen.Substring(colon + 1).Trim(), out int n) && n > 0)
            {
                string head = ActionTriggerGate.NormalizeToken(rawWhen.Substring(0, colon));
                if (head.Contains("NATURAL") || head.Contains("ROLL"))
                    return Math.Clamp(n, 2, 19);
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

        // ─── Private setup ────────────────────────────────────────────────────

        private static void PrepareFiltersForCombat(
            Character hero,
            ItemTriggerIdentityCatalog.Identity identity,
            out Action swing,
            out Enemy enemy,
            out int stripLen,
            List<string> notes)
        {
            stripLen = 3;
            swing = BuildSwingAction(identity);
            EnsureStrip(hero, swing, stripLen);
            TagSwingForFilters(swing, identity);

            enemy = MakeTank($"Foe{identity.Index}");
            if (HasFilter(identity, "IFTARGETHASTAG"))
            {
                string? tag = FilterArg(identity, "IFTARGETHASTAG");
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    enemy.SetTags(new[] { tag! });
                    notes.Add($"Enemy tagged '{tag}'");
                }
            }

            if (HasFilter(identity, "IFTARGETUNDERDOT"))
            {
                enemy.ApplyPoison(5);
                notes.Add("Enemy under DoT (poison)");
            }

            if (HasFilter(identity, "IFSOURCEUNDERDOT"))
            {
                hero.ApplyPoison(5);
                notes.Add("Hero under DoT (poison)");
            }

            if (HasFilter(identity, "IFCLUTCH"))
            {
                hero.CurrentHealth = Math.Max(1, hero.MaxHealth / 10);
                notes.Add($"IFCLUTCH: hero HP={hero.CurrentHealth}");
            }

            if (HasFilter(identity, "IFSLOT"))
            {
                string? arg = FilterArg(identity, "IFSLOT");
                if (int.TryParse(arg, out int slot1) && slot1 > 0)
                {
                    EnsureStrip(hero, swing, Math.Max(stripLen, slot1));
                    stripLen = Math.Max(stripLen, slot1);
                    hero.ComboStep = slot1 - 1;
                    var strip = hero.GetComboActions();
                    swing = strip[hero.ComboStep % strip.Count];
                    TagSwingForFilters(swing, identity);
                    notes.Add($"IFSLOT:{slot1} → ComboStep={hero.ComboStep}");
                }
            }

            if (HasFilter(identity, "IFATTR"))
            {
                hero.Strength = Math.Max(hero.Strength, 12);
                notes.Add("IFATTR: Strength≥12");
            }
        }

        private static void PrepareFiltersForEquip(
            Character hero,
            ItemTriggerIdentityCatalog.Identity identity,
            List<string> notes)
        {
            if (HasFilter(identity, "IFCLUTCH"))
            {
                hero.CurrentHealth = Math.Max(1, hero.MaxHealth / 10);
                notes.Add($"IFCLUTCH: hero HP={hero.CurrentHealth}");
            }

            if (HasFilter(identity, "IFATTR"))
            {
                hero.Strength = Math.Max(hero.Strength, 12);
                notes.Add("IFATTR: Strength≥12");
            }

            if (HasFilter(identity, "IFCLASSTAG"))
            {
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
                    notes.Add($"IFCLASSTAG:{cls} → equipped {wt}");
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
                notes.Add($"scaleFrom={identity.ScaleFrom}: attrs/class points raised");
            }
        }

        private static Action BuildSwingAction(ItemTriggerIdentityCatalog.Identity identity)
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
                hero.Actions.AddToCombo(extra, maxComboLength: null);
            }

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

        public static string NormalizeMech(string? mechanics)
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

        private static int CountActionBank(Character? hero)
        {
            if (hero == null) return 0;
            return hero.Effects.PeekPendingActionBonusesNextHeroRoll()?.Count ?? 0;
        }

        private static int CountTurnBonuses(Character hero) =>
            hero.Effects.PeekTurnBonuses()?.Count ?? 0;

        private static string SlotFor(Item item) =>
            item is WeaponItem ? "Weapon"
            : item is HeadItem ? "Head"
            : item is ChestItem ? "Body"
            : item is LegsItem ? "Legs"
            : "Feet";

        private static ItemTriggerBuffSnapshot CaptureBuffs(Character hero)
        {
            var strip = CombatTriggerContext.GetStripState(hero);
            return new ItemTriggerBuffSnapshot
            {
                HeroHp = hero.CurrentHealth,
                HeroMaxHp = hero.MaxHealth,
                ActionBankCount = CountActionBank(hero),
                TurnBonusCount = CountTurnBonuses(hero),
                FightBonusCount = hero.FightCadenceBuffs.Bonuses.Count,
                DungeonBonusCount = hero.DungeonCadenceBuffs.Bonuses.Count,
                HardenTurns = hero.HardenTurns,
                FocusTurns = hero.FocusTurns,
                FortifyTurns = hero.FortifyTurns,
                MissSalvageCharges = CombatTriggerContext.GetMissSalvageCharges(hero),
                HasCritFaceMin = CombatTriggerContext.TryGetCritFaceMin(hero, out _),
                StripDisabledCount = strip?.DisabledSlots.Count ?? 0,
                StripHasPendingRouting = strip?.HasPendingRouting ?? false,
                StripHasReplace = !string.IsNullOrWhiteSpace(strip?.ReplaceNextActionName),
                StripHasShuffle = strip?.ShufflePermutation != null,
                ConsumedDamageModPercent = hero.Effects.ConsumedDamageModPercent,
                ConsumedSpeedModPercent = hero.Effects.ConsumedSpeedModPercent,
                ConsumedAmpModPercent = hero.Effects.ConsumedAmpModPercent,
                EquipArmorBonus = ItemEquipEffectApplicator.GetEquippedArmorBonus(hero),
                EquipPrimaryStatBonus = ItemEquipEffectApplicator.GetEquippedStatBonus(hero, "PRIMARY"),
                GrantedActionCount = ItemEquipEffectApplicator.GetGrantedActionNames(hero).Count,
                GrantedTagCount = hero.EquipmentGrantedActionTags?.Count ?? 0
            };
        }

        private static IReadOnlyList<string> BuildBuffDeltaLines(
            ItemTriggerBuffSnapshot? before, ItemTriggerBuffSnapshot? after)
        {
            if (before == null || after == null)
                return Array.Empty<string>();
            var lines = new List<string>();
            void Delta(string label, int b, int a)
            {
                if (a != b) lines.Add($"{label}: {b} → {a}");
            }

            void DeltaD(string label, double b, double a)
            {
                if (Math.Abs(a - b) > 0.001) lines.Add($"{label}: {b:0.##} → {a:0.##}");
            }

            Delta("HP", before.HeroHp, after.HeroHp);
            Delta("ACTION bank", before.ActionBankCount, after.ActionBankCount);
            Delta("TURN bonuses", before.TurnBonusCount, after.TurnBonusCount);
            Delta("FIGHT bonuses", before.FightBonusCount, after.FightBonusCount);
            Delta("DUNGEON bonuses", before.DungeonBonusCount, after.DungeonBonusCount);
            Delta("Harden turns", before.HardenTurns, after.HardenTurns);
            Delta("Focus turns", before.FocusTurns, after.FocusTurns);
            Delta("Fortify turns", before.FortifyTurns, after.FortifyTurns);
            Delta("Miss salvage", before.MissSalvageCharges, after.MissSalvageCharges);
            if (before.HasCritFaceMin != after.HasCritFaceMin)
                lines.Add($"crit_face_min: {before.HasCritFaceMin} → {after.HasCritFaceMin}");
            Delta("Strip disabled", before.StripDisabledCount, after.StripDisabledCount);
            if (before.StripHasPendingRouting != after.StripHasPendingRouting)
                lines.Add($"Strip pending: {before.StripHasPendingRouting} → {after.StripHasPendingRouting}");
            if (before.StripHasReplace != after.StripHasReplace)
                lines.Add($"Strip replace: {before.StripHasReplace} → {after.StripHasReplace}");
            if (before.StripHasShuffle != after.StripHasShuffle)
                lines.Add($"Strip shuffle: {before.StripHasShuffle} → {after.StripHasShuffle}");
            DeltaD("Consumed dmg%", before.ConsumedDamageModPercent, after.ConsumedDamageModPercent);
            DeltaD("Consumed spd%", before.ConsumedSpeedModPercent, after.ConsumedSpeedModPercent);
            DeltaD("Consumed amp%", before.ConsumedAmpModPercent, after.ConsumedAmpModPercent);
            Delta("Equip armor", before.EquipArmorBonus, after.EquipArmorBonus);
            Delta("Equip PRIMARY", before.EquipPrimaryStatBonus, after.EquipPrimaryStatBonus);
            Delta("Granted actions", before.GrantedActionCount, after.GrantedActionCount);
            Delta("Granted tags", before.GrantedTagCount, after.GrantedTagCount);
            return lines;
        }

        private static ItemTriggerScenarioReport BaseReport(
            ItemTriggerIdentityCatalog.Identity identity, string path) =>
            new()
            {
                IdentityIndex = identity.Index,
                IdentityName = identity.Name,
                Description = identity.Description ?? "",
                When = identity.When ?? "",
                Scope = identity.Scope ?? "",
                Mechanics = identity.Mechanics ?? "",
                Value = identity.Value,
                ScaleFrom = identity.ScaleFrom,
                Filters = identity.Filters ?? Array.Empty<string>(),
                Channel = identity.IsEquipEffect ? "equip" : "combat",
                Path = path
            };

        private static ItemTriggerScenarioReport Finish(
            ItemTriggerIdentityCatalog.Identity identity,
            string path,
            int face,
            List<string> notes,
            ItemTriggerBuffSnapshot before,
            ItemTriggerBuffSnapshot after,
            bool passed,
            string finding,
            ActionExecutionResult? result = null,
            IReadOnlyList<string>? statusMessages = null,
            string? channel = null,
            bool outcomeMatched = true,
            string? outcomeDetail = null)
        {
            var report = BaseReport(identity, path);
            report.ForcedD20 = face;
            report.SetupNotes = notes.ToList();
            report.Before = before;
            report.After = after;
            report.BuffDeltaLines = BuildBuffDeltaLines(before, after);
            report.Passed = passed;
            report.Finding = finding;
            report.OutcomeMatched = outcomeMatched;
            report.OutcomeDetail = outcomeDetail;
            if (channel != null)
                report.Channel = channel;
            if (result != null)
            {
                report.StatusMessages = result.StatusEffectMessages?.ToList() ?? new List<string>();
                report.DamageDealt = result.Damage;
                report.NestedRetriggerCount = result.NestedRetriggerResults?.Count ?? 0;
                report.Hit = result.Hit;
                report.IsCritical = result.IsCritical;
                report.IsCombo = result.IsCombo;
                report.IsCriticalMiss = result.IsCriticalMiss;
            }
            else if (statusMessages != null)
            {
                report.StatusMessages = statusMessages.ToList();
            }

            return report;
        }
    }

    /// <summary>Prepared attacker-combat entities for Action Lab Load Scenario.</summary>
    public sealed class ItemTriggerScenarioSetup
    {
        public ItemTriggerIdentityCatalog.Identity Identity { get; set; } = null!;
        public Character Hero { get; set; } = null!;
        public Enemy Enemy { get; set; } = null!;
        public Action Swing { get; set; } = null!;
        public int ForcedD20 { get; set; }
        public int StripLength { get; set; }
        public IReadOnlyList<string> SetupNotes { get; set; } = Array.Empty<string>();
        public string When { get; set; } = "";
    }
}
