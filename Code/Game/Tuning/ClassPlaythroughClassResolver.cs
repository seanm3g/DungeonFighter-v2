using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Tuning
{
    public static class ClassPlaythroughClassResolver
    {
        public static IReadOnlyList<(WeaponType WeaponType, int MenuSlot, string DisplayName)> ResolveAllClasses()
        {
            var presentation = GameConfiguration.Instance?.ClassPresentation ?? new ClassPresentationConfig();
            var result = new List<(WeaponType, int, string)>();

            for (int i = 0; i < ClassPresentationConfig.ClassWeaponOrder.Length; i++)
            {
                var weaponType = ClassPresentationConfig.ClassWeaponOrder[i];
                result.Add((weaponType, i + 1, presentation.GetDisplayName(weaponType)));
            }

            return result;
        }

        public static IReadOnlyList<(WeaponType WeaponType, int MenuSlot, string DisplayName)> ResolveClasses(string? classesCsv)
        {
            var all = ResolveAllClasses();
            if (string.IsNullOrWhiteSpace(classesCsv))
                return all;

            var tokens = classesCsv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(t => t.Trim())
                .Where(t => t.Length > 0)
                .ToList();

            if (tokens.Count == 0)
                return all;

            var resolved = new List<(WeaponType, int, string)>();
            foreach (var token in tokens)
            {
                var match = TryResolveToken(token, all);
                if (match == null)
                    throw new ArgumentException($"Unknown class or weapon path: '{token}'. Expected Barbarian, Warrior, Rogue, Wizard, Mace, Sword, Dagger, Wand, or menu slot 1-4.");
                if (!resolved.Any(r => r.Item1 == match.Value.WeaponType))
                    resolved.Add(match.Value);
            }

            return resolved;
        }

        private static (WeaponType WeaponType, int MenuSlot, string DisplayName)? TryResolveToken(
            string token,
            IReadOnlyList<(WeaponType WeaponType, int MenuSlot, string DisplayName)> all)
        {
            if (int.TryParse(token, out int slot) && slot >= 1 && slot <= all.Count)
                return all[slot - 1];

            foreach (var entry in all)
            {
                if (token.Equals(entry.DisplayName, StringComparison.OrdinalIgnoreCase))
                    return entry;
            }

            if (Enum.TryParse<WeaponType>(token, ignoreCase: true, out var weaponType))
            {
                var match = all.FirstOrDefault(a => a.WeaponType == weaponType);
                if (match != default && all.Any(a => a.WeaponType == weaponType))
                    return match;
            }

            return null;
        }
    }
}
