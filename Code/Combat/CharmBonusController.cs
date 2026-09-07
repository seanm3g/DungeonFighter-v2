using System;

namespace RPGGame
{
    /// <summary>
    /// Reads the equipped charm and supplies layer amplify multipliers.
    /// Charms do not mint currencies; they only scale Class / Material / Animal systems.
    /// </summary>
    public static class CharmBonusController
    {
        public const string LayerClass = "class";
        public const string LayerMaterial = "material";
        public const string LayerAnimal = "animal";

        public static CharmItem? GetEquippedCharm(Character? hero)
        {
            if (hero?.Equipment?.Charm is CharmItem charm)
                return charm;
            return null;
        }

        public static bool IsLayer(Character? hero, string layer)
        {
            var charm = GetEquippedCharm(hero);
            if (charm == null || string.IsNullOrWhiteSpace(layer))
                return false;
            return string.Equals(charm.Layer, layer, StringComparison.OrdinalIgnoreCase);
        }

        public static double GetMintMultiplier(Character? hero)
        {
            var charm = GetEquippedCharm(hero);
            if (charm == null || !IsLayer(hero, LayerMaterial))
                return 1.0;
            return charm.AmplifyMint <= 0 ? 1.0 : charm.AmplifyMint;
        }

        public static double GetConvertMultiplier(Character? hero)
        {
            var charm = GetEquippedCharm(hero);
            if (charm == null || !IsLayer(hero, LayerMaterial))
                return 1.0;
            return charm.AmplifyConvert <= 0 ? 1.0 : charm.AmplifyConvert;
        }

        public static double GetClassDefenseMultiplier(Character? hero)
        {
            var charm = GetEquippedCharm(hero);
            if (charm == null || !IsLayer(hero, LayerClass))
                return 1.0;
            return charm.AmplifyClassDefense <= 0 ? 1.0 : charm.AmplifyClassDefense;
        }

        public static double GetClassTagDamageMultiplier(Character? hero)
        {
            var charm = GetEquippedCharm(hero);
            if (charm == null || !IsLayer(hero, LayerClass))
                return 1.0;
            return charm.AmplifyClassTagDamage <= 0 ? 1.0 : charm.AmplifyClassTagDamage;
        }

        public static double GetAnimalLadderMultiplier(Character? hero)
        {
            var charm = GetEquippedCharm(hero);
            if (charm == null || !IsLayer(hero, LayerAnimal))
                return 1.0;
            return charm.AmplifyAnimalLadder <= 0 ? 1.0 : charm.AmplifyAnimalLadder;
        }

        public static bool UnlocksAnimalActions(Character? hero)
        {
            var charm = GetEquippedCharm(hero);
            return charm != null
                && IsLayer(hero, LayerAnimal)
                && charm.UnlockAnimalAction;
        }
    }
}
