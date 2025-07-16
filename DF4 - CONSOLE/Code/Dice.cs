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