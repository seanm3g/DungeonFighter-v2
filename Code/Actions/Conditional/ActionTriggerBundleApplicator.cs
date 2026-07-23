using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RPGGame;
using RPGGame.Actions.Execution;
using RPGGame.Actions.RollModification;
using RPGGame.Combat.Events;
using RPGGame.Data;

namespace RPGGame.Actions.Conditional
{
    /// <summary>
    /// Applies <see cref="ActionTriggerBundle"/> mechanics when their WHEN matches a combat event.
    /// Blank SCOPE = instant; TURN / ACTION / FIGHT / DUNGEON = lasting grants via existing cadence deposits.
    /// Mechanics listed under any TRIGGERS → are "owned" and skipped by whole-row status / sheet-mod apply.
    /// </summary>
    public static class ActionTriggerBundleApplicator
    {
        private static readonly EffectHandlerRegistry EffectRegistry = new EffectHandlerRegistry();

        private static readonly HashSet<string> InstantStatusIds = new(StringComparer.OrdinalIgnoreCase)
        {
            "weaken", "slow", "vulnerability", "harden", "focus", "confuse", "confusion",
            "stat_drain", "statdrain", "fortify", "pierce", "disrupt", "expose", "silence",
            "bleed", "poison", "burn", "acid", "stun", "mark", "hpregen", "armorbreak",
            "absorb", "temporaryhp"
        };

        /// <summary>Mechanic IDs pointed at by any enabled trigger bundle on this action.</summary>
        public static HashSet<string> GetOwnedMechanicIds(Action? action)
        {
            var owned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (action?.Triggers?.Bundles == null)
                return owned;
            foreach (var bundle in action.Triggers.Bundles)
            {
                if (bundle == null || !bundle.IsEnabled)
                    continue;
                foreach (string id in bundle.ParseMechanicIds())
                    owned.Add(ActionMechanicsRegistry.NormalizeMechanicId(id));
            }

            return owned;
        }

        public static bool IsMechanicOwned(Action? action, string? mechanicId)
        {
            if (string.IsNullOrWhiteSpace(mechanicId) || action == null)
                return false;
            string id = ActionMechanicsRegistry.NormalizeMechanicId(mechanicId);
            var owned = GetOwnedMechanicIds(action);
            if (owned.Contains(id))
                return true;
            // Status aliases between sheet IDs and effect registry names
            if (id is "confusion" && owned.Contains("confuse")) return true;
            if (id is "confuse" && owned.Contains("confusion")) return true;
            if (id is "statdrain" && owned.Contains("stat_drain")) return true;
            if (id is "stat_drain" && owned.Contains("statdrain")) return true;
            return false;
        }

        public static bool IsStatusEffectOwned(Action? action, string? effectType) =>
            IsMechanicOwned(action, effectType);

        public static bool IsSheetModTypeOwned(Action? action, string? bonusType, bool useEnemySpreadsheetMods = false)
        {
            if (string.IsNullOrWhiteSpace(bonusType) || action == null)
                return false;
            if (!ActionMechanicsRegistry.TryGetMechanicIdFromBonusType(bonusType, out string mechanicId, out _))
                return false;
            if (useEnemySpreadsheetMods
                && mechanicId.StartsWith("hero_", StringComparison.OrdinalIgnoreCase))
            {
                mechanicId = "enemy_" + mechanicId.Substring("hero_".Length);
            }

            return IsMechanicOwned(action, mechanicId);
        }

        /// <summary>
        /// Removes sheet SPEED/DAMAGE/MULTIHIT/AMP items whose mechanic IDs are owned by TRIGGERS → pointers.
        /// </summary>
        public static List<ActionAttackBonusItem> FilterOutOwnedSheetMods(
            Action? action,
            List<ActionAttackBonusItem> bonuses,
            bool useEnemySpreadsheetMods = false)
        {
            if (action == null || bonuses == null || bonuses.Count == 0)
                return bonuses ?? new List<ActionAttackBonusItem>();
            if (GetOwnedMechanicIds(action).Count == 0)
                return bonuses;
            return bonuses
                .Where(b => !IsSheetModTypeOwned(action, b.Type, useEnemySpreadsheetMods))
                .ToList();
        }

        /// <summary>
        /// Applies every enabled bundle whose WHEN matches <paramref name="combatEvent"/> (row filters still AND).
        /// </summary>
        public static bool ApplyMatchingBundles(
            Action action,
            CombatEvent? combatEvent,
            Actor source,
            Actor target,
            List<string> messages)
        {
            if (action?.Triggers?.Bundles == null || action.Triggers.Bundles.Count == 0)
                return false;
            if (!ActionTriggerGate.PassesNonOutcomeFilters(action, combatEvent))
                return false;

            bool any = false;
            foreach (var bundle in action.Triggers.Bundles)
            {
                if (bundle == null || !bundle.IsEnabled)
                    continue;
                if (bundle.ParseMechanicIds().Count == 0)
                    continue;
                if (!BundleWhenMatches(bundle, action, combatEvent))
                    continue;

                any |= ApplyBundle(bundle, action, source, target, messages);
            }

            return any;
        }

        private static bool BundleWhenMatches(ActionTriggerBundle bundle, Action action, CombatEvent? combatEvent)
        {
            string when = ActionTriggerGate.NormalizeToken(bundle.When ?? "");
            // Equip-time channel — never fire during combat swings.
            if (when is "WHILEEQUIPPED" or "ONEQUIP")
                return false;

            if (when == "ONROLLVALUE"
                && int.TryParse((bundle.Count ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int face)
                && face > 0)
            {
                return ActionTriggerGate.MatchesConditionToken($"ONROLLVALUE:{face}", action, combatEvent);
            }

            if (when == "ONNATURALROLL"
                && int.TryParse((bundle.Count ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int nat)
                && nat > 0)
            {
                return ActionTriggerGate.MatchesConditionToken($"ONNATURALROLL:{nat}", action, combatEvent);
            }

            if (when == "ONROOMSCLEARED"
                && int.TryParse((bundle.Count ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n)
                && n > 0)
            {
                return ActionTriggerGate.MatchesConditionToken($"ONROOMSCLEARED:{n}", action, combatEvent);
            }

            return ActionTriggerGate.MatchesConditionToken(when, action, combatEvent);
        }

        private static bool ApplyBundle(
            ActionTriggerBundle bundle,
            Action action,
            Actor source,
            Actor target,
            List<string> messages)
        {
            string scope = CadenceKeywords.Normalize(bundle.Scope);
            bool lasting = CadenceKeywords.IsTurn(scope)
                           || CadenceKeywords.IsAction(scope)
                           || CadenceKeywords.IsFight(scope)
                           || CadenceKeywords.IsDungeon(scope);

            var heroBonuses = new List<ActionAttackBonusItem>();
            var enemyBonuses = new List<ActionAttackBonusItem>();
            bool any = false;

            foreach (string rawId in bundle.ParseMechanicIds())
            {
                string id = ActionMechanicsRegistry.NormalizeMechanicId(rawId);
                string? mechanicArg = null;
                int colon = (rawId ?? "").IndexOf(':');
                if (colon > 0)
                {
                    id = ActionMechanicsRegistry.NormalizeMechanicId(rawId!.Substring(0, colon));
                    mechanicArg = rawId.Substring(colon + 1).Trim();
                }

                if (string.IsNullOrEmpty(id))
                    continue;

                if (id is "heal" or "max_health")
                {
                    any |= ApplyHealOrMaxHealth(id, action, source, messages, bundle.Value);
                    continue;
                }

                // Same-swing % damage (not next-action bank). Used by item mirror procs.
                if (id is "hero_action_damage")
                {
                    if (source is Character sameSwingHero && bundle.Value is { } sv && sv != 0)
                    {
                        sameSwingHero.Effects.ConsumedDamageModPercent += sv;
                        messages.Add($"{source.Name} gains {sv:+0;-0}% damage this swing.");
                        any = true;
                    }
                    continue;
                }

                if (StripMutationApplier.IsStripMechanic(id)
                    || StripMutationApplier.IsStripMechanic(rawId))
                {
                    any |= StripMutationApplier.TryApply(id, mechanicArg, bundle.Count, action, source, messages);
                    continue;
                }

                if (RetriggerScheduler.IsRetriggerMechanic(id))
                {
                    any |= RetriggerScheduler.TrySchedule(id, mechanicArg, bundle.Count, action, source, messages);
                    continue;
                }

                if (id is "salvage_miss")
                {
                    int charges = ParsePositiveCount(bundle.Count, 1);
                    if (!string.IsNullOrWhiteSpace(mechanicArg)
                        && int.TryParse(mechanicArg, NumberStyles.Integer, CultureInfo.InvariantCulture, out int a)
                        && a > 0)
                        charges = a;
                    else if (bundle.Value is > 0)
                        charges = Math.Max(1, (int)Math.Round(bundle.Value.Value));
                    CombatTriggerContext.AddMissSalvageCharges(source, charges);
                    messages.Add($"{source.Name} gains miss salvage ({charges}).");
                    any = true;
                    continue;
                }

                if (id is "crit_face_min")
                {
                    int minFace = 19;
                    if (!string.IsNullOrWhiteSpace(mechanicArg)
                        && int.TryParse(mechanicArg, NumberStyles.Integer, CultureInfo.InvariantCulture, out int mf)
                        && mf > 0)
                        minFace = mf;
                    else if (bundle.Value is >= 2 and <= 20)
                        minFace = (int)Math.Round(bundle.Value.Value);
                    else if (int.TryParse((bundle.Count ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int cf)
                             && cf >= 2 && cf <= 20)
                        minFace = cf;
                    else if (action.RollMods.CriticalHitThresholdOverride > 0)
                        minFace = action.RollMods.CriticalHitThresholdOverride;
                    CombatTriggerContext.SetCritFaceMin(source, minFace);
                    // Also shift live crit threshold for this fight so Selection picks it up after reset+gear.
                    if (source is Character critHero)
                    {
                        int critStacks = lasting ? ParsePositiveCount(bundle.Count, 1) : 1;
                        var critItems = new List<ActionAttackBonusItem>
                        {
                            new ActionAttackBonusItem
                            {
                                Type = RollModificationManager.SetCriticalHitThresholdType,
                                Value = minFace
                            }
                        };
                        if (lasting && (CadenceKeywords.IsFight(scope) || CadenceKeywords.IsDungeon(scope) || CadenceKeywords.IsTurn(scope)))
                            CadenceScopedBuffApplicator.DepositToScope(critHero, scope, critItems, critStacks);
                        else
                        {
                            // Instant: set fight override via context; Selection reads TryGetCritFaceMin
                        }
                    }
                    messages.Add($"{source.Name}: crit on natural faces ≥ {minFace}.");
                    any = true;
                    continue;
                }

                if (id is "replace_next_roll")
                {
                    int face = 20;
                    if (!string.IsNullOrWhiteSpace(mechanicArg)
                        && int.TryParse(mechanicArg, NumberStyles.Integer, CultureInfo.InvariantCulture, out int fa)
                        && fa > 0)
                        face = fa;
                    else if (bundle.Value is >= 1 and <= 20)
                        face = (int)Math.Round(bundle.Value.Value);
                    else if (int.TryParse((bundle.Count ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int fb)
                             && fb >= 1 && fb <= 20)
                        face = fb;
                    else if (action.RollMods.ExplodingDiceThreshold > 0
                             && action.RollMods.ExplodingDiceThreshold <= 20)
                    {
                        // No dedicated ReplaceNextRoll runtime field on Action — use exact roll trigger as hint
                    }
                    if (action.Triggers?.ExactRollTriggerValue > 0 && face == 20
                        && string.IsNullOrWhiteSpace(mechanicArg)
                        && bundle.Value == null
                        && !int.TryParse((bundle.Count ?? "").Trim(), out _))
                        face = action.Triggers.ExactRollTriggerValue;
                    CombatTriggerContext.SetPendingReplaceRollFace(source, face);
                    messages.Add($"{source.Name}'s next roll becomes {face}.");
                    any = true;
                    continue;
                }

                if (InstantStatusIds.Contains(id))
                {
                    any |= ApplyStatus(id, action, source, target, messages);
                    continue;
                }

                bool isEnemy = id.StartsWith("enemy_", StringComparison.OrdinalIgnoreCase);
                var into = isEnemy ? enemyBonuses : heroBonuses;
                if (TryBuildBonusItems(action, id, into, bundle.Value))
                    any = true;
            }

            int stacks = lasting ? ParsePositiveCount(bundle.Count, 1) : 1;
            if (heroBonuses.Count > 0 && source is Character hero && hero is not Enemy)
            {
                DepositBonuses(hero, action, heroBonuses, lasting ? scope : "", lasting ? stacks : 1, lasting);
                any = true;
            }

            if (enemyBonuses.Count > 0 && target is Character foe)
            {
                DepositBonuses(foe, action, enemyBonuses, lasting ? scope : "", lasting ? stacks : 1, lasting);
                any = true;
            }

            return any;
        }

        private static int ParsePositiveCount(string? countCell, int fallback)
        {
            if (int.TryParse((countCell ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) && n > 0)
                return n;
            return fallback;
        }

        private static void DepositBonuses(
            Character recipient,
            Action action,
            List<ActionAttackBonusItem> bonusItems,
            string scope,
            int stacks,
            bool lasting)
        {
            if (!lasting || string.IsNullOrWhiteSpace(scope))
            {
                DepositInstantBonuses(recipient, action, bonusItems);
                return;
            }

            stacks = Math.Max(1, stacks);
            if (CadenceKeywords.IsAction(scope))
            {
                recipient.Effects.AccumulatePendingActionCadenceBank(bonusItems, stacks);
                return;
            }

            if (CadenceKeywords.IsFight(scope) || CadenceKeywords.IsDungeon(scope))
            {
                CadenceScopedBuffApplicator.DepositToScope(recipient, scope, bonusItems, stacks);
                return;
            }

            // TURN
            recipient.Effects.AddActionAttackBonuses(new ActionAttackBonuses
            {
                BonusGroups = new List<ActionAttackBonusGroup>
                {
                    new ActionAttackBonusGroup
                    {
                        Keyword = CadenceKeywords.Turn,
                        CadenceType = CadenceKeywords.Turn,
                        Count = stacks,
                        Bonuses = bonusItems,
                        DurationType = CadenceKeywords.Turn
                    }
                }
            }, action, recipient);
        }

        private static void DepositInstantBonuses(
            Character recipient,
            Action action,
            List<ActionAttackBonusItem> bonusItems)
        {
            var nextAction = new List<ActionAttackBonusItem>();
            var turn = new List<ActionAttackBonusItem>();
            foreach (var item in bonusItems)
            {
                string t = (item.Type ?? "").Trim().ToUpperInvariant();
                if (t is "SPEED_MOD" or "DAMAGE_MOD" or "MULTIHIT_MOD" or "AMP_MOD")
                    nextAction.Add(item);
                else
                    turn.Add(item);
            }

            if (nextAction.Count > 0)
                recipient.Effects.AccumulatePendingActionCadenceBank(nextAction, 1);

            if (turn.Count > 0)
            {
                recipient.Effects.AddActionAttackBonuses(new ActionAttackBonuses
                {
                    BonusGroups = new List<ActionAttackBonusGroup>
                    {
                        new ActionAttackBonusGroup
                        {
                            Keyword = CadenceKeywords.Turn,
                            CadenceType = CadenceKeywords.Turn,
                            Count = 1,
                            Bonuses = turn,
                            DurationType = CadenceKeywords.Turn
                        }
                    }
                }, action, recipient);
            }
        }

        private static bool ApplyHealOrMaxHealth(string mechanicId, Action action, Actor source, List<string> messages, double? bundleValue)
        {
            if (mechanicId == "heal")
            {
                int amount = action.Advanced?.HealAmount ?? 0;
                if (amount <= 0 && bundleValue is > 0)
                    amount = Math.Max(1, (int)Math.Round(bundleValue.Value));
                if (amount <= 0)
                    return false;
                ActionUtilities.ApplyHealing(source, amount);
                messages.Add($"{source.Name} heals {amount}.");
                return true;
            }

            if (mechanicId == "max_health" && source is Character hero)
            {
                int amount = action.Advanced?.MaxHealthIncrease ?? 0;
                if (amount <= 0 && bundleValue is > 0)
                    amount = Math.Max(1, (int)Math.Round(bundleValue.Value));
                if (amount <= 0)
                    return false;
                ActionUtilities.ApplyMaxHealthIncrease(hero, amount);
                messages.Add($"{hero.Name} gains permanent vitality (+{amount}).");
                return true;
            }

            return false;
        }

        private static bool ApplyStatus(
            string mechanicId,
            Action action,
            Actor source,
            Actor target,
            List<string> messages)
        {
            string effectType = NormalizeStatusEffectType(mechanicId);
            var effectRecipient = ActionEffectTargetResolver.ResolveStatusEffectRecipient(
                action, effectType, source, target);
            var effectAction = StatusEffectActionResolver.ResolveActionForEffectApplication(action, effectType);
            return EffectRegistry.ApplyEffect(effectType, effectRecipient, effectAction, messages);
        }

        private static string NormalizeStatusEffectType(string mechanicId)
        {
            string id = ActionMechanicsRegistry.NormalizeMechanicId(mechanicId);
            return id switch
            {
                "confuse" => "confusion",
                "stat_drain" => "statdrain",
                _ => id
            };
        }

        private static bool TryBuildBonusItems(Action action, string mechanicId, List<ActionAttackBonusItem> into, double? bundleValue)
        {
            switch (mechanicId)
            {
                case "hero_accuracy":
                    if (action.Advanced.RollBonus != 0)
                    {
                        into.Add(new ActionAttackBonusItem { Type = "ACCURACY", Value = action.Advanced.RollBonus });
                        return true;
                    }
                    return AddBundleValue(into, "ACCURACY", bundleValue);
                case "enemy_accuracy":
                    if (action.Advanced.EnemyRollBonus != 0)
                    {
                        into.Add(new ActionAttackBonusItem { Type = "ACCURACY", Value = action.Advanced.EnemyRollBonus });
                        return true;
                    }
                    return AddBundleValue(into, "ACCURACY", bundleValue);
                case "hero_hit_threshold":
                    return AddThreshold(into, "HIT", action.RollMods.HitThresholdAdjustment, bundleValue);
                case "enemy_hit_threshold":
                    return AddThreshold(into, "HIT", action.RollMods.EnemyHitThresholdAdjustment, bundleValue);
                case "hero_combo_threshold":
                    return AddThreshold(into, "COMBO", action.RollMods.ComboThresholdAdjustment, bundleValue);
                case "enemy_combo_threshold":
                    return AddThreshold(into, "COMBO", action.RollMods.EnemyComboThresholdAdjustment, bundleValue);
                case "hero_crit_threshold":
                    return AddThreshold(into, "CRIT", action.RollMods.CriticalHitThresholdAdjustment, bundleValue);
                case "enemy_crit_threshold":
                    return AddThreshold(into, "CRIT", action.RollMods.EnemyCriticalHitThresholdAdjustment, bundleValue);
                case "hero_crit_miss_threshold":
                    return AddThreshold(into, "CRIT_MISS", action.RollMods.CriticalMissThresholdAdjustment, bundleValue);
                case "enemy_crit_miss_threshold":
                    return AddThreshold(into, "CRIT_MISS", action.RollMods.EnemyCriticalMissThresholdAdjustment, bundleValue);
                case "hero_next_action_speed":
                    return AddParsedModOrBundle(into, "SPEED_MOD", action.SpeedMod, percent: true, bundleValue);
                case "hero_next_action_damage":
                    return AddParsedModOrBundle(into, "DAMAGE_MOD", action.DamageMod, percent: true, bundleValue);
                case "hero_action_damage":
                    // Same-swing handled in ApplyBundle; not a depositable bonus item.
                    return false;
                case "hero_next_action_multihit":
                    return AddParsedModOrBundle(into, "MULTIHIT_MOD", action.MultiHitMod, percent: false, bundleValue);
                case "hero_next_action_amp":
                    return AddParsedModOrBundle(into, "AMP_MOD", action.AmpMod, percent: true, bundleValue);
                case "enemy_next_action_speed":
                    return AddParsedModOrBundle(into, "SPEED_MOD", action.EnemySpeedMod, percent: true, bundleValue);
                case "enemy_next_action_damage":
                    return AddParsedModOrBundle(into, "DAMAGE_MOD", action.EnemyDamageMod, percent: true, bundleValue);
                case "enemy_next_action_multihit":
                    return AddParsedModOrBundle(into, "MULTIHIT_MOD", action.EnemyMultiHitMod, percent: false, bundleValue);
                case "enemy_next_action_amp":
                    return AddParsedModOrBundle(into, "AMP_MOD", action.EnemyAmpMod, percent: true, bundleValue);
                case "hero_weapon_speed":
                    return AddParsedModOrBundle(into, "WEAPON_SPEED", action.WeaponSpeedMod, percent: false, bundleValue);
                case "hero_weapon_damage":
                    return AddParsedModOrBundle(into, "WEAPON_DAMAGE", action.WeaponDamageMod, percent: false, bundleValue);
                case "enemy_weapon_speed":
                    return AddParsedModOrBundle(into, "WEAPON_SPEED", action.EnemyWeaponSpeedMod, percent: false, bundleValue);
                case "enemy_weapon_damage":
                    return AddParsedModOrBundle(into, "WEAPON_DAMAGE", action.EnemyWeaponDamageMod, percent: false, bundleValue);
                case "hero_stat_bonus":
                case "enemy_stat_bonus":
                    return AddStatBonuses(action, mechanicId, into);
                default:
                    return false;
            }
        }

        private static bool AddThreshold(List<ActionAttackBonusItem> into, string type, int value, double? bundleValue = null)
        {
            if (value == 0 && bundleValue is { } bv && bv != 0)
                value = (int)Math.Round(bv);
            if (value == 0)
                return false;
            into.Add(new ActionAttackBonusItem { Type = type, Value = value });
            return true;
        }

        private static bool AddBundleValue(List<ActionAttackBonusItem> into, string type, double? bundleValue)
        {
            if (bundleValue is not { } v || v == 0)
                return false;
            into.Add(new ActionAttackBonusItem { Type = type, Value = v });
            return true;
        }

        private static bool AddParsedModOrBundle(
            List<ActionAttackBonusItem> into,
            string type,
            string? cell,
            bool percent,
            double? bundleValue)
        {
            if (AddParsedMod(into, type, cell, percent))
                return true;
            return AddBundleValue(into, type, bundleValue);
        }

        private static bool AddStatBonuses(Action action, string mechanicId, List<ActionAttackBonusItem> into)
        {
            bool any = false;
            var entries = action.Advanced?.StatBonuses;
            if (entries == null || entries.Count == 0)
            {
                if (action.Advanced != null
                    && (action.Advanced.StatBonus != 0 || !string.IsNullOrEmpty(action.Advanced.StatBonusType)))
                {
                    entries = new List<StatBonusEntry>
                    {
                        new StatBonusEntry
                        {
                            Value = action.Advanced.StatBonus,
                            Type = action.Advanced.StatBonusType ?? ""
                        }
                    };
                }
            }

            if (entries == null)
                return false;

            foreach (var e in entries)
            {
                if (e.Value == 0 || string.IsNullOrWhiteSpace(e.Type))
                    continue;
                if (!ActionMechanicsRegistry.TryGetBonusTypeForMechanic(mechanicId, e.Type, out string bt))
                    continue;
                into.Add(new ActionAttackBonusItem { Type = bt, Value = e.Value });
                any = true;
            }

            return any;
        }

        private static bool AddParsedMod(List<ActionAttackBonusItem> into, string type, string? cell, bool percent)
        {
            if (string.IsNullOrWhiteSpace(cell))
                return false;
            double qty;
            if (percent)
            {
                if (ModifierParser.ParsePercent(cell) is { } p)
                    qty = p * 100.0;
                else if (!double.TryParse(cell.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out qty))
                    return false;
            }
            else
            {
                qty = ModifierParser.ParseValue(cell) ?? 0;
            }

            if (qty == 0)
                return false;
            into.Add(new ActionAttackBonusItem { Type = type, Value = qty });
            return true;
        }
    }
}
