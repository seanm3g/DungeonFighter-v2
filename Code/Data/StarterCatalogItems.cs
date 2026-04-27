using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;
using RPGGame.Utils;

namespace RPGGame
{
    /// <summary>
    /// Beginning-of-game gear from catalog JSON rows tagged <c>starter</c> (see <see cref="GameDataTagHelper.HasTag"/>).
    /// </summary>
    public static class StarterCatalogItems
    {
        public const string StarterTag = "starter";

        /// <summary>
        /// Every <see cref="WeaponData"/> row in <c>Weapons.json</c> list order that includes the <c>starter</c> tag and a valid <see cref="WeaponType"/>.
        /// </summary>
        public static List<WeaponData> LoadStarterTaggedWeaponRows()
        {
            var rows = JsonLoader.LoadJsonList<WeaponData>(GameConstants.WeaponsJson, useCache: true);
            var result = new List<WeaponData>();
            foreach (var w in rows)
            {
                if (!GameDataTagHelper.HasTag(w.Tags, StarterTag))
                    continue;
                if (string.IsNullOrWhiteSpace(w.Type))
                    continue;
                if (!Enum.TryParse(w.Type.Trim(), ignoreCase: true, out WeaponType _))
                    continue;
                result.Add(w);
            }

            return result;
        }

        /// <summary>
        /// New-game weapon menu: all <c>starter</c>-tagged weapons sorted by <see cref="ClassPresentationConfig.ClassWeaponOrder"/> (Barbarian→Warrior→Rogue→Wizard paths), then JSON list order within the same type; otherwise one tier-1 row per class path.
        /// </summary>
        public static List<WeaponData> ResolveStarterWeaponMenuCatalogRows()
        {
            var tagged = LoadStarterTaggedWeaponRows();
            if (tagged.Count > 0)
            {
                static int TypeOrderIndex(WeaponData w)
                {
                    if (!Enum.TryParse(w.Type?.Trim(), ignoreCase: true, out WeaponType wt))
                        return int.MaxValue;
                    int ix = Array.IndexOf(ClassPresentationConfig.ClassWeaponOrder, wt);
                    return ix >= 0 ? ix : int.MaxValue;
                }

                return tagged
                    .Select((w, fileIndex) => (w, fileIndex))
                    .OrderBy(x => TypeOrderIndex(x.w))
                    .ThenBy(x => x.fileIndex)
                    .Select(x => x.w)
                    .ToList();
            }

            var fallback = new List<WeaponData>();
            foreach (var wt in ClassPresentationConfig.ClassWeaponOrder)
            {
                if (WeaponTypeFromCatalog.TryGetFirstTierOneCatalogRow(wt, out var row) && row != null)
                    fallback.Add(row);
            }

            return fallback;
        }

        /// <summary>
        /// At most one generated piece per JSON slot (<c>head</c>, <c>chest</c>, <c>feet</c>), using the first matching row in file order.
        /// </summary>
        public static List<Item> LoadStarterArmorItems()
        {
            var rows = JsonLoader.LoadJsonList<ArmorData>(GameConstants.ArmorJson, useCache: true);
            if (rows == null || rows.Count == 0)
                return new List<Item>();

            var filledSlots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<Item>();
            foreach (var row in rows)
            {
                if (!GameDataTagHelper.HasTag(row.Tags, StarterTag))
                    continue;
                string slotKey = (row.Slot ?? "").Trim().ToLowerInvariant();
                if (slotKey != "head" && slotKey != "chest" && slotKey != "feet")
                    continue;
                if (!filledSlots.Add(slotKey))
                    continue;
                result.Add(ItemGenerator.GenerateArmorItem(row));
            }

            return result;
        }

        public static string GetEquipmentSlotKey(Item armorItem) => armorItem switch
        {
            HeadItem => "head",
            ChestItem => "body",
            FeetItem => "feet",
            _ => "head"
        };
    }
}
