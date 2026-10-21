using System;
using System.Linq;
using Avalonia.Media;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Layout;
using RPGGame.UI.ColorSystem.Themes;

namespace RPGGame
{
    /// <summary>
    /// One color rule for action names everywhere (strip card titles, SEQUENCE / POOL / INVENTORY rows, tooltip titles):
    /// class actions use their weapon-path color (Barbarian red, Warrior green, Rogue yellow, Wizard cyan), hybrid class
    /// actions purple, everything else its ACTIONS-sheet rarity color.
    /// </summary>
    public static class ActionNameColors
    {
        public enum Source { Rarity, Class, Hybrid, Basic }

        private static bool HasTag(Action action, string tag) =>
            action.Tags != null && action.Tags.Any(t => string.Equals(t?.Trim(), tag, StringComparison.OrdinalIgnoreCase));

        /// <summary>Class key from tags (barbarian / warrior / rogue / wizard), or null.</summary>
        public static string? ClassTag(Action? action)
        {
            if (action == null)
                return null;
            foreach (var c in new[] { "barbarian", "warrior", "rogue", "wizard" })
                if (HasTag(action, c))
                    return c;
            return null;
        }

        public static string RarityLabel(Action? action)
        {
            if (action == null || string.IsNullOrWhiteSpace(action.Name))
                return "";
            string rarity = (ActionLoader.GetActionData(action.Name)?.Rarity ?? "").Trim();
            if (rarity.Length > 0)
                return rarity;
            foreach (var r in new[] { "legendary", "epic", "rare", "uncommon", "common" })
                if (HasTag(action, r))
                    return char.ToUpperInvariant(r[0]) + r.Substring(1);
            return "";
        }

        public static Source Classify(Action? action)
        {
            if (action == null)
                return Source.Basic;
            if (HasTag(action, "hybrid"))
                return Source.Hybrid;
            if (ClassTag(action) != null)
                return Source.Class;
            if (RarityLabel(action).Length > 0)
                return Source.Rarity;
            return Source.Basic;
        }

        public static Color Resolve(Action? action)
        {
            if (action == null)
                return AsciiArtAssets.Colors.White;
            switch (Classify(action))
            {
                case Source.Hybrid:
                    return AsciiArtAssets.Colors.Purple;
                case Source.Class:
                    return ClassTag(action) switch
                    {
                        "barbarian" => PrimaryStatRowHighlightColors.ForWeaponPath(WeaponType.Mace),
                        "warrior" => PrimaryStatRowHighlightColors.ForWeaponPath(WeaponType.Sword),
                        "rogue" => PrimaryStatRowHighlightColors.ForWeaponPath(WeaponType.Dagger),
                        "wizard" => PrimaryStatRowHighlightColors.ForWeaponPath(WeaponType.Wand),
                        _ => AsciiArtAssets.Colors.White
                    };
                case Source.Rarity:
                    return ItemThemeProvider.GetRarityColor(RarityLabel(action));
                default:
                    return AsciiArtAssets.Colors.White;
            }
        }

        /// <summary>"Barbarian class action", "Hybrid class action", "Rare action", "Weapon basic" … for tooltips.</summary>
        public static string Describe(Action? action)
        {
            if (action == null)
                return "";
            string rarity = RarityLabel(action);
            string withRarity(string s) => rarity.Length > 0 ? $"{s} · {rarity}" : s;
            return Classify(action) switch
            {
                Source.Hybrid => withRarity("Hybrid class action"),
                Source.Class => withRarity(char.ToUpperInvariant(ClassTag(action)![0]) + ClassTag(action)!.Substring(1) + " class action"),
                Source.Rarity => $"{rarity} action",
                _ => HasTag(action, "weapon_basic") || HasTag(action, "startingweapon") ? "Weapon basic" : ""
            };
        }

        /// <summary>Lookup by name (inventory pool rows only know the action name).</summary>
        public static Color ResolveByName(string? actionName)
        {
            if (string.IsNullOrWhiteSpace(actionName))
                return AsciiArtAssets.Colors.White;
            var action = ActionLoader.GetAction(actionName);
            return Resolve(action);
        }
    }
}
