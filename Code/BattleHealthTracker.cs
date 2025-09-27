using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Tracks health milestones and leadership changes during battle
    /// </summary>
    public class BattleHealthTracker
    {
        private Dictionary<Entity, HealthMilestone> entityMilestones;
        private Entity? currentLeader;
        private List<Entity> battleParticipants;
        private List<string> pendingNotifications;
        
        public BattleHealthTracker()
        {
            entityMilestones = new Dictionary<Entity, HealthMilestone>();
            battleParticipants = new List<Entity>();
            pendingNotifications = new List<string>();
        }
        
        /// <summary>
        /// Initialize tracking for a new battle
        /// </summary>
        public void InitializeBattle(List<Entity> participants)
        {
            entityMilestones.Clear();
            battleParticipants.Clear();
            pendingNotifications.Clear();
            battleParticipants.AddRange(participants);
            currentLeader = null;
            
            // Initialize milestones for all participants
            foreach (var entity in participants)
            {
                entityMilestones[entity] = new HealthMilestone();
            }
            
            // Set initial leader
            UpdateLeader();
        }
        
        /// <summary>
        /// Check for health milestones and leadership changes after damage is dealt
        /// </summary>
        public List<string> CheckHealthMilestones(Entity entity, int damageDealt)
        {
            var notifications = new List<string>();
            
            if (!entityMilestones.ContainsKey(entity))
                return notifications;
                
            var milestone = entityMilestones[entity];
            double healthPercentage = 0;
            
            if (entity is Character character)
            {
                healthPercentage = (double)character.CurrentHealth / character.MaxHealth * 100;
            }
            else if (entity is Enemy enemy)
            {
                healthPercentage = (double)enemy.CurrentHealth / enemy.MaxHealth * 100;
            }
            
            // Check for health milestone thresholds
            if (healthPercentage <= 50 && !milestone.Reached50Percent)
            {
                milestone.Reached50Percent = true;
                var message = GetHealthMilestoneMessage(entity, 50);
                notifications.Add(message);
                pendingNotifications.Add(message);
            }
            
            if (healthPercentage <= 20 && !milestone.Reached20Percent)
            {
                milestone.Reached20Percent = true;
                var message = GetHealthMilestoneMessage(entity, 20);
                notifications.Add(message);
                pendingNotifications.Add(message);
            }
            
            if (healthPercentage <= 5 && !milestone.Reached5Percent)
            {
                milestone.Reached5Percent = true;
                var message = GetHealthMilestoneMessage(entity, 5);
                notifications.Add(message);
                pendingNotifications.Add(message);
            }
            
            // Check for leadership change
            var newLeader = GetCurrentLeader();
            if (newLeader != currentLeader)
            {
                if (currentLeader != null && newLeader != null)
                {
                    var message = GetLeadershipChangeMessage(currentLeader, newLeader);
                    notifications.Add(message);
                    pendingNotifications.Add(message);
                }
                currentLeader = newLeader;
            }
            
            return notifications;
        }
        
        /// <summary>
        /// Get and clear pending notifications
        /// </summary>
        public List<string> GetAndClearPendingNotifications()
        {
            var notifications = new List<string>(pendingNotifications);
            pendingNotifications.Clear();
            return notifications;
        }
        
        /// <summary>
        /// Get the current leader (entity with most health)
        /// </summary>
        private Entity? GetCurrentLeader()
        {
            return battleParticipants
                .Where(e => IsEntityAlive(e))
                .OrderByDescending(e => GetEntityHealth(e))
                .FirstOrDefault();
        }
        
        /// <summary>
        /// Get health value for an entity
        /// </summary>
        private int GetEntityHealth(Entity entity)
        {
            if (entity is Character character)
            {
                return character.CurrentHealth;
            }
            else if (entity is Enemy enemy)
            {
                return enemy.CurrentHealth;
            }
            return 0;
        }
        
        /// <summary>
        /// Update the current leader
        /// </summary>
        private void UpdateLeader()
        {
            currentLeader = GetCurrentLeader();
        }
        
        /// <summary>
        /// Generate health milestone message
        /// </summary>
        private string GetHealthMilestoneMessage(Entity entity, int percentage)
        {
            var messages = new Dictionary<int, List<string>>
            {
                [50] = new List<string>
                {
                    $"[{entity.Name}] is showing signs of fatigue!",
                    $"[{entity.Name}]'s movements are becoming sluggish!",
                    $"[{entity.Name}] is starting to struggle!",
                    $"[{entity.Name}] looks wounded but determined!",
                    $"[{entity.Name}] is bleeding but still fighting!"
                },
                [20] = new List<string>
                {
                    $"[{entity.Name}] is severely wounded!",
                    $"[{entity.Name}] is on the brink of collapse!",
                    $"[{entity.Name}]'s life force is fading!",
                    $"[{entity.Name}] is barely standing!",
                    $"[{entity.Name}] is critically injured!"
                },
                [5] = new List<string>
                {
                    $"[{entity.Name}] is at death's door!",
                    $"[{entity.Name}] is moments from defeat!",
                    $"[{entity.Name}]'s final moments approach!",
                    $"[{entity.Name}] is on the verge of death!",
                    $"[{entity.Name}] is fighting for their last breath!"
                }
            };
            
            var messageList = messages[percentage];
            var random = new Random();
            return messageList[random.Next(messageList.Count)];
        }
        
        /// <summary>
        /// Generate leadership change message
        /// </summary>
        private string GetLeadershipChangeMessage(Entity oldLeader, Entity newLeader)
        {
            var messages = new List<string>
            {
                $"[{newLeader.Name}] now has the upper hand in battle!",
                $"[{newLeader.Name}] has gained the advantage!",
                $"[{newLeader.Name}] is now dominating the fight!",
                $"[{newLeader.Name}] has taken control of the battle!",
                $"[{newLeader.Name}] is now leading the combat!",
                $"[{newLeader.Name}] has turned the tide of battle!"
            };
            
            var random = new Random();
            return messages[random.Next(messages.Count)];
        }
        
        /// <summary>
        /// Check if entity is alive
        /// </summary>
        public bool IsEntityAlive(Entity entity)
        {
            if (entity is Character character)
            {
                return character.IsAlive;
            }
            else if (entity is Enemy enemy)
            {
                return enemy.IsAlive;
            }
            return false;
        }
        
        /// <summary>
        /// Get current battle leader
        /// </summary>
        public Entity? GetCurrentBattleLeader()
        {
            return currentLeader;
        }
    }
    
    /// <summary>
    /// Tracks health milestones for an entity
    /// </summary>
    public class HealthMilestone
    {
        public bool Reached50Percent { get; set; } = false;
        public bool Reached20Percent { get; set; } = false;
        public bool Reached5Percent { get; set; } = false;
    }
}
