using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Tracks action speed and turn order for combat entities
    /// </summary>
    public class ActionSpeedSystem
    {
        private List<CombatEntity> entities = new List<CombatEntity>();

        public void AddEntity(Actor entity, double baseSpeed)
        {
            double currentTime = GameTicker.Instance.GetCurrentGameTime();
            entities.Add(new CombatEntity
            {
                Entity = entity,
                BaseSpeed = baseSpeed,
                NextActionTime = currentTime // Start ready to act immediately
            });
        }

        public void RemoveEntity(Actor entity)
        {
            entities.RemoveAll(e => e.Entity == entity);
        }

        public Actor? GetNextEntityToAct()
        {
            double currentTime = GameTicker.Instance.GetCurrentGameTime();
            
            // Find the entity with the lowest NextActionTime that is actually ready to act
            // Don't check IsReady flag, just check if NextActionTime <= currentTime
            var readyEntity = entities
                .Where(e => IsEntityAlive(e.Entity) && e.NextActionTime <= currentTime)
                .OrderBy(e => e.NextActionTime)
                .ThenBy(e => e.Entity.Name) // Add secondary sort by name for consistent ordering when times are equal
                .FirstOrDefault();

            return readyEntity?.Entity;
        }

        // Add method to advance an entity's turn even when stunned
        public void AdvanceEntityTurn(Actor entity, double turnDuration = 1.0)
        {
            var combatEntity = entities.FirstOrDefault(e => e.Entity == entity);
            if (combatEntity == null) return;

            // Update the entity's NextActionTime to ensure proper turn alternation
            double currentTime = GameTicker.Instance.GetCurrentGameTime();
            currentTime = Math.Max(currentTime, combatEntity.NextActionTime);
            
            // Add a small buffer to prevent entities with identical speeds from acting simultaneously
            double buffer = 0.01; // 10ms buffer
            combatEntity.NextActionTime = currentTime + turnDuration + buffer;
        }

        public double ExecuteAction(Actor entity, Action action, bool isBasicAttack = false, bool isCriticalMiss = false)
        {
            var combatEntity = entities.FirstOrDefault(e => e.Entity == entity);
            if (combatEntity == null) return 0.0;

            double actionDuration;
            
            // New system: (agility + weapon attack speed) * action length
            if (entity is Character character)
            {
                // For characters: use the new attack speed system
                double attackSpeed = character.GetTotalAttackSpeed();
                
                // Apply critical miss penalty (doubles action speed/recovery time for THIS action)
                if (isCriticalMiss || entity.HasCriticalMissPenalty)
                {
                    attackSpeed *= 2.0;
                }
                
                // Use default length (1.0) for basic attacks, action length for combo actions
                double lengthMultiplier = isBasicAttack ? 1.0 : action.Length;
                actionDuration = attackSpeed * lengthMultiplier;
            }
            else if (entity is Enemy enemy)
            {
                // For enemies: use the BaseSpeed that was set in AddEntity, modified by agility
                double baseSpeed = combatEntity.BaseSpeed;
                double agilityModifier = Math.Max(0.5, 1.0 - (enemy.GetEffectiveAgility() * 0.05)); // Agility reduces action time
                
                // Apply critical miss penalty (doubles action speed/recovery time for THIS action)
                if (isCriticalMiss || entity.HasCriticalMissPenalty)
                {
                    baseSpeed *= 2.0;
                }
                
                actionDuration = baseSpeed * agilityModifier * action.Length;
            }
            else if (entity is Environment environment)
            {
                // For environment: use base speed (environment should have slow, long cooldowns)
                actionDuration = combatEntity.BaseSpeed * action.Length;
            }
            else
            {
                // For any other entities: use a reasonable default
                actionDuration = 1.0 * action.Length;
            }
            
            // Update current time to when this action completes
            double currentTime = GameTicker.Instance.GetCurrentGameTime();
            currentTime = Math.Max(currentTime, combatEntity.NextActionTime);
            combatEntity.NextActionTime = currentTime + actionDuration;
            
            // Clear critical miss penalty after action is executed
            if (entity.HasCriticalMissPenalty)
            {
                entity.HasCriticalMissPenalty = false;
                entity.CriticalMissPenaltyTurns = 0;
            }
            
            return actionDuration;
        }

        public bool IsEntityReady(Actor entity)
        {
            var combatEntity = entities.FirstOrDefault(e => e.Entity == entity);
            if (combatEntity == null) return false;

            double currentTime = GameTicker.Instance.GetCurrentGameTime();
            return combatEntity.NextActionTime <= currentTime;
        }

        public double GetTimeUntilReady(Actor entity)
        {
            var combatEntity = entities.FirstOrDefault(e => e.Entity == entity);
            if (combatEntity == null) return 0.0;

            double currentTime = GameTicker.Instance.GetCurrentGameTime();
            return Math.Max(0.0, combatEntity.NextActionTime - currentTime);
        }

        public void AdvanceTime(double timeAmount)
        {
            GameTicker.Instance.AdvanceGameTime(timeAmount);
        }

        public void Reset()
        {
            entities.Clear();
            GameTicker.Instance.Reset();
        }
        
        public double GetCurrentTime()
        {
            return GameTicker.Instance.GetCurrentGameTime();
        }
        
        public double GetNextReadyTime()
        {
            var nextEntity = entities
                .Where(e => IsEntityAlive(e.Entity))
                .OrderBy(e => e.NextActionTime)
                .FirstOrDefault();
                
            if (nextEntity == null)
                return -1.0; // Return -1 to indicate no entities exist
                
            double currentTime = GameTicker.Instance.GetCurrentGameTime();
            
            // If the next entity is already ready, return a time slightly in the future
            // to prevent infinite loops in the combat system
            if (nextEntity.NextActionTime <= currentTime)
                return currentTime + 0.1;
                
            return nextEntity.NextActionTime;
        }

        public string GetTurnOrderInfo()
        {
            var sortedEntities = entities
                .Where(e => IsEntityAlive(e.Entity))
                .OrderBy(e => e.NextActionTime)
                .ToList();

            var info = new List<string>();
            double currentTime = GameTicker.Instance.GetCurrentGameTime();
            foreach (var entity in sortedEntities)
            {
                double timeUntilReady = Math.Max(0.0, entity.NextActionTime - currentTime);
                string status = timeUntilReady <= 0.0 ? "READY" : $"Ready in {timeUntilReady:F1}s";
                info.Add($"{entity.Entity.Name}: {status}");
            }

            return string.Join(" | ", info);
        }

        private bool IsEntityAlive(Actor entity)
        {
            if (entity is Character character)
                return character.IsAlive;
            else if (entity is Enemy enemy)
                return enemy.IsAlive;
            else
                return true; // Environment and other entities are always "alive"
        }
    }

    public class CombatEntity
    {
        public Actor Entity { get; set; } = null!;
        public double BaseSpeed { get; set; }
        public double NextActionTime { get; set; }
    }
}


