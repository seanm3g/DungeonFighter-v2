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

        public Environment(string name, string description, bool isHostile, string theme, string roomType = "")
            : base(name)
        {
            Description = description;
            IsHostile = isHostile;
            Theme = theme;
            RoomType = roomType;

            // Initialize specialized managers
            actionInitializer = new EnvironmentalActionInitializer(theme, roomType);
            enemyGenerator = new EnemyGenerationManager(theme, isHostile);
            combatStateManager = new EnvironmentCombatStateManager();
            effectManager = new EnvironmentEffectManager();

            InitializeActions();
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
        public void GenerateEnemies(int roomLevel, List<string>? possibleEnemies = null, int? minLevel = null, int? maxLevel = null)
        {
            enemyGenerator.GenerateEnemies(roomLevel, possibleEnemies, minLevel, maxLevel);
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
            combatStateManager.ResetForNewFight();
        }

        /// <summary>
        /// Checks if the environment should act during combat.
        /// </summary>
        public bool ShouldEnvironmentAct()
        {
            return combatStateManager.ShouldEnvironmentAct(IsHostile);
        }

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


