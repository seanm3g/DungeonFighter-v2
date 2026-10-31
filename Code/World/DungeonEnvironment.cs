using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    public enum PassiveEffectType
    {
        None,
        DamageMultiplier, // e.g., -10% damage
        SpeedMultiplier   // e.g., +25% attack speed
    }

    /// <summary>
    /// Represents a room/environment in a dungeon with enemies, effects, and environmental actions.
    /// Uses a facade pattern to delegate specialized responsibilities to focused managers.
    /// </summary>
    public class Environment : Actor
    {
        public string Description { get; private set; }
        public bool IsHostile { get; private set; }
        public string Theme { get; private set; }
        public string RoomType { get; private set; }

        /// <summary>Match tags from room data or theme fallback (elements, environment states).</summary>
        public IReadOnlyList<string> Tags => _tags;

        /// <summary>Flat threshold shift from unstable room field (+4, -2, 2, 0).</summary>
        public int UnstableThresholdMod { get; private set; }

        /// <summary>
        /// The room's trigger face (1-20): when any fighter's d20 lands on it, the room takes a bonus environmental
        /// action right after that swing. 0 when the room has no environmental actions.
        /// </summary>
        public int TriggerRoll { get; private set; }

        /// <summary>Who rolled <see cref="TriggerRoll"/> on the swing that hasn't been answered yet (null when none).</summary>
        private Actor? _pendingRollTriggerBy;

        private readonly List<string> _tags = new List<string>();

        internal void SetTriggerRoll(int face) => TriggerRoll = face is >= 1 and <= 20 ? face : 0;

        /// <summary>Called with each swing's resolved d20 face; arms the room's bonus action when it hits <see cref="TriggerRoll"/>.</summary>
        public void NotifyCombatRoll(Actor roller, int face)
        {
            if (TriggerRoll > 0 && face == TriggerRoll && roller != null && !ReferenceEquals(roller, this))
                _pendingRollTriggerBy ??= roller;
        }

        public bool HasPendingRollTrigger => _pendingRollTriggerBy != null;

        /// <summary>Takes the pending trigger (if a swing rolled <see cref="TriggerRoll"/>) and clears it.</summary>
        public bool TryConsumeRollTrigger(out Actor? rolledBy)
        {
            rolledBy = _pendingRollTriggerBy;
            _pendingRollTriggerBy = null;
            return rolledBy != null;
        }

        internal void SetUnstableThresholdMod(int mod) => UnstableThresholdMod = mod;

        internal void SetTags(IEnumerable<string>? tags)
        {
            _tags.Clear();
            if (tags == null)
                return;
            foreach (var tag in tags)
            {
                if (!string.IsNullOrWhiteSpace(tag) &&
                    !_tags.Any(t => string.Equals(t, tag.Trim(), StringComparison.OrdinalIgnoreCase)))
                    _tags.Add(tag.Trim());
            }
        }

        // Backward compatibility properties for passive effects
        public PassiveEffectType PassiveEffectType
        {
            get => effectManager.PassiveEffectType;
            set => effectManager.PassiveEffectType = value;
        }

        public double PassiveEffectValue
        {
            get => effectManager.PassiveEffectValue;
            set => effectManager.PassiveEffectValue = value;
        }

        // Specialized managers using facade pattern
        private readonly EnvironmentalActionInitializer actionInitializer;
        private readonly EnemyGenerationManager enemyGenerator;
        private readonly EnvironmentCombatStateManager combatStateManager;
        private readonly EnvironmentEffectManager effectManager;

        public Environment(string name, string description, bool isHostile, string theme, string roomType = "", IReadOnlyList<RoomEnemyData>? roomEnemySpawnPool = null, Random? random = null)
            : base(name)
        {
            Description = description;
            IsHostile = isHostile;
            Theme = theme;
            RoomType = roomType;

            // Initialize specialized managers
            actionInitializer = new EnvironmentalActionInitializer(theme, roomType);
            enemyGenerator = new EnemyGenerationManager(theme, isHostile, roomEnemySpawnPool, random);
            combatStateManager = new EnvironmentCombatStateManager();
            effectManager = new EnvironmentEffectManager();

            InitializeActions();
            if (ActionPool.Count > 0)
                TriggerRoll = (random ?? RandomUtility.Current).Next(1, 21);
        }

        private void InitializeActions()
        {
            var actions = actionInitializer.InitializeActions();
            foreach (var (action, probability) in actions)
            {
                AddAction(action, probability);
            }
        }


        /// <summary>
        /// Generates enemies for this environment. Delegates to EnemyGenerationManager.
        /// </summary>
        public void GenerateEnemies(
            int roomLevel,
            List<string>? possibleEnemies = null,
            int? minLevel = null,
            int? maxLevel = null,
            EnemySpawnContext? spawnContext = null,
            TravelRegion? resolvedRegion = null,
            Random? random = null)
        {
            if (random != null)
                enemyGenerator.SetRandom(random);
            enemyGenerator.GenerateEnemies(roomLevel, possibleEnemies, minLevel, maxLevel, spawnContext, resolvedRegion);
        }

        /// <summary>
        /// Gets all enemies in this environment.
        /// </summary>
        public List<Enemy> GetEnemies()
        {
            return enemyGenerator.Enemies;
        }

        /// <summary>
        /// Checks if the environment has any living enemies.
        /// </summary>
        public bool HasLivingEnemies()
        {
            return enemyGenerator.HasLivingEnemies;
        }

        /// <summary>
        /// Gets the next living enemy, or null if none remain.
        /// </summary>
        public Enemy? GetNextLivingEnemy()
        {
            return enemyGenerator.GetNextLivingEnemy;
        }

        /// <summary>
        /// Removes dead enemies from this environment.
        /// This ensures dead enemies don't persist and cause issues during room transitions.
        /// </summary>
        public void RemoveDeadEnemies()
        {
            enemyGenerator.RemoveDeadEnemies();
        }

        /// <summary>
        /// Resets the environment combat state for a new fight.
        /// </summary>
        public void ResetForNewFight()
        {
            combatStateManager.ConfigureActivityFromTags(_tags);
            combatStateManager.ResetForNewFight();
            _pendingRollTriggerBy = null;
        }

        /// <summary>
        /// Checks if the environment should act during combat.
        /// </summary>
        public bool ShouldEnvironmentAct()
        {
            return combatStateManager.ShouldEnvironmentAct(IsHostile);
        }

        /// <summary>Lowest d20 face on which the environment's next turn acts (21 = won't act), for the combat board.</summary>
        public int StrikeThreshold => combatStateManager.GetStrikeThreshold(IsHostile);

        /// <summary>Face and threshold of the room's last act roll, for the combat board's quiet card.</summary>
        public int LastStrikeRoll => combatStateManager.LastRoll;
        public int LastStrikeRollThreshold => combatStateManager.LastThreshold;

        public override string GetDescription()
        {
            var allEnemies = enemyGenerator.Enemies;
            string enemyInfo = "";
            if (allEnemies.Any())
            {
                var livingEnemies = allEnemies.Count(e => e.IsAlive);
                enemyInfo = $"\nThere are {livingEnemies} enemies present.";
            }
            return $"{Description}{enemyInfo}";
        }

        public override string ToString()
        {
            return $"{Name}: {GetDescription()}";
        }

        /// <summary>
        /// Applies a passive effect to a value. Delegates to EnvironmentEffectManager.
        /// </summary>
        public double ApplyPassiveEffect(double value)
        {
            return effectManager.ApplyPassiveEffect(value);
        }

        /// <summary>
        /// Applies the active effect to player and enemy. Delegates to EnvironmentEffectManager.
        /// </summary>
        public void ApplyActiveEffect(Character player, Enemy enemy)
        {
            effectManager.ApplyActiveEffect(player, enemy);
        }

        /// <summary>
        /// Gets the passive effect type.
        /// </summary>
        public PassiveEffectType GetPassiveEffectType()
        {
            return effectManager.PassiveEffectType;
        }

        /// <summary>
        /// Gets the passive effect value.
        /// </summary>
        public double GetPassiveEffectValue()
        {
            return effectManager.PassiveEffectValue;
        }

        /// <summary>
        /// Sets a passive effect on this environment.
        /// </summary>
        public void SetPassiveEffect(PassiveEffectType type, double value)
        {
            effectManager.SetPassiveEffect(type, value);
        }

        /// <summary>
        /// Gets the active effect action.
        /// </summary>
        public Action? GetActiveEffectAction()
        {
            return effectManager.ActiveEffectAction;
        }

        /// <summary>
        /// Sets an active effect action.
        /// </summary>
        public void SetActiveEffectAction(Action action)
        {
            effectManager.SetActiveEffectAction(action);
        }
    }

} 


