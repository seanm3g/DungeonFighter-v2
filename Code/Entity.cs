namespace RPGGame
{
    public abstract class Entity
    {
        public List<(Action action, double probability)> ActionPool { get; private set; }
        public string Name { get; set; }
        
        // Weaken debuff system
        public bool IsWeakened { get; set; } = false; // Whether entity is weakened
        public int WeakenTurns { get; set; } = 0; // Number of turns weakened
        public double WeakenMultiplier { get; set; } = 0.5; // Damage reduction when weakened (50% outgoing damage)
        
        // Stun debuff system
        public bool IsStunned { get; set; } = false; // Whether entity is stunned
        public int StunTurnsRemaining { get; set; } = 0; // Number of turns stunned
        
        // Roll penalty system (for effects like Dust Cloud)
        public int RollPenalty { get; set; } = 0; // Penalty to dice rolls
        public int RollPenaltyTurns { get; set; } = 0; // Number of turns the penalty lasts

        protected Entity(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Entity name cannot be null or empty", nameof(name));

            Name = name;
            ActionPool = new List<(Action, double)>();
        }

        public void AddAction(Action action, double probability)
        {
            if (probability < 0 || probability > 1)
                throw new ArgumentException("Probability must be between 0 and 1", nameof(probability));

            // Remove any existing action with the same name
            ActionPool.RemoveAll(a => a.action.Name == action.Name);
            ActionPool.Add((action, probability));
        }

        public virtual void RemoveAction(Action action)
        {
            ActionPool.RemoveAll(a => a.action.Name == action.Name && a.action.ComboOrder == action.ComboOrder);
        }
        
        /// <summary>
        /// Applies a roll penalty to the entity
        /// </summary>
        /// <param name="penalty">Amount to reduce rolls by</param>
        /// <param name="turns">Number of turns the penalty lasts</param>
        public void ApplyRollPenalty(int penalty, int turns)
        {
            RollPenalty = penalty;
            RollPenaltyTurns = turns;
        }

        public Action? SelectAction()
        {
            if (ActionPool.Count == 0)
                return null;

            // Check if entity is stunned (now works for all entities since stun properties are in base class)
            if (IsStunned)
                return null;

            double totalProbability = ActionPool.Sum(a => a.probability);
            double randomValue = new Random().NextDouble() * totalProbability;
            double cumulativeProbability = 0;

            foreach (var (action, probability) in ActionPool)
            {
                cumulativeProbability += probability;
                if (randomValue <= cumulativeProbability)
                    return action;
            }

            // Fallback to the last action if no action was selected
            return ActionPool.Last().action;
        }

        public abstract string GetDescription();

        public override string ToString()
        {
            return $"{Name} - {GetDescription()}";
        }
    }
} 