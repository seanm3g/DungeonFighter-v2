using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RPGGame.Actions.Conditional;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Evaluates <c>WHILE_EQUIPPED</c> bundles on equipped gear (always-on channel).
    /// Combat WHENs stay on <see cref="EquippedItemTriggerApplicator"/>.
    /// </summary>
    public static class ItemEquipEffectApplicator
    {
        public const string WhileEquippedWhen = "WHILE_EQUIPPED";

        /// <summary>Flat armor from WHILE_EQUIPPED armor mechanics (after filters).</summary>
        public static int GetEquippedArmorBonus(Character character)
        {
            int total = 0;
            foreach (var effect in EnumerateActiveEquipEffects(character))
            {
                foreach (string rawId in effect.Bundle.ParseMechanicIds())
                {
                    string id = ActionMechanicsRegistry.NormalizeMechanicId(StripArg(rawId, out _));
                    if (id is "armor" or "hero_armor")
                        total += (int)Math.Round(ItemTriggerMagnitude.ResolveOrZero(effect.Bundle, character));
                }
            }
            return total;
        }

        /// <summary>Flat primary-attribute (or named stat) bonuses from WHILE_EQUIPPED hero_stat_bonus.</summary>
        public static int GetEquippedStatBonus(Character character, string statType)
        {
            if (character == null || string.IsNullOrWhiteSpace(statType))
                return 0;
            string want = CanonicalStat(statType);
            int total = 0;
            foreach (var effect in EnumerateActiveEquipEffects(character))
            {
                foreach (string rawId in effect.Bundle.ParseMechanicIds())
                {
                    string id = ActionMechanicsRegistry.NormalizeMechanicId(StripArg(rawId, out string? arg));
                    if (id is not ("hero_stat_bonus" or "stat_bonus"))
                        continue;
                    string resolved = ResolveStatArg(character, arg);
                    if (!StatsMatch(want, resolved))
                        continue;
                    total += (int)Math.Round(ItemTriggerMagnitude.ResolveOrZero(effect.Bundle, character));
                }
            }
            return total;
        }

        /// <summary>Rebuild <see cref="Character.EquipmentGrantedActionTags"/> from equipped grant_action_tag mechanics.</summary>
        public static void RefreshGrantedActionTags(Character character)
        {
            if (character == null)
                return;
            character.EquipmentGrantedActionTags.Clear();
            foreach (var effect in EnumerateActiveEquipEffects(character))
            {
                foreach (string rawId in effect.Bundle.ParseMechanicIds())
                {
                    string id = ActionMechanicsRegistry.NormalizeMechanicId(StripArg(rawId, out string? arg));
                    if (id is not "grant_action_tag" || string.IsNullOrWhiteSpace(arg))
                        continue;
                    character.EquipmentGrantedActionTags.Add(arg.Trim());
                }
            }
        }

        /// <summary>Action names granted by WHILE_EQUIPPED grant_action (e.g. unarmed PUNCH HARD).</summary>
        public static IReadOnlyList<string> GetGrantedActionNames(Character character)
        {
            var names = new List<string>();
            foreach (var effect in EnumerateActiveEquipEffects(character))
            {
                foreach (string rawId in effect.Bundle.ParseMechanicIds())
                {
                    string id = ActionMechanicsRegistry.NormalizeMechanicId(StripArg(rawId, out string? arg));
                    if (id is not "grant_action" || string.IsNullOrWhiteSpace(arg))
                        continue;
                    string name = arg.Trim();
                    if (!names.Exists(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase)))
                        names.Add(name);
                }
            }
            return names;
        }

        /// <summary>All WHILE_EQUIPPED bundles that pass filters for the current loadout.</summary>
        public static IEnumerable<(Item Item, ActionTriggerBundle Bundle)> EnumerateActiveEquipEffects(Character character)
        {
            if (character == null)
                yield break;

            foreach (var item in EquippedItemTriggerApplicator.EnumerateEquipped(character))
            {
                if (item == null)
                    continue;
                foreach (var bundle in EnumerateEquipBundles(item))
                {
                    if (!PassesEquipFilters(character, item, bundle))
                        continue;
                    yield return (item, bundle);
                }
            }
        }

        public static IEnumerable<ActionTriggerBundle> EnumerateEquipBundles(Item item)
        {
            if (item == null)
                yield break;
            if (item.EquipEffects != null)
            {
                foreach (var b in item.EquipEffects)
                {
                    if (IsWhileEquipped(b))
                        yield return b;
                }
            }
            if (item.TriggerBundles != null)
            {
                foreach (var b in item.TriggerBundles)
                {
                    if (IsWhileEquipped(b))
                        yield return b;
                }
            }
        }

        public static bool IsWhileEquipped(ActionTriggerBundle? bundle)
        {
            if (bundle == null || !bundle.IsEnabled)
                return false;
            string when = ActionTriggerGate.NormalizeToken(bundle.When ?? "");
            return when is "WHILEEQUIPPED" or "ONEQUIP";
        }

        private static bool PassesEquipFilters(Character character, Item item, ActionTriggerBundle bundle)
        {
            var filters = new List<(string Family, string? Arg)>();
            if (bundle.Filters != null)
            {
                foreach (var raw in bundle.Filters)
                {
                    if (string.IsNullOrWhiteSpace(raw))
                        continue;
                    if (ActionTriggerPredicates.TryClassifyFilter(raw, out string family, out string? arg))
                        filters.Add((family, arg));
                }
            }

            // Carrier is unused for equip filters that need swing; pass a stub with item name.
            var stub = new Action { Name = item.Name ?? "Equip", Tags = item.Tags?.ToList() ?? new List<string>() };
            // Synthetic event so GearHasTag / IFUNARMED see the character as source.
            var evt = new Combat.Events.CombatEvent(Combat.Events.CombatEventType.ActionHit, character)
            {
                Action = stub,
                Target = character
            };
            return ActionTriggerPredicates.AllFiltersPass(filters, evt, stub);
        }

        private static string StripArg(string rawId, out string? arg)
        {
            arg = null;
            if (string.IsNullOrWhiteSpace(rawId))
                return "";
            int colon = rawId.IndexOf(':');
            if (colon <= 0)
                return rawId.Trim();
            arg = rawId.Substring(colon + 1).Trim();
            return rawId.Substring(0, colon).Trim();
        }

        private static string CanonicalStat(string statType)
        {
            string u = statType.Trim().ToUpperInvariant().Replace(" ", "").Replace("_", "");
            return u switch
            {
                "STR" or "STRENGTH" => "STRENGTH",
                "AGI" or "AGILITY" => "AGILITY",
                "TEC" or "TECH" or "TECHNIQUE" => "TECHNIQUE",
                "INT" or "INTELLIGENCE" => "INTELLIGENCE",
                "PRIMARY" => "PRIMARY",
                "ARMOR" => "ARMOR",
                _ => u
            };
        }

        private static bool StatsMatch(string requestedNormalized, string resolvedCanon)
        {
            string a = CanonicalStat(requestedNormalized);
            string b = CanonicalStat(resolvedCanon);
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveStatArg(Character character, string? arg)
        {
            string canon = CanonicalStat(arg ?? "PRIMARY");
            if (canon != "PRIMARY")
                return canon;
            // Primary from equipped weapon path (Mace→STR, Sword→…); unarmed → STRENGTH.
            if (character.Weapon is WeaponItem w)
            {
                return w.WeaponType switch
                {
                    WeaponType.Mace => "STRENGTH",
                    WeaponType.Sword => "STRENGTH",
                    WeaponType.Dagger => "AGILITY",
                    WeaponType.Wand => "INTELLIGENCE",
                    _ => "STRENGTH"
                };
            }
            return "STRENGTH";
        }
    }
}
