using System.Collections.Generic;

namespace RPGGame
{
    public class Dice
    {
        private static readonly Random _random = new Random();

        /// <summary>
        /// Rolls X dice with Y sides each (XdY)
        /// </summary>
        /// <param name="numberOfDice">X - Number of dice to roll</param>
        /// <param name="sides">Y - Number of sides on each die</param>
        /// <returns>Sum of all dice rolls</returns>
        public static int Roll(int numberOfDice, int sides)
        {
            if (numberOfDice < 1)
                throw new ArgumentException("Number of dice must be at least 1", nameof(numberOfDice));
            if (sides < 2)
                throw new ArgumentException("Dice must have at least 2 sides", nameof(sides));

            int total = 0;
            for (int i = 0; i < numberOfDice; i++)
            {
                total += _random.Next(1, sides + 1);
            }
            return total;
        }

        /// <summary>
        /// Rolls a single die with Y sides
        /// </summary>
        /// <param name="sides">Number of sides on the die</param>
        /// <returns>Result of the die roll</returns>
        public static int Roll(int sides)
        {
            return Roll(1, sides);
        }

        /// <summary>
        /// New dice mechanics for combo system:
        /// 1-5: Fail at attack
        /// 6-15: Normal attack
        /// 16-20: Combo attack (triggers combo mode)
        /// </summary>
        /// <param name="bonus">Bonus to add to the roll</param>
        /// <returns>DiceResult containing the roll, success status, and combo trigger</returns>
        public static DiceResult RollComboAction(int bonus = 0)
        {
            int roll = Roll(20) + bonus;
            
            if (roll <= 5)
            {
                return new DiceResult(roll, false, false, "Fail");
            }
            else if (roll <= 15)
            {
                return new DiceResult(roll, true, false, "Normal Attack");
            }
            else
            {
                return new DiceResult(roll, true, true, "Combo Attack");
            }
        }

        /// <summary>
        /// Roll for continuing a combo sequence (once combo mode is triggered)
        /// 11+ continues the combo, 10 or below fails
        /// </summary>
        /// <param name="bonus">Bonus to add to the roll</param>
        /// <returns>DiceResult containing the roll and success status</returns>
        public static DiceResult RollComboContinue(int bonus = 0)
        {
            int roll = Roll(20) + bonus;
            bool success = roll >= 11;
            
            return new DiceResult(roll, success, false, success ? "Combo Continue" : "Combo Fail");
        }

        /// <summary>
        /// Roll for item modifications based on item tier and quality
        /// Higher tier items have better chances for good modifications
        /// </summary>
        /// <param name="itemTier">Tier of the item (1-10+)</param>
        /// <param name="bonus">Additional bonus to the roll</param>
        /// <returns>Modification roll result (1-24)</returns>
        public static int RollModification(int itemTier = 1, int bonus = 0)
        {
            // Base roll on d24 for modification table
            int baseRoll = Roll(24);
            
            // Higher tier items get bonuses to roll better modifications
            int tierBonus = Math.Max(0, itemTier - 1); // Tier 1 = +0, Tier 2 = +1, etc.
            
            int finalRoll = Math.Min(24, baseRoll + tierBonus + bonus);
            
            return finalRoll;
        }

        /// <summary>
        /// Roll multiple modifications for an item
        /// Higher tier items can get multiple modifications
        /// </summary>
        /// <param name="itemTier">Tier of the item</param>
        /// <param name="itemRarity">Rarity of the item (affects number of mods)</param>
        /// <returns>List of modification roll results</returns>
        public static List<int> RollMultipleModifications(int itemTier = 1, string itemRarity = "Common")
        {
            List<int> modifications = new List<int>();
            
            // Determine number of modifications based on rarity
            int modCount = itemRarity.ToLower() switch
            {
                "common" => 1,
                "uncommon" => Roll(2), // 1-2 mods
                "rare" => Roll(2) + 1, // 2-3 mods
                "epic" => Roll(3) + 1, // 2-4 mods
                "legendary" => Roll(3) + 2, // 3-5 mods
                "mythic" => Roll(4) + 2, // 3-6 mods
                _ => 1
            };
            
            for (int i = 0; i < modCount; i++)
            {
                int modRoll = RollModification(itemTier);
                
                // Handle reroll effect (Divine modification)
                if (modRoll == 24) // Divine modification
                {
                    // Add 3 to subsequent rolls and roll again
                    modifications.Add(modRoll);
                    int rerollResult = RollModification(itemTier, 3);
                    modifications.Add(rerollResult);
                }
                else
                {
                    modifications.Add(modRoll);
                }
            }
            
            return modifications;
        }

        /// <summary>
        /// Roll for loot drop chance based on enemy level and player level
        /// </summary>
        /// <param name="enemyLevel">Level of defeated enemy</param>
        /// <param name="playerLevel">Level of player</param>
        /// <returns>True if loot should drop</returns>
        public static bool RollLootDrop(int enemyLevel, int playerLevel)
        {
            // Base 60% chance for loot drop
            double baseChance = 0.6;
            
            // Higher level enemies drop loot more often
            double levelBonus = (enemyLevel - playerLevel) * 0.05;
            
            // Cap the bonus/penalty
            levelBonus = Math.Max(-0.3, Math.Min(0.3, levelBonus));
            
            double finalChance = baseChance + levelBonus;
            
            return _random.NextDouble() < finalChance;
        }
    }

    /// <summary>
    /// Result of a dice roll with additional context
    /// </summary>
    public class DiceResult
    {
        public int Roll { get; }
        public bool Success { get; }
        public bool ComboTriggered { get; }
        public string Description { get; }

        public DiceResult(int roll, bool success, bool comboTriggered, string description)
        {
            Roll = roll;
            Success = success;
            ComboTriggered = comboTriggered;
            Description = description;
        }

        public override string ToString()
        {
            return $"Roll: {Roll}, Success: {Success}, Combo Triggered: {ComboTriggered}, Type: {Description}";
        }
    }
} 