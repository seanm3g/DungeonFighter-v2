using System;
using System.Text.RegularExpressions;
using RPGGame.Combat.Calculators;
using RPGGame.Entity.Services;
using RPGGame.UI.ColorSystem;

namespace RPGGame
{
    /// <summary>
    /// Applies the death-screen clone rules to an existing character.
    /// </summary>
    public static class CharacterCloneService
    {
        private static readonly string[] EquipmentSlots = { "head", "body", "legs", "weapon", "feet" };

        public static void CloneAfterDeath(Character character)
        {
            if (character == null)
                throw new ArgumentNullException(nameof(character));

            character.Name = GetNextCloneName(character.Name);
            DiscardEquippedGear(character);

            character.ClearAllTempEffects();
            character.Effects.RerollCharges = character.Equipment.GetTotalRerollCharges();
            character.CurrentHealth = character.GetEffectiveMaxHealth();
            character.ResetCombo();
            CharacterSerializer.RebuildCharacterActions(character);
            character.ResetSessionStatistics();

            DamageCalculator.InvalidateCache(character);
            KeywordColorSystem.RegisterCharacterName(character.Name, EntityColorHelper.GetActorColor(character));
        }

        public static string GetNextCloneName(string? currentName)
        {
            string name = string.IsNullOrWhiteSpace(currentName) ? "Clone" : currentName.Trim();
            string[] parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return "Clone Jr.";

            string suffixToken = parts[^1].TrimEnd('.');
            if (TryParseGenerationSuffix(suffixToken, out int generation))
            {
                string baseName = string.Join(' ', parts, 0, parts.Length - 1).Trim();
                if (string.IsNullOrWhiteSpace(baseName))
                    baseName = "Clone";
                return $"{baseName} {FormatGenerationSuffix(generation + 1)}";
            }

            return $"{name} Jr.";
        }

        private static void DiscardEquippedGear(Character character)
        {
            foreach (string slot in EquipmentSlots)
                character.Equipment.UnequipItem(slot);
        }

        private static bool TryParseGenerationSuffix(string token, out int generation)
        {
            generation = 0;
            if (string.Equals(token, "Jr", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(token, "Junior", StringComparison.OrdinalIgnoreCase))
            {
                generation = 2;
                return true;
            }

            if (!Regex.IsMatch(token, "^[IVXLCDM]+$", RegexOptions.IgnoreCase))
                return false;

            generation = RomanToInt(token.ToUpperInvariant());
            return generation >= 2;
        }

        private static string FormatGenerationSuffix(int generation)
        {
            return generation <= 2 ? "Jr." : IntToRoman(generation);
        }

        private static int RomanToInt(string roman)
        {
            int total = 0;
            int previous = 0;

            for (int i = roman.Length - 1; i >= 0; i--)
            {
                int current = RomanValue(roman[i]);
                if (current < previous)
                    total -= current;
                else
                    total += current;
                previous = current;
            }

            return total;
        }

        private static int RomanValue(char c)
        {
            return c switch
            {
                'I' => 1,
                'V' => 5,
                'X' => 10,
                'L' => 50,
                'C' => 100,
                'D' => 500,
                'M' => 1000,
                _ => 0
            };
        }

        private static string IntToRoman(int value)
        {
            if (value <= 0)
                return string.Empty;

            (int Amount, string Numeral)[] numerals =
            {
                (1000, "M"),
                (900, "CM"),
                (500, "D"),
                (400, "CD"),
                (100, "C"),
                (90, "XC"),
                (50, "L"),
                (40, "XL"),
                (10, "X"),
                (9, "IX"),
                (5, "V"),
                (4, "IV"),
                (1, "I")
            };

            var result = new System.Text.StringBuilder();
            foreach (var (amount, numeral) in numerals)
            {
                while (value >= amount)
                {
                    result.Append(numeral);
                    value -= amount;
                }
            }

            return result.ToString();
        }
    }
}
