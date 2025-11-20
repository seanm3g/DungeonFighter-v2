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
        private Dictionary<Actor, HealthMilestone> entityMilestones;
        private Actor? currentLeader;
        private List<Actor> battleParticipants;
        private List<string> pendingNotifications;
        
        public BattleHealthTracker()
        {
            entityMilestones = new Dictionary<Actor, HealthMilestone>();
            battleParticipants = new List<Actor>();
            pendingNotifications = new List<string>();
        }
        
        /// <summary>
        /// Initialize tracking for a new battle
        /// </summary>
        public void InitializeBattle(List<Actor> participants)
        {
            entityMilestones.Clear();
            battleParticipants.Clear();
            pendingNotifications.Clear();
            battleParticipants.AddRange(participants);
            currentLeader = null;
            
            // Initialize milestones for all participants
            foreach (var Actor in participants)
            {
                entityMilestones[Actor] = new HealthMilestone();
            }
            
            // Set initial leader
            UpdateLeader();
        }
        
        /// <summary>
        /// Check for health milestones and leadership changes after damage is dealt
        /// </summary>
        public List<string> CheckHealthMilestones(Actor Actor, int damageDealt)
        {
            var notifications = new List<string>();
            
            if (!entityMilestones.ContainsKey(Actor))
                return notifications;
                
            var milestone = entityMilestones[Actor];
            double healthPercentage = 0;
            
            if (Actor is Character character)
            {
                healthPercentage = (double)character.CurrentHealth / character.MaxHealth * 100;
            }
            else if (Actor is Enemy enemy)
            {
                healthPercentage = (double)enemy.CurrentHealth / enemy.MaxHealth * 100;
            }
            
            // Check for health milestone thresholds
            if (healthPercentage <= 50 && !milestone.Reached50Percent)
            {
                milestone.Reached50Percent = true;
                var message = GetHealthMilestoneMessage(Actor, 50);
                notifications.Add(message);
                pendingNotifications.Add(message);
            }
            
            if (healthPercentage <= 20 && !milestone.Reached20Percent)
            {
                milestone.Reached20Percent = true;
                var message = GetHealthMilestoneMessage(Actor, 20);
                notifications.Add(message);
                pendingNotifications.Add(message);
            }
            
            if (healthPercentage <= 5 && !milestone.Reached5Percent)
            {
                milestone.Reached5Percent = true;
                var message = GetHealthMilestoneMessage(Actor, 5);
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
        /// Get the current leader (Actor with most health)
        /// </summary>
        private Actor? GetCurrentLeader()
        {
            return battleParticipants
                .Where(e => IsEntityAlive(e))
                .OrderByDescending(e => GetEntityHealth(e))
                .FirstOrDefault();
        }
        
        /// <summary>
        /// Get health value for an Actor
        /// </summary>
        private int GetEntityHealth(Actor Actor)
        {
            if (Actor is Character character)
            {
                return character.CurrentHealth;
            }
            else if (Actor is Enemy enemy)
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
        private string GetHealthMilestoneMessage(Actor Actor, int percentage)
        {
            var messages = new Dictionary<int, List<string>>
            {
                [50] = new List<string>
                {
                    $"[{Actor.Name}] is showing signs of fatigue!",
                    $"[{Actor.Name}]'s movements are becoming sluggish!",
                    $"[{Actor.Name}] is starting to struggle!",
                    $"[{Actor.Name}] looks wounded but determined!",
                    $"[{Actor.Name}] is bleeding but still fighting!"
                },
                [20] = new List<string>
                {
                    $"[{Actor.Name}] is severely wounded!",
                    $"[{Actor.Name}] is on the brink of collapse!",
                    $"[{Actor.Name}]'s life force is fading!",
                    $"[{Actor.Name}] is barely standing!",
                    $"[{Actor.Name}] is critically injured!"
                },
                [5] = new List<string>
                {
                    $"[{Actor.Name}] is at death's door!",
                    $"[{Actor.Name}] is moments from defeat!",
                    $"[{Actor.Name}]'s final moments approach!",
                    $"[{Actor.Name}] is on the verge of death!",
                    $"[{Actor.Name}] is fighting for their last breath!"
                }
            };
            
            var messageList = messages[percentage];
            var random = new Random();
            return messageList[random.Next(messageList.Count)];
        }
        
        /// <summary>
        /// Generate leadership change message
        /// </summary>
        private string GetLeadershipChangeMessage(Actor oldLeader, Actor newLeader)
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
        /// Check if Actor is alive
        /// </summary>
        public bool IsEntityAlive(Actor Actor)
        {
            if (Actor is Character character)
            {
                return character.IsAlive;
            }
            else if (Actor is Enemy enemy)
            {
                return enemy.IsAlive;
            }
            return false;
        }
        
        /// <summary>
        /// Get current battle leader
        /// </summary>
        public Actor? GetCurrentBattleLeader()
        {
            return currentLeader;
        }
    }
    
    /// <summary>
    /// Tracks health milestones for an Actor
    /// </summary>
    public class HealthMilestone
    {
        public bool Reached50Percent { get; set; } = false;
        public bool Reached20Percent { get; set; } = false;
        public bool Reached5Percent { get; set; } = false;
    }
}



