using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame.Data;
using RPGGame.UI.Avalonia.Layout;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI
{
    /// <summary>
    /// The "Tags: …" line on item details. Tags that feed one of the hero's builds are highlighted:
    /// a material set or animal family already worn in another slot, or a class the hero has put points into
    /// (class tag, that class's weapon type, or one of its materials once the Core root makes them count).
    /// </summary>
    public static class ItemTagSynergy
    {
        /// <summary>Distinct display tags for <paramref name="item"/> (no "starter" bookkeeping tag).</summary>
        public static List<string> GetDisplayTags(Item? item)
        {
            if (item?.Tags == null)
                return new List<string>();
            return item.Tags
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim().ToLowerInvariant())
                .Where(t => t != "starter")
                .Distinct()
                .ToList();
        }

        /// <summary>True when <paramref name="tag"/> on <paramref name="item"/> feeds one of <paramref name="hero"/>'s builds.</summary>
        public static bool Synergizes(Character? hero, Item item, string tag)
        {
            if (hero == null || hero is Enemy || string.IsNullOrWhiteSpace(tag))
                return false;
            tag = tag.Trim();

            var others = OtherSlotItems(hero, item);

            // Class: class tag or weapon-type tag of a class the hero has points in.
            string? classTag = ClassForTag(tag);
            if (classTag != null && HasClassInvestment(hero, classTag))
                return true;

            // Material set: this piece joins a material worn in another slot.
            if (MatchesItemMaterial(item, tag))
            {
                foreach (var other in others)
                {
                    if (!string.IsNullOrWhiteSpace(other.Material) && MaterialSetController.JoinsSet(hero, item, other.Material))
                        return true;
                }
                // Class material that counts as the class tag (Core root learned) for an invested class.
                string material = ItemMaterialRules.RemapLegacyMaterial(item.Material);
                foreach (string cls in ClassTags.All)
                {
                    if (ClassTags.MaterialsCountAsClass(hero, cls) && HasClassInvestment(hero, cls) &&
                        ClassTags.MaterialsOf(cls).Any(m => string.Equals(m, material, StringComparison.OrdinalIgnoreCase)))
                        return true;
                }
                return false;
            }

            // Animal / taxon / other shared tags: another worn piece carries it.
            if (string.Equals(tag, AnimalTagHelper.AnimalTag, StringComparison.OrdinalIgnoreCase) || AnimalTagHelper.IsTaxon(tag))
                return others.Any(o => o.Tags != null && o.Tags.Any(t => string.Equals(t?.Trim(), tag, StringComparison.OrdinalIgnoreCase)));

            return false;
        }

        /// <summary>Colored "Tags: a, b, c" segments; synergy tags in gold with a ★, the rest gray.</summary>
        public static List<ColoredText> BuildTagsLineSegments(Item item, Character? hero, Color labelColor)
        {
            var builder = new ColoredTextBuilder();
            builder.Add("Tags: ", labelColor);
            var tags = GetDisplayTags(item);
            for (int i = 0; i < tags.Count; i++)
            {
                if (i > 0)
                    builder.Add(", ", ColorPalette.Gray.GetColor());
                bool synergy = Synergizes(hero, item, tags[i]);
                if (synergy)
                    builder.Add("★" + tags[i], ColorPalette.Gold.GetColor());
                else
                    builder.Add(tags[i], ColorPalette.Gray.GetColor());
            }
            return builder.Build();
        }

        private static string? ClassForTag(string tag)
        {
            foreach (string cls in ClassTags.All)
            {
                if (string.Equals(cls, tag, StringComparison.OrdinalIgnoreCase))
                    return cls;
                if (ClassTags.WeaponOf(cls) is WeaponType wt && string.Equals(wt.ToString(), tag, StringComparison.OrdinalIgnoreCase))
                    return cls;
            }
            return null;
        }

        private static bool HasClassInvestment(Character hero, string classTag)
        {
            if (hero.Progression == null)
                return false;
            if (ClassTags.WeaponOf(classTag) is WeaponType wt && hero.Progression.GetClassPoints(wt) > 0)
                return true;
            foreach (string partner in HybridTrees.MergedPartners(hero, classTag))
                if (ClassTags.WeaponOf(partner) is WeaponType pwt && hero.Progression.GetClassPoints(pwt) > 0)
                    return true;
            return false;
        }

        private static bool MatchesItemMaterial(Item item, string tag) =>
            !string.IsNullOrWhiteSpace(item.Material) &&
            (string.Equals(item.Material.Trim(), tag, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(ItemMaterialRules.RemapLegacyMaterial(item.Material), tag, StringComparison.OrdinalIgnoreCase));

        /// <summary>Worn items in slots other than the one <paramref name="item"/> would occupy (and not the item itself).</summary>
        private static List<Item> OtherSlotItems(Character hero, Item item)
        {
            string? slot = InventoryActionPoolEntries.GetEquipSlotForItem(item);
            var replaced = slot != null ? hero.Equipment.GetSlotItem(slot) : null;
            var list = new List<Item>();
            foreach (var worn in MaterialSetController.EnumerateEquipped(hero))
            {
                if (worn == null || ReferenceEquals(worn, item) || ReferenceEquals(worn, replaced))
                    continue;
                list.Add(worn);
            }
            return list;
        }
    }
}
