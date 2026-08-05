using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Actions.Conditional;
using RPGGame.Actions.RollModification;
using RPGGame.Combat;
using RPGGame.Combat.Events;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Fires catalog <see cref="Item.TriggerBundles"/> on the attacker's equipped gear
    /// through <see cref="ActionTriggerBundleApplicator"/> (same WHEN×DO×SCOPE grammar as actions).
    /// Carrier actions hold bundles/magnitudes only; filters evaluate the swing on <see cref="CombatEvent.Action"/>.
    /// </summary>
    public static class EquippedItemTriggerApplicator
    {
        private static readonly HashSet<string> SameSwingMechanicIds = new(StringComparer.OrdinalIgnoreCase)
        {
            "hero_action_damage",
            "hero_action_speed",
            "hero_action_amp"
        };

        private static readonly HashSet<string> PreRollSameSwingMechanicIds = new(StringComparer.OrdinalIgnoreCase)
        {
            "hero_action_speed",
            "hero_hit_threshold",
            "hero_combo_threshold",
            "hero_crit_threshold"
        };

        /// <summary>
        /// Apply trigger bundles from every equipped item on <paramref name="attacker"/>.
        /// No-op when attacker is not a <see cref="Character"/> or has no gear procs.
        /// </summary>
        public static bool ApplyFromAttacker(
            Actor attacker,
            Actor target,
            CombatEvent? combatEvent,
            List<string> messages)
        {
            if (attacker is not Character character)
                return false;
            if (combatEvent == null)
                return false;

            bool any = false;
            foreach (var item in EnumerateEquipped(character))
            {
                if (item?.TriggerBundles == null || item.TriggerBundles.Count == 0)
                    continue;
                if (!item.TriggerBundles.Any(b => b != null && b.IsEnabled && !ItemEquipEffectApplicator.IsWhileEquipped(b)))
                    continue;

                var carrier = BuildCarrierAction(item);
                any |= ActionTriggerBundleApplicator.ApplyMatchingBundles(
                    carrier, combatEvent, character, target ?? character, messages);
            }

            return any;
        }

        /// <summary>
        /// Apply combat trigger bundles when the hero is hit (defender channel — <c>ONTAKEHIT</c>).
        /// </summary>
        public static bool ApplyFromDefender(
            Character hero,
            Actor? attacker,
            CombatEvent? combatEvent,
            List<string> messages)
        {
            if (hero == null || hero is Enemy)
                return false;
            if (combatEvent == null)
                return false;

            bool any = false;
            foreach (var item in EnumerateEquipped(hero))
            {
                if (item?.TriggerBundles == null || item.TriggerBundles.Count == 0)
                    continue;
                if (!item.TriggerBundles.Any(b =>
                        b != null
                        && b.IsEnabled
                        && !ItemEquipEffectApplicator.IsWhileEquipped(b)
                        && IsTakeHitWhen(b.When)))
                    continue;

                var carrier = BuildCarrierAction(item, onlyWhenTakeHit: true);
                any |= ActionTriggerBundleApplicator.ApplyMatchingBundles(
                    carrier, combatEvent, hero, attacker ?? hero, messages);
            }

            return any;
        }

        /// <summary>
        /// Pre-damage pass: apply same-swing mods from equipped items
        /// (<c>hero_action_damage</c> / speed / amp), including <c>WHILE_EQUIPPED</c> tag amps.
        /// Must run after cadence consume, before CalculateDamage.
        /// </summary>
        public static bool ApplySameSwingDamageMods(
            Character attacker,
            Actor? target,
            Action swingAction,
            int naturalRoll,
            int attackTotal,
            bool isCombo,
            bool isCritical,
            List<string>? messages = null)
        {
            if (attacker == null || swingAction == null)
                return false;

            var provisional = new CombatEvent(CombatEventType.ActionHit, attacker)
            {
                Target = target ?? attacker,
                Action = swingAction,
                NaturalRollValue = naturalRoll,
                RollValue = attackTotal > 0 ? attackTotal : naturalRoll,
                IsCombo = isCombo,
                IsCritical = isCritical,
                IsMiss = false
            };

            bool any = false;
            messages ??= new List<string>();
            foreach (var item in EnumerateEquipped(attacker))
            {
                if (item == null)
                    continue;

                any |= ApplySameSwingFromItem(
                    item,
                    attacker,
                    target ?? attacker,
                    provisional,
                    messages,
                    SameSwingMechanicIds,
                    allowWhileEquipped: true);
            }

            return any;
        }

        /// <summary>
        /// Pre-roll pass: WHILE_EQUIPPED / same-swing speed and hit-threshold tag amps
        /// (must run after action selection, before thresholds finalize).
        /// </summary>
        public static bool ApplySameSwingPreRollMods(
            Character attacker,
            Actor? target,
            Action swingAction,
            List<string>? messages = null)
        {
            if (attacker == null || swingAction == null)
                return false;

            var provisional = new CombatEvent(CombatEventType.ActionHit, attacker)
            {
                Target = target ?? attacker,
                Action = swingAction,
                IsMiss = false
            };

            bool any = false;
            messages ??= new List<string>();
            var thresholdManager = RollModificationManager.GetThresholdManager();

            foreach (var item in EnumerateEquipped(attacker))
            {
                if (item == null)
                    continue;

                foreach (var bundle in EnumerateSameSwingBundles(item, PreRollSameSwingMechanicIds, includeWhileEquipped: true))
                {
                    // Pre-roll threshold/speed amps are WHILE_EQUIPPED only. Combat WHEN×threshold
                    // bundles (e.g. ONCRITICAL → hero_crit_threshold) deposit after the real event;
                    // applying them here with Value -1 raises the crit bar before the roll resolves.
                    if (!ItemEquipEffectApplicator.IsWhileEquipped(bundle))
                        continue;
                    if (!PassesBundleFilters(bundle, attacker, provisional, swingAction))
                        continue;

                    double mag = ItemTriggerMagnitude.ResolveOrZero(bundle, attacker);
                    if (mag == 0)
                        continue;

                    foreach (string rawId in bundle.ParseMechanicIds())
                    {
                        string id = NormalizeMechanicId(rawId);
                        if (!PreRollSameSwingMechanicIds.Contains(id))
                            continue;

                        if (id == "hero_action_speed")
                        {
                            attacker.Effects.ConsumedSpeedModPercent += mag;
                            messages.Add($"{attacker.Name} gains {mag:+0;-0}% speed this swing.");
                            any = true;
                        }
                        else if (id == "hero_hit_threshold")
                        {
                            thresholdManager.AdjustHitThreshold(attacker, (int)Math.Round(mag));
                            any = true;
                        }
                        else if (id == "hero_combo_threshold")
                        {
                            thresholdManager.AdjustComboThreshold(attacker, (int)Math.Round(mag));
                            any = true;
                        }
                        else if (id == "hero_crit_threshold")
                        {
                            thresholdManager.AdjustCriticalHitThreshold(attacker, (int)Math.Round(mag));
                            any = true;
                        }
                    }
                }
            }

            return any;
        }

        /// <summary>Equipped slots that can carry catalog trigger bundles.</summary>
        public static IEnumerable<Item> EnumerateEquipped(Character character)
        {
            if (character == null)
                yield break;
            if (character.Weapon != null)
                yield return character.Weapon;
            if (character.Head != null)
                yield return character.Head;
            if (character.Body != null)
                yield return character.Body;
            if (character.Legs != null)
                yield return character.Legs;
            if (character.Feet != null)
                yield return character.Feet;
        }

        /// <summary>
        /// Synthetic action holding the item's bundles + filter tokens for gate evaluation.
        /// Magnitudes live on bundle <see cref="ActionTriggerBundle.Value"/>.
        /// </summary>
        public static Action BuildCarrierAction(
            Item item,
            string? onlyMechanic = null,
            bool onlyWhenTakeHit = false)
        {
            var bundles = ItemGenerator.CloneTriggerBundles(item.TriggerBundles) ?? new List<ActionTriggerBundle>();
            if (!string.IsNullOrWhiteSpace(onlyMechanic))
            {
                string want = ActionMechanicsRegistry.NormalizeMechanicId(onlyMechanic);
                bundles = bundles
                    .Where(b => b != null
                        && b.ParseMechanicIds().Any(m => NormalizeMechanicId(m) == want))
                    .ToList()!;
            }
            else if (onlyWhenTakeHit)
            {
                bundles = bundles
                    .Where(b => b != null && IsTakeHitWhen(b.When) && !ItemEquipEffectApplicator.IsWhileEquipped(b))
                    .ToList()!;
            }
            else
            {
                // Combat attacker path: skip WHILE_EQUIPPED and defender-only ONTAKEHIT.
                bundles = bundles
                    .Where(b => !ItemEquipEffectApplicator.IsWhileEquipped(b) && !IsTakeHitWhen(b?.When))
                    .ToList();
            }

            var filters = new List<string>();
            foreach (var b in bundles)
            {
                if (b?.Filters == null)
                    continue;
                foreach (var f in b.Filters)
                {
                    if (string.IsNullOrWhiteSpace(f))
                        continue;
                    if (!filters.Exists(x => string.Equals(x, f, StringComparison.OrdinalIgnoreCase)))
                        filters.Add(f.Trim());
                }
            }

            // Self-buff statuses from gear should hit the wearer, not the swing target
            // (carrier Target defaults to enemy via ResolveStatusEffectRecipient).
            var selfBuffs = new List<string>();
            foreach (var b in bundles)
            {
                if (b == null) continue;
                foreach (string rawId in b.ParseMechanicIds())
                {
                    string id = NormalizeMechanicId(rawId);
                    if (id is "harden" or "focus" or "fortify"
                        && !selfBuffs.Exists(x => string.Equals(x, id, StringComparison.OrdinalIgnoreCase)))
                        selfBuffs.Add(id);
                }
            }

            return new Action
            {
                Name = $"ItemProc:{item.Name}",
                Type = ActionType.Attack,
                Triggers = new ConditionalTriggerProperties
                {
                    Bundles = bundles,
                    TriggerConditions = filters
                },
                Advanced = new AdvancedMechanicsProperties
                {
                    SelfTargetEffects = selfBuffs
                },
                RollMods = new RollModificationProperties()
            };
        }

        private static bool ApplySameSwingFromItem(
            Item item,
            Character attacker,
            Actor target,
            CombatEvent provisional,
            List<string> messages,
            HashSet<string> mechanicIds,
            bool allowWhileEquipped)
        {
            var bundles = EnumerateSameSwingBundles(item, mechanicIds, allowWhileEquipped).ToList();
            if (bundles.Count == 0)
                return false;

            var carrier = new Action
            {
                Name = $"ItemProc:{item.Name}",
                Type = ActionType.Attack,
                Triggers = new ConditionalTriggerProperties
                {
                    Bundles = bundles,
                    TriggerConditions = CollectFilters(bundles)
                },
                Advanced = new AdvancedMechanicsProperties(),
                RollMods = new RollModificationProperties()
            };

            return ActionTriggerBundleApplicator.ApplyMatchingBundles(
                carrier,
                provisional,
                attacker,
                target,
                messages,
                allowWhileEquippedSameSwing: allowWhileEquipped);
        }

        private static IEnumerable<ActionTriggerBundle> EnumerateSameSwingBundles(
            Item item,
            HashSet<string> mechanicIds,
            bool includeWhileEquipped)
        {
            IEnumerable<ActionTriggerBundle?> sources = Enumerable.Empty<ActionTriggerBundle?>();
            if (item.TriggerBundles != null)
                sources = sources.Concat(item.TriggerBundles);
            if (includeWhileEquipped && item.EquipEffects != null)
                sources = sources.Concat(item.EquipEffects);

            foreach (var b in sources)
            {
                if (b == null || !b.IsEnabled)
                    continue;
                bool whileEq = ItemEquipEffectApplicator.IsWhileEquipped(b);
                if (whileEq && !includeWhileEquipped)
                    continue;
                if (!whileEq && includeWhileEquipped
                    && !(item.TriggerBundles?.Contains(b) ?? false))
                {
                    // equip-only WHILE_EQUIPPED already handled; non-while combat bundles from TriggerBundles ok
                }

                if (!b.ParseMechanicIds().Any(m => mechanicIds.Contains(NormalizeMechanicId(m))))
                    continue;

                yield return CloneBundle(b);
            }
        }

        private static ActionTriggerBundle CloneBundle(ActionTriggerBundle b) =>
            new()
            {
                When = b.When ?? "",
                Count = b.Count ?? "1",
                Scope = b.Scope ?? "",
                Mechanics = b.Mechanics ?? "",
                Value = b.Value,
                Filters = b.Filters == null ? null : new List<string>(b.Filters),
                ScaleFrom = b.ScaleFrom
            };

        private static List<string> CollectFilters(IEnumerable<ActionTriggerBundle> bundles)
        {
            var filters = new List<string>();
            foreach (var b in bundles)
            {
                if (b?.Filters == null)
                    continue;
                foreach (var f in b.Filters)
                {
                    if (string.IsNullOrWhiteSpace(f))
                        continue;
                    if (!filters.Exists(x => string.Equals(x, f, StringComparison.OrdinalIgnoreCase)))
                        filters.Add(f.Trim());
                }
            }

            return filters;
        }

        private static bool PassesBundleFilters(
            ActionTriggerBundle bundle,
            Character character,
            CombatEvent evt,
            Action swingAction)
        {
            var carrier = new Action
            {
                Name = $"ItemProc:{character.Name}",
                Type = ActionType.Attack,
                Triggers = new ConditionalTriggerProperties
                {
                    Bundles = new List<ActionTriggerBundle> { bundle },
                    TriggerConditions = bundle.Filters == null
                        ? new List<string>()
                        : new List<string>(bundle.Filters)
                },
                Advanced = new AdvancedMechanicsProperties(),
                RollMods = new RollModificationProperties()
            };
            return ActionTriggerGate.PassesNonOutcomeFilters(carrier, evt);
        }

        private static string NormalizeMechanicId(string raw)
        {
            string id = raw ?? "";
            int c = id.IndexOf(':');
            if (c > 0)
                id = id.Substring(0, c);
            return ActionMechanicsRegistry.NormalizeMechanicId(id);
        }

        private static bool IsTakeHitWhen(string? when)
        {
            string t = ActionTriggerGate.NormalizeToken(when ?? "");
            return t is "ONTAKEHIT" or "ONHEROHURT";
        }
    }
}
