using System;
using System.Collections.Generic;
using System.Linq;

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

        private static void EnsureLoaded()
        {
            lock (Sync)
            {
                if (_rows != null)
                    return;
                string? path = JsonLoader.FindGameDataFile("Weapons.json");
                _rows = path != null
                    ? JsonLoader.LoadJsonList<WeaponData>(path) ?? new List<WeaponData>()
                    : new List<WeaponData>();
            }
        }
    }
}
