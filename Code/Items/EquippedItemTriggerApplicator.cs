using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Actions.Conditional;
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
        /// Pre-damage pass: apply <c>hero_action_damage</c> same-swing mods from equipped items
        /// (e.g. mirror IFSAMESACTION +100%). Must run after cadence consume, before CalculateDamage.
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
                if (item?.TriggerBundles == null)
                    continue;
                bool hasSameSwing = item.TriggerBundles.Any(b =>
                    b != null
                    && b.IsEnabled
                    && !ItemEquipEffectApplicator.IsWhileEquipped(b)
                    && b.ParseMechanicIds().Any(m =>
                    {
                        string id = m.Contains(':') ? m.Substring(0, m.IndexOf(':')) : m;
                        return ActionMechanicsRegistry.NormalizeMechanicId(id) == "hero_action_damage";
                    }));
                if (!hasSameSwing)
                    continue;

                var carrier = BuildCarrierAction(item, onlyMechanic: "hero_action_damage");
                any |= ActionTriggerBundleApplicator.ApplyMatchingBundles(
                    carrier, provisional, attacker, target ?? attacker, messages);
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
        public static Action BuildCarrierAction(Item item, string? onlyMechanic = null)
        {
            var bundles = ItemGenerator.CloneTriggerBundles(item.TriggerBundles) ?? new List<ActionTriggerBundle>();
            if (!string.IsNullOrWhiteSpace(onlyMechanic))
            {
                string want = ActionMechanicsRegistry.NormalizeMechanicId(onlyMechanic);
                bundles = bundles
                    .Where(b => b != null
                        && b.ParseMechanicIds().Any(m =>
                        {
                            string id = m.Contains(':') ? m.Substring(0, m.IndexOf(':')) : m;
                            return ActionMechanicsRegistry.NormalizeMechanicId(id) == want;
                        }))
                    .ToList()!;
            }
            else
            {
                bundles = bundles.Where(b => !ItemEquipEffectApplicator.IsWhileEquipped(b)).ToList();
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

            return new Action
            {
                Name = $"ItemProc:{item.Name}",
                Type = ActionType.Attack,
                Triggers = new ConditionalTriggerProperties
                {
                    Bundles = bundles,
                    TriggerConditions = filters
                },
                Advanced = new AdvancedMechanicsProperties(),
                RollMods = new RollModificationProperties()
            };
        }
    }
}
