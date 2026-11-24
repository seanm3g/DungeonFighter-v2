using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RPGGame.Combat.Events;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Listener affix (Documentation/BUILD_SYSTEM_CONDENSED.md §5, template 1): "this piece's material store also
    /// fills on &lt;event&gt;". A prefix <see cref="Modification"/> with <see cref="EffectName"/>; its event
    /// (<see cref="Modification.TriggerWhen"/>) is rolled at loot time from the slot's event family (weapon = hit,
    /// head = dice, body = defense, legs = combo, feet = time). Loot-only (no reroll / craft; decision 3).
    /// Mints <see cref="CombatConfig.ListenerMintShare"/> of the set's normal mint per trigger, only while the
    /// piece's material set is formed (2+ pieces): an item-owned rule (<see cref="AffixTemplates.Rules"/>).
    /// </summary>
    public static class ListenerAffix
    {
        public const string EffectName = "LISTEN";

        public static bool IsListener(Modification? mod) =>
            mod != null && string.Equals(mod.Effect?.Trim(), EffectName, StringComparison.OrdinalIgnoreCase);

        /// <summary>The event family a gear slot listens to; null for items without a slot role.</summary>
        public static TriggerEventFamily? SlotFamily(Item? item) => item switch
        {
            WeaponItem => TriggerEventFamily.Hit,
            HeadItem => TriggerEventFamily.Dice,
            ChestItem => TriggerEventFamily.Defense,
            LegsItem => TriggerEventFamily.Combo,
            FeetItem => TriggerEventFamily.Time,
            _ => null
        };

        /// <summary>Events a Listener on <paramref name="item"/> can roll (its slot family; hub events included).</summary>
        public static IReadOnlyList<string> EventsForSlot(Item? item)
        {
            var family = SlotFamily(item);
            if (family == null)
                return Array.Empty<string>();
            return TriggerEvents.All
                .Where(e => e.Family == family.Value && e.Listenable)
                .Select(e => e.Token)
                .ToList();
        }

        /// <summary>Stamps a rolled event onto a freshly cloned Listener prefix. False when the slot has no family.</summary>
        public static bool RollEvent(Modification mod, Item item, Random random)
        {
            var events = EventsForSlot(item);
            if (events.Count == 0)
                return false;
            string token = events[random.Next(events.Count)];
            mod.TriggerWhen = token;
            mod.Description = Describe(mod, item);
            return true;
        }

        public static string Describe(Modification mod, Item? item = null)
        {
            string when = TriggerEvents.Label(mod.TriggerWhen) ?? mod.TriggerWhen;
            string material = item != null ? MaterialBuildData.CanonicalMaterialName(ItemMaterialRules.RemapLegacyMaterial(item.Material)) : "";
            string keyword = material.Length > 0 ? MaterialBuildsLoader.FindByMaterial(material)?.Keyword ?? "" : "";
            string store = keyword.Length > 0 ? keyword : "this piece's material currency";
            return $"Listening: {when} also mints {store} (set formed).";
        }
    }
}
