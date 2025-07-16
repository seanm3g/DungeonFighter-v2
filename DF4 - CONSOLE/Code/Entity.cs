namespace RPGGame
{
    public abstract class Entity
    {
        public List<(Action action, double probability)> ActionPool { get; private set; }
        public string Name { get; protected set; }

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

            // Remove any existing action with the same name and combo order
            ActionPool.RemoveAll(a => a.action.Name == action.Name && a.action.ComboOrder == action.ComboOrder);
            ActionPool.Add((action, probability));
        }

        public void RemoveAction(Action action)
        {
            ActionPool.RemoveAll(a => a.action.Name == action.Name && a.action.ComboOrder == action.ComboOrder);
        }

        public Action? SelectAction()
        {
            if (ActionPool.Count == 0)
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