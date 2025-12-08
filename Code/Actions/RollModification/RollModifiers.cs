using System;
using RPGGame;

namespace RPGGame.Actions.RollModification
{
    /// <summary>
    /// Concrete roll modifier implementations
    /// </summary>

    /// <summary>
    /// Adds or subtracts a flat value to the roll
    /// </summary>
    public class AdditiveRollModifier : IRollModifier
    {
        private readonly int _value;
        public int Priority => 100; // Low priority - applied after multipliers
        public string Name { get; }

        public AdditiveRollModifier(string name, int value)
        {
            Name = name;
            _value = value;
        }

        public int ModifyRoll(int baseRoll, RollModificationContext context)
        {
            return baseRoll + _value;
        }
    }

    /// <summary>
    /// Multiplies the roll by a factor
    /// </summary>
    public class MultiplicativeRollModifier : IRollModifier
    {
        private readonly double _multiplier;
        public int Priority => 200; // Medium priority - applied before additive
        public string Name { get; }

        public MultiplicativeRollModifier(string name, double multiplier)
        {
            Name = name;
            _multiplier = multiplier;
        }

        public int ModifyRoll(int baseRoll, RollModificationContext context)
        {
            return (int)Math.Round(baseRoll * _multiplier);
        }
    }

    /// <summary>
    /// Clamps the roll between min and max values
    /// </summary>
    public class ClampRollModifier : IRollModifier
    {
        private readonly int _min;
        private readonly int _max;
        public int Priority => 300; // High priority - applied last
        public string Name { get; }

        public ClampRollModifier(string name, int min, int max)
        {
            Name = name;
            _min = min;
            _max = max;
        }

        public int ModifyRoll(int baseRoll, RollModificationContext context)
        {
            return Math.Max(_min, Math.Min(_max, baseRoll));
        }
    }

    /// <summary>
    /// Re-rolls the same action if condition is met
    /// </summary>
    public class RerollModifier : IRollModifier
    {
        private readonly double _chance;
        private readonly Func<RollModificationContext, bool>? _condition;
        public int Priority => 50; // Very low priority - applied first
        public string Name { get; }

        public RerollModifier(string name, double chance, Func<RollModificationContext, bool>? condition = null)
        {
            Name = name;
            _chance = chance;
            _condition = condition;
        }

        public int ModifyRoll(int baseRoll, RollModificationContext context)
        {
            // Check condition if provided
            if (_condition != null && !_condition(context))
                return baseRoll;

            // Roll for reroll chance
            var random = new Random();
            if (random.NextDouble() < _chance)
            {
                // Reroll - return new roll
                return Dice.Roll(20);
            }

            return baseRoll;
        }
    }

    /// <summary>
    /// Exploding dice - if roll exceeds threshold, add another roll
    /// </summary>
    public class ExplodingDiceModifier : IRollModifier
    {
        private readonly int _threshold;
        public int Priority => 150; // Medium-low priority
        public string Name { get; }

        public ExplodingDiceModifier(string name, int threshold)
        {
            Name = name;
            _threshold = threshold;
        }

        public int ModifyRoll(int baseRoll, RollModificationContext context)
        {
            int total = baseRoll;
            int currentRoll = baseRoll;
            int maxExplosions = 10; // Prevent infinite loops

            for (int i = 0; i < maxExplosions && currentRoll >= _threshold; i++)
            {
                int additionalRoll = Dice.Roll(20);
                total += additionalRoll;
                currentRoll = additionalRoll;
            }

            return total;
        }
    }
}

