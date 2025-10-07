using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    public class BattleEvent
    {
        public string Actor { get; set; } = "";
        public string Target { get; set; } = "";
        public string Action { get; set; } = "";
        public int Damage { get; set; }
        public bool IsSuccess { get; set; }
        public bool IsCombo { get; set; }
        public int ComboStep { get; set; }
        public bool CausesBleed { get; set; }
        public bool CausesWeaken { get; set; }
        public bool IsHeal { get; set; }
        public int HealAmount { get; set; }
        public double ComboAmplifier { get; set; }
        public string EnvironmentEffect { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public int ActorHealthBefore { get; set; }
        public int TargetHealthBefore { get; set; }
        public int ActorHealthAfter { get; set; }
        public int TargetHealthAfter { get; set; }
        public int Roll { get; set; }
        public int Difficulty { get; set; }
        public bool IsCritical { get; set; }

        public BattleEvent()
        {
            Timestamp = DateTime.Now;
        }
    }

    public class BattleNarrative
    {
        private List<BattleEvent> events;
        private string playerName;
        private string enemyName;
        private string environmentName;
        private int initialPlayerHealth;
        private int initialEnemyHealth;
        private int finalPlayerHealth;
        private int finalEnemyHealth;
        private List<string> narrativeEvents;
        private List<string> pendingNarrativeEvents;
        private bool firstBloodOccurred;
        private bool healthReversalOccurred;
        private bool goodComboOccurred;
        private bool playerBelow50Percent;
        private bool enemyBelow50Percent;
        private bool playerBelow10Percent;
        private bool enemyBelow10Percent;
        private bool criticalHitOccurred;
        private bool playerDefeated;
        private bool enemyDefeated;

        public BattleNarrative(string playerName, string enemyName, string environmentName = "", int playerHealth = 0, int enemyHealth = 0)
        {
            this.playerName = playerName;
            this.enemyName = enemyName;
            this.environmentName = environmentName;
            this.initialPlayerHealth = playerHealth;
            this.initialEnemyHealth = enemyHealth;
            this.finalPlayerHealth = playerHealth;
            this.finalEnemyHealth = enemyHealth;
            this.events = new List<BattleEvent>();
            this.narrativeEvents = new List<string>();
            this.pendingNarrativeEvents = new List<string>();
            this.firstBloodOccurred = false;
            this.healthReversalOccurred = false;
            this.goodComboOccurred = false;
            this.playerBelow50Percent = false;
            this.enemyBelow50Percent = false;
            this.playerBelow10Percent = false;
            this.enemyBelow10Percent = false;
            this.criticalHitOccurred = false;
            this.playerDefeated = false;
            this.enemyDefeated = false;
        }

        public void AddEvent(BattleEvent evt)
        {
            events.Add(evt);
            
            // Update final health based on damage/healing
            if (evt.Actor == playerName && evt.Target == enemyName && evt.Damage > 0)
            {
                finalEnemyHealth = Math.Max(0, finalEnemyHealth - evt.Damage);
            }
            else if (evt.Actor == enemyName && evt.Target == playerName && evt.Damage > 0)
            {
                finalPlayerHealth = Math.Max(0, finalPlayerHealth - evt.Damage);
            }
            else if (evt.IsHeal && evt.Target == playerName)
            {
                finalPlayerHealth += evt.HealAmount;
            }

            // Check for significant events that trigger narrative
            CheckForSignificantEvents(evt);
        }
        
        /// <summary>
        /// Gets the narratives that were triggered by the last event
        /// </summary>
        /// <returns>List of triggered narrative messages</returns>
        public List<string> GetTriggeredNarratives()
        {
            var triggeredNarratives = new List<string>();
            
            // Get the last event to check for triggered narratives
            if (events.Count > 0)
            {
                var lastEvent = events[events.Count - 1];
                triggeredNarratives = CheckForSignificantEvents(lastEvent);
            }
            
            return triggeredNarratives;
        }

        private List<string> CheckForSignificantEvents(BattleEvent evt)
        {
            var settings = GameSettings.Instance;
            var triggeredNarratives = new List<string>();
            
            // First Blood - first successful hit that deals damage
            if (!firstBloodOccurred && evt.Damage > 0 && evt.IsSuccess)
            {
                firstBloodOccurred = true;
                string narrative = GetRandomNarrative("firstBlood");
                narrativeEvents.Add(narrative);
                
                // Add to triggered narratives for immediate display
                if (settings.EnableNarrativeEvents)
                {
                    triggeredNarratives.Add(narrative);
                }
            }

            // Critical Hit - when a critical hit occurs
            if (!criticalHitOccurred && evt.IsCritical)
            {
                criticalHitOccurred = true;
                string narrative = GetRandomNarrative("criticalHit");
                narrativeEvents.Add(narrative);
                
                // Add to triggered narratives for immediate display
                if (settings.EnableNarrativeEvents)
                {
                    triggeredNarratives.Add(narrative);
                }
            }

            // Health Recovery - when someone heals
            if (evt.IsHeal && evt.HealAmount > 0)
            {
                string narrative = GetRandomNarrative("healthRecovery").Replace("{name}", evt.Target);
                narrativeEvents.Add(narrative);
                
                // Add to triggered narratives for immediate display
                if (settings.EnableNarrativeEvents)
                {
                    triggeredNarratives.Add(narrative);
                }
            }

            // Health Reversal - when someone goes from higher to lower health than opponent
            if (!healthReversalOccurred)
            {
                var playerHealthPercentage = (double)finalPlayerHealth / initialPlayerHealth;
                var enemyHealthPercentage = (double)finalEnemyHealth / initialEnemyHealth;
                
                // Check if there was a reversal in health advantage
                if (evt.Actor == playerName && evt.Damage > 0)
                {
                    // Player was behind in health percentage but now has advantage
                    if (playerHealthPercentage < enemyHealthPercentage && finalPlayerHealth > finalEnemyHealth)
                    {
                        healthReversalOccurred = true;
                        string narrative = $"{playerName} turns the tide! Their relentless assault has brought {enemyName} to their knees!";
                        narrativeEvents.Add(narrative);
                        
                        // Add to triggered narratives for immediate display
                        if (settings.EnableNarrativeEvents)
                        {
                            triggeredNarratives.Add(narrative);
                        }
                    }
                }
                else if (evt.Actor == enemyName && evt.Damage > 0)
                {
                    // Enemy was behind in health percentage but now has advantage
                    if (enemyHealthPercentage < playerHealthPercentage && finalEnemyHealth > finalPlayerHealth)
                    {
                        healthReversalOccurred = true;
                        string narrative = $"{enemyName} seizes control! Their brutal counterattack has {playerName} reeling!";
                        narrativeEvents.Add(narrative);
                        
                        // Add to triggered narratives for immediate display
                        if (settings.EnableNarrativeEvents)
                        {
                            triggeredNarratives.Add(narrative);
                        }
                    }
                }
            }

            // Below 50% Health - when someone gets below 50% health
            var playerHealthPercentage50 = (double)finalPlayerHealth / initialPlayerHealth;
            var enemyHealthPercentage50 = (double)finalEnemyHealth / initialEnemyHealth;
            
            if (!playerBelow50Percent && playerHealthPercentage50 < 0.5 && finalPlayerHealth > 0)
            {
                playerBelow50Percent = true;
                string narrative = GetRandomNarrative("below50Percent").Replace("{name}", playerName);
                narrativeEvents.Add(narrative);
                
                // Add to triggered narratives for immediate display
                if (settings.EnableNarrativeEvents)
                {
                    triggeredNarratives.Add(narrative);
                }
            }
            
            if (!enemyBelow50Percent && enemyHealthPercentage50 < 0.5 && finalEnemyHealth > 0)
            {
                enemyBelow50Percent = true;
                string narrative = GetRandomNarrative("below50Percent").Replace("{name}", enemyName);
                narrativeEvents.Add(narrative);
                
                // Add to triggered narratives for immediate display
                if (settings.EnableNarrativeEvents)
                {
                    triggeredNarratives.Add(narrative);
                }
            }

            // Below 10% Health - when someone gets below 10% health
            if (!playerBelow10Percent && playerHealthPercentage50 < 0.1 && finalPlayerHealth > 0)
            {
                playerBelow10Percent = true;
                string narrative = GetRandomNarrative("below10Percent").Replace("{name}", playerName);
                narrativeEvents.Add(narrative);
                
                // Add to triggered narratives for immediate display
                if (settings.EnableNarrativeEvents)
                {
                    triggeredNarratives.Add(narrative);
                }
            }
            
            if (!enemyBelow10Percent && enemyHealthPercentage50 < 0.1 && finalEnemyHealth > 0)
            {
                enemyBelow10Percent = true;
                string narrative = GetRandomNarrative("below10Percent").Replace("{name}", enemyName);
                narrativeEvents.Add(narrative);
                
                // Add to triggered narratives for immediate display
                if (settings.EnableNarrativeEvents)
                {
                    triggeredNarratives.Add(narrative);
                }
            }

            // Good Combo - when someone gets a 3+ step combo
            if (!goodComboOccurred && evt.IsCombo && evt.ComboStep >= 2)
            {
                goodComboOccurred = true;
                string narrative;
                if (evt.Actor == playerName)
                {
                    narrative = $"{playerName} unleashes a devastating combo sequence! Each strike flows into the next with deadly precision!";
                }
                else
                {
                    narrative = $"{enemyName} demonstrates masterful technique with a brutal combo that leaves {playerName} reeling!";
                }
                narrativeEvents.Add(narrative);
                
                // Add to triggered narratives for immediate display
                if (settings.EnableNarrativeEvents)
                {
                    triggeredNarratives.Add(narrative);
                }
            }

            // Defeat Events - when someone is defeated
            if (!playerDefeated && finalPlayerHealth <= 0)
            {
                playerDefeated = true;
                string narrative = GetRandomNarrative("playerDefeated");
                narrativeEvents.Add(narrative);
                
                // Add to triggered narratives for immediate display
                if (settings.EnableNarrativeEvents)
                {
                    triggeredNarratives.Add(narrative);
                }
            }
            
            if (!enemyDefeated && finalEnemyHealth <= 0)
            {
                enemyDefeated = true;
                string narrative = GetRandomNarrative("enemyDefeated").Replace("{name}", enemyName);
                narrativeEvents.Add(narrative);
                
                // Add to triggered narratives for immediate display
                if (settings.EnableNarrativeEvents)
                {
                    triggeredNarratives.Add(narrative);
                }
            }
            
            return triggeredNarratives;
        }

        public void EndBattle()
        {
            // Battle has ended - this method is kept for compatibility
        }

        /// <summary>
        /// Updates the final health values from actual entities
        /// </summary>
        public void UpdateFinalHealth(int playerHealth, int enemyHealth)
        {
            finalPlayerHealth = playerHealth;
            finalEnemyHealth = enemyHealth;
        }



        /// <summary>
        /// Gets a random narrative text from FlavorText.json for the specified event type
        /// </summary>
        /// <param name="eventType">The type of narrative event (firstBlood, criticalHit, etc.)</param>
        /// <returns>A random narrative string for the event type</returns>
        private string GetRandomNarrative(string eventType)
        {
            try
            {
                var flavorData = FlavorText.GetData();
                
                // Use reflection to get the combat narratives
                var combatNarrativesProperty = flavorData.GetType().GetProperty("CombatNarratives");
                if (combatNarrativesProperty == null)
                {
                    return GetFallbackNarrative(eventType);
                }

                var combatNarratives = combatNarrativesProperty.GetValue(flavorData);
                if (combatNarratives == null)
                {
                    return GetFallbackNarrative(eventType);
                }

                // Get the specific event type array
                var eventProperty = combatNarratives.GetType().GetProperty(eventType);
                if (eventProperty == null)
                {
                    return GetFallbackNarrative(eventType);
                }

                var narratives = eventProperty.GetValue(combatNarratives) as string[];
                if (narratives == null || narratives.Length == 0)
                {
                    return GetFallbackNarrative(eventType);
                }

                // Return a random narrative
                var random = new Random();
                return narratives[random.Next(narratives.Length)];
            }
            catch (Exception)
            {
                return GetFallbackNarrative(eventType);
            }
        }

        /// <summary>
        /// Provides fallback narrative text when FlavorText.json is not available
        /// </summary>
        /// <param name="eventType">The type of narrative event</param>
        /// <returns>A fallback narrative string</returns>
        private string GetFallbackNarrative(string eventType)
        {
            return eventType switch
            {
                "firstBlood" => "The first drop of blood is drawn! The battle has truly begun.",
                "criticalHit" => "A devastating blow strikes true! The impact is felt throughout the battlefield.",
                "healthRecovery" => "{name} feels renewed strength flowing through their veins.",
                "below50Percent" => "{name} staggers under the weight of their injuries, but refuses to yield!",
                "below10Percent" => "{name} is on the brink of collapse, but their will to fight remains unbroken!",
                "playerDefeated" => "You collapse to the ground, your strength finally exhausted.",
                "enemyDefeated" => "{name} falls to the ground, defeated at last!",
                _ => "A significant event occurs in the battle."
            };
        }


        public string GenerateInformationalSummary()
        {
            var playerEvents = events.Where(e => e.Actor == playerName && e.IsSuccess).ToList();
            var enemyEvents = events.Where(e => e.Actor == enemyName && e.IsSuccess).ToList();
            var totalPlayerDamage = playerEvents.Sum(e => e.Damage);
            var totalEnemyDamage = enemyEvents.Sum(e => e.Damage);
            var playerComboCount = playerEvents.Count(e => e.IsCombo);
            var enemyComboCount = enemyEvents.Count(e => e.IsCombo);
            
            return GenerateInformationalSummary(totalPlayerDamage, totalEnemyDamage, playerComboCount, enemyComboCount);
        }

        public string GenerateInformationalSummary(int totalPlayerDamage, int totalEnemyDamage, int playerComboCount, int enemyComboCount)
        {
            bool playerWon = finalEnemyHealth <= 0;
            bool enemyWon = finalPlayerHealth <= 0;

            if (playerWon)
            {
                return $"Total damage dealt: {totalPlayerDamage} vs {totalEnemyDamage} received. " +
                       $"Combos executed: {playerComboCount} vs {enemyComboCount}.";
            }
            else if (enemyWon)
            {
                return $"{enemyName} defeats {playerName}!\nTotal damage dealt: {totalEnemyDamage} vs {totalPlayerDamage} received. " +
                       $"Combos executed: {enemyComboCount} vs {playerComboCount}.";
            }
            else
            {
                return $"Battle ends in a stalemate. {playerName} dealt {totalPlayerDamage} damage, {enemyName} dealt {totalEnemyDamage} damage. " +
                       $"Combos: {playerComboCount} vs {enemyComboCount}.";
            }
        }
    }
} 