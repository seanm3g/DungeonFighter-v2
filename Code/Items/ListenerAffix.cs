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
    /// piece's material set is formed (2+ pieces).
    /// </summary>
    public static class ListenerAffix
    {
        public const string EffectName = "LISTEN";

        /// <summary>Events that need an argument to mean anything; Listener never rolls them.</summary>
        private static readonly HashSet<string> NeedsArg = new(StringComparer.OrdinalIgnoreCase) { "ONNATURALROLL", "ONROLLVALUE" };

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
                .Where(e => e.Family == family.Value && !NeedsArg.Contains(e.Token))
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

        /// <summary>Equipped pieces with a Listener and their listened event.</summary>
        public static IEnumerable<(Item Item, Modification Mod)> Equipped(Character hero)
        {
            foreach (var item in MaterialSetController.EnumerateEquipped(hero))
            {
                if (item?.Modifications == null)
                    continue;
                foreach (var mod in item.Modifications)
                    if (IsListener(mod) && !string.IsNullOrWhiteSpace(mod.TriggerWhen))
                        yield return (item, mod);
            }
        }

        /// <summary>Combat-pipeline events (ONCRITICAL, ONMISS, ONTAKEHIT, …): called from material minting.</summary>
        /// <param name="whenMatches">Already gated for take-hit vs own events, aliases included (MaterialSetController).</param>
        internal static bool MintFromCombatEvent(Character hero, Func<string, bool> whenMatches, bool takeHitOnly)
        {
            bool any = false;
            foreach (var (item, mod) in Equipped(hero).ToList())
            {
                if (TriggerEvents.TryGet(mod.TriggerWhen, out var def) && !def.Emitted)
                    continue; // hub events come through MintFromHub
                if (whenMatches(mod.TriggerWhen))
                    any |= Mint(hero, item, mod);
            }
            return any;
        }

        /// <summary>Hub events (ONBEAT, ONCASH, ONSTATUSAPPLY, …).</summary>
        public static void MintFromHub(Character hero, string token, string? arg)
        {
            foreach (var (item, mod) in Equipped(hero).ToList())
                if (HeroEventHub.Matches(mod.TriggerWhen, token, arg))
                    Mint(hero, item, mod);
        }

        private static bool Mint(Character hero, Item item, Modification mod)
        {
            string material = MaterialBuildData.CanonicalMaterialName(ItemMaterialRules.RemapLegacyMaterial(item.Material));
            var build = material.Length > 0 ? MaterialBuildsLoader.FindByMaterial(material) : null;
            if (build == null || string.IsNullOrWhiteSpace(build.Keyword))
                return false;
            int full = MaterialSetController.ComputeMintPerTrigger(hero, build);
            if (full <= 0)
                return false; // set not formed
            double share = GameConfiguration.Instance.Combat.ListenerMintShare;
            int amount = Math.Max(1, (int)Math.Round(full * share * Math.Max(1.0, mod.RolledValue)));
            string label = TriggerEvents.Label(mod.TriggerWhen) ?? mod.TriggerWhen;
            MaterialSetController.MintKeyword(hero, build.Keyword, amount, $"{item.Name} · Listening: {label}");
            return true;
        }
    }
}
