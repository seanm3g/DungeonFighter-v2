using System;
using System.Collections.Generic;
using System.Globalization;

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

        /// <summary>Plain-language effect lines for item tooltips, built from the charm's own multipliers.</summary>
        public static List<string> DescribeEffects(CharmItem charm)
        {
            var lines = new List<string>();
            if (charm == null)
                return lines;

            string layer = (charm.Layer ?? "").Trim();
            if (string.Equals(layer, LayerClass, StringComparison.OrdinalIgnoreCase))
            {
                AddMultiplierLine(lines, charm.AmplifyClassDefense, "Class DEFENSE");
                AddMultiplierLine(lines, charm.AmplifyClassTagDamage, "Damage of actions tagged with your class");
            }
            else if (string.Equals(layer, LayerMaterial, StringComparison.OrdinalIgnoreCase))
            {
                AddMultiplierLine(lines, charm.AmplifyMint, "Material keywords minted");
                AddMultiplierLine(lines, charm.AmplifyConvert, "Material convert payoff");
            }
            else if (string.Equals(layer, LayerAnimal, StringComparison.OrdinalIgnoreCase))
            {
                AddMultiplierLine(lines, charm.AmplifyAnimalLadder,
                    $"Animal set bonuses (needs {AnimalSetController.GeneralThreshold}+ animal gear pieces)");
                if (charm.UnlockAnimalAction)
                    lines.Add($"{AnimalSetController.SpecificThreshold} pieces of the same animal: grants an ANIMAL action");
            }

            if (lines.Count == 0)
            {
                string? catalogText = Data.CharmsLoader.FindByName(charm.Name)?.Description;
                if (!string.IsNullOrWhiteSpace(catalogText))
                    lines.Add(catalogText.Trim());
            }
            return lines;
        }

        private static void AddMultiplierLine(List<string> lines, double mult, string what)
        {
            if (mult <= 0 || Math.Abs(mult - 1.0) < 0.001)
                return;
            lines.Add($"{what} x{mult.ToString("0.##", CultureInfo.InvariantCulture)}");
        }
    }
}
