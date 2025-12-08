using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;

namespace RPGGame.Actions.RollModification
{
    /// <summary>
    /// Handles rolling multiple dice and selecting results (take lowest, highest, average)
    /// </summary>
    public static class MultiDiceRoller
    {
        /// <summary>
        /// Enum for how to combine multiple dice rolls
        /// </summary>
        public enum DiceSelectionMode
        {
            TakeLowest,
            TakeHighest,
            TakeAverage,
            Sum // Standard sum of all dice
        }

        /// <summary>
        /// Rolls multiple dice and returns result based on selection mode
        /// </summary>
        /// <param name="numberOfDice">Number of dice to roll</param>
        /// <param name="sides">Number of sides per die</param>
        /// <param name="mode">How to combine the dice results</param>
        /// <returns>The selected result</returns>
        public static int RollMultipleDice(int numberOfDice, int sides, DiceSelectionMode mode)
        {
            if (numberOfDice < 1)
                throw new ArgumentException("Number of dice must be at least 1", nameof(numberOfDice));
            if (sides < 2)
                throw new ArgumentException("Dice must have at least 2 sides", nameof(sides));

            // Roll all dice
            var rolls = new List<int>();
            for (int i = 0; i < numberOfDice; i++)
            {
                rolls.Add(Dice.Roll(sides));
            }

            // Apply selection mode
            return mode switch
            {
                DiceSelectionMode.TakeLowest => rolls.Min(),
                DiceSelectionMode.TakeHighest => rolls.Max(),
                DiceSelectionMode.TakeAverage => (int)Math.Round(rolls.Average()),
                DiceSelectionMode.Sum => rolls.Sum(),
                _ => rolls.Sum()
            };
        }

        /// <summary>
        /// Rolls multiple dice and returns all individual results
        /// </summary>
        public static List<int> RollMultipleDiceResults(int numberOfDice, int sides)
        {
            if (numberOfDice < 1)
                throw new ArgumentException("Number of dice must be at least 1", nameof(numberOfDice));
            if (sides < 2)
                throw new ArgumentException("Dice must have at least 2 sides", nameof(sides));

            var rolls = new List<int>();
            for (int i = 0; i < numberOfDice; i++)
            {
                rolls.Add(Dice.Roll(sides));
            }
            return rolls;
        }
    }
}

