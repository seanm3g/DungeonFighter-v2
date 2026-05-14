using Avalonia.Media;
using RPGGame;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Left-panel STR/AGI/TECH/INT row highlight for the weapon-path primary (+3 on level-up):
    /// Barbarian (mace) red, Warrior (sword) green, Rogue (dagger) yellow, Wizard (wand) cyan.
    /// </summary>
    public static class PrimaryStatRowHighlightColors
    {
        /// <summary>Dominant class path, then equipped weapon if no path points yet; unknown paths keep legacy purple.</summary>
        public static Color ForCharacter(Character character)
        {
            WeaponType? path = character.Progression.GetPrimaryClassWeaponType()
                ?? (character.Weapon is WeaponItem wi ? wi.WeaponType : (WeaponType?)null);
            return ForWeaponPath(path);
        }

        public static Color ForWeaponPath(WeaponType? path) =>
            path switch
            {
                WeaponType.Mace => AsciiArtAssets.Colors.Red,
                WeaponType.Sword => AsciiArtAssets.Colors.Green,
                WeaponType.Dagger => AsciiArtAssets.Colors.Yellow,
                WeaponType.Wand => AsciiArtAssets.Colors.Cyan,
                _ => AsciiArtAssets.Colors.Purple,
            };
    }
}
