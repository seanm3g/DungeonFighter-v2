using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Resolves <see cref="WeaponType"/> from base weapon display names in Weapons.json (used when items deserialize as <see cref="Item"/> without a derived <see cref="WeaponItem"/>).
    /// </summary>
    public static class WeaponTypeFromCatalog
    {
        private static List<WeaponData>? _rows;
        private static readonly object Sync = new();

        /// <summary>Exact name match (case-insensitive) to a row in Weapons.json.</summary>
        public static bool TryGetByWeaponName(string? name, out WeaponType weaponType)
        {
            weaponType = WeaponType.Sword;
            if (string.IsNullOrWhiteSpace(name))
                return false;

            EnsureLoaded();
            if (_rows == null || _rows.Count == 0)
                return false;

            var row = _rows.FirstOrDefault(w =>
                !string.IsNullOrWhiteSpace(w.Name)
                && string.Equals(w.Name.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase));
            if (row == null || string.IsNullOrWhiteSpace(row.Type))
                return false;

            return Enum.TryParse(row.Type.Trim(), ignoreCase: true, out weaponType);
        }

        /// <summary>
        /// First <see cref="WeaponData"/> row in <c>Weapons.json</c> list order where <see cref="WeaponData.Tier"/> is 1,
        /// <see cref="WeaponData.Type"/> matches <paramref name="weaponType"/>, and the row includes the <c>starter</c> tag
        /// if any such row exists; otherwise the first tier-1 row for that type (legacy fallback).
        /// </summary>
        public static bool TryGetFirstTierOneCatalogRow(WeaponType weaponType, out WeaponData? row)
        {
            row = null;
            EnsureLoaded();
            if (_rows == null || _rows.Count == 0)
                return false;

            foreach (var w in _rows)
            {
                if (w.Tier != 1)
                    continue;
                if (string.IsNullOrWhiteSpace(w.Type))
                    continue;
                if (!Enum.TryParse(w.Type.Trim(), ignoreCase: true, out WeaponType wt) || wt != weaponType)
                    continue;
                if (!GameDataTagHelper.HasTag(w.Tags, "starter"))
                    continue;
                row = w;
                return true;
            }

            foreach (var w in _rows)
            {
                if (w.Tier != 1)
                    continue;
                if (string.IsNullOrWhiteSpace(w.Type))
                    continue;
                if (!Enum.TryParse(w.Type.Trim(), ignoreCase: true, out WeaponType wt) || wt != weaponType)
                    continue;
                row = w;
                return true;
            }

            return false;
        }

        private static void EnsureLoaded()
        {
            lock (Sync)
            {
                if (_rows != null)
                    return;
                _rows = JsonLoader.LoadJsonList<WeaponData>(GameConstants.WeaponsJson, useCache: true);
            }
        }

        /// <summary>Clears cached <c>Weapons.json</c> rows (test suite may change working directory or GameData discovery order).</summary>
        public static void InvalidateCache()
        {
            lock (Sync)
            {
                _rows = null;
            }
        }
    }
}
