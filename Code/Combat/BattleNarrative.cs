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
        private bool goodComboOccurred;
        private bool playerBelow50Percent;
        private bool enemyBelow50Percent;
        private bool playerBelow10Percent;
        private bool enemyBelow10Percent;
        private bool intenseBattleTriggered;
        private bool environmentalActionOccurred;
        private bool playerDefeated;
        private bool enemyDefeated;
        private bool playerHadHealthLead;
        private bool enemyHadHealthLead;
        private int narrativeEventCount;
        private Random narrativeRandom;
        private int playerActionCount;
        private int enemyActionCount;
        private int playerTauntCount;
        private int enemyTauntCount;
        private string currentLocation;

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
            this.goodComboOccurred = false;
            this.playerBelow50Percent = false;
            this.enemyBelow50Percent = false;
            this.playerBelow10Percent = false;
            this.enemyBelow10Percent = false;
            this.intenseBattleTriggered = false;
            this.environmentalActionOccurred = false;
            this.playerDefeated = false;
            this.enemyDefeated = false;
            this.playerHadHealthLead = false;
            this.enemyHadHealthLead = false;
            this.narrativeEventCount = 0;
            this.narrativeRandom = new Random();
            this.playerActionCount = 0;
            this.enemyActionCount = 0;
            this.playerTauntCount = 0;
            this.enemyTauntCount = 0;
            this.currentLocation = environmentName;
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
        /// Adds an environmental action event to the narrative
        /// </summary>
        public void AddEnvironmentalAction(string effectDescription)
        {
            var envEvent = new BattleEvent
            {
                Actor = environmentName,
                Target = "both",
                Action = "Environmental Effect",
                EnvironmentEffect = effectDescription,
                IsSuccess = true
            };
            
            AddEvent(envEvent);
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
                narrativeEventCount++;
                
                // Add to triggered narratives for immediate display
                if (settings.EnableNarrativeEvents)
                {
                    triggeredNarratives.Add(narrative);
                }
            }

            // Critical Hit - when a critical hit occurs
            if (evt.IsCritical && evt.IsSuccess)
            {
                string narrative = GetRandomNarrative("criticalHit").Replace("{name}", evt.Actor);
                narrativeEvents.Add(narrative);
                narrativeEventCount++;
                
                // Add to triggered narratives for immediate display
                if (settings.EnableNarrativeEvents)
                {
                    triggeredNarratives.Add(narrative);
                }
            }

            // Critical Miss - when a critical miss occurs (roll <= 1)
            if (!evt.IsSuccess && evt.Roll <= 1)
            {
                string narrative = GetRandomNarrative("criticalMiss").Replace("{name}", evt.Actor);
                narrativeEvents.Add(narrative);
                narrativeEventCount++;
                
                // Add to triggered narratives for immediate display
                if (settings.EnableNarrativeEvents)
                {
                    triggeredNarratives.Add(narrative);
                }
            }

            // Environmental Action - when environment takes action
            if (!string.IsNullOrEmpty(evt.EnvironmentEffect) && !environmentalActionOccurred)
            {
                environmentalActionOccurred = true;
                string narrative = GetRandomNarrative("environmentalAction").Replace("{effect}", evt.EnvironmentEffect);
                narrativeEvents.Add(narrative);
                narrativeEventCount++;
                
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

            // Health Lead Change - when someone gains or loses health advantage
            CheckHealthLeadChange(evt, triggeredNarratives, settings);

            // Taunt System - characters and enemies taunt periodically
            CheckTauntSystem(evt, triggeredNarratives, settings);


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

            // Intense Battle - when both combatants are below 50% health
            if (!intenseBattleTriggered && playerHealthPercentage50 < 0.5 && enemyHealthPercentage50 < 0.5 && finalPlayerHealth > 0 && finalEnemyHealth > 0)
            {
                intenseBattleTriggered = true;
                string narrative = GetRandomNarrative("intenseBattle").Replace("{player}", playerName).Replace("{enemy}", enemyName);
                narrativeEvents.Add(narrative);
                narrativeEventCount++;
                
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
                string narrative = GetRandomNarrative("playerDefeated").Replace("{enemy}", enemyName);
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
                string narrative = GetRandomNarrative("enemyDefeated").Replace("{name}", enemyName).Replace("{player}", playerName);
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
                "criticalMiss" => "A wild swing misses completely! The attack goes wide of its target.",
                "environmentalAction" => "The environment itself joins the fray! {effect}",
                "healthLeadChange" => "The tide of battle shifts! {name} now holds the advantage!",
                "escalatingTension" => "The battle grows more desperate with each passing moment!",
                "healthRecovery" => "{name} feels renewed strength flowing through their veins.",
                "below50Percent" => "{name} staggers under the weight of their injuries, but refuses to yield!",
                "below10Percent" => "{name} is on the brink of collapse, but their will to fight remains unbroken!",
                "playerDefeated" => "You collapse to the ground, your strength finally exhausted.",
                "enemyDefeated" => "{name} falls to the ground, defeated at last!",
                "playerTaunt" => "\"{enemy}, you're no match for me!\" {name} declares confidently.",
                "enemyTaunt" => "\"You cannot defeat me, {player}!\" {name} growls menacingly.",
                "intenseBattle" => "The battle reaches a fever pitch as both {player} and {enemy} stand bloodied but unbroken!",
                // Library taunts
                "playerTaunt_library" => "\"Shh! We're in a library!\" {name} whispers fiercely to {enemy}.",
                "enemyTaunt_library" => "\"Silence! This sacred place demands respect!\" {name} hisses at {player}.",
                // Underwater taunts
                "playerTaunt_underwater" => "*Bubbles escape {name}'s mouth as they gesture threateningly at {enemy}.*",
                "enemyTaunt_underwater" => "*{name} makes aggressive gestures, bubbles streaming from their mouth.*",
                // Lava taunts
                "playerTaunt_lava" => "\"The heat won't save you, {enemy}!\" {name} shouts over the roaring flames.",
                "enemyTaunt_lava" => "\"You'll burn before you defeat me, {player}!\" {name} roars through the inferno.",
                // Crypt taunts
                "playerTaunt_crypt" => "\"You belong here with the dead, {enemy}!\" {name} declares in the echoing tomb.",
                "enemyTaunt_crypt" => "\"Join the others in eternal rest, {player}!\" {name} intones ominously.",
                // Crystal taunts
                "playerTaunt_crystal" => "\"Your fate is crystal clear, {enemy}!\" {name} shouts, voice echoing off the gems.",
                "enemyTaunt_crystal" => "\"You'll shatter like glass, {player}!\" {name} bellows in the crystalline chamber.",
                // Temple taunts
                "playerTaunt_temple" => "\"The gods favor me, not you, {enemy}!\" {name} proclaims in the sacred hall.",
                "enemyTaunt_temple" => "\"Your blasphemy ends here, {player}!\" {name} thunders in the holy sanctuary.",
                // Forest taunts
                "playerTaunt_forest" => "\"The forest itself will aid me against you, {enemy}!\" {name} calls to the trees.",
                "enemyTaunt_forest" => "\"Nature's wrath will consume you, {player}!\" {name} growls among the ancient oaks.",
                _ => "A significant event occurs in the battle."
            };
        }

        /// <summary>
        /// Checks for health lead changes and triggers appropriate narratives
        /// </summary>
        private void CheckHealthLeadChange(BattleEvent evt, List<string> triggeredNarratives, GameSettings settings)
        {
            // Determine current health lead
            bool playerCurrentlyLeads = finalPlayerHealth > finalEnemyHealth;
            bool enemyCurrentlyLeads = finalEnemyHealth > finalPlayerHealth;
            
            // Only trigger health lead change narratives for significant damage (3+ damage)
            // This reduces frequency of these narratives
            bool significantDamage = evt.Damage >= 3;
            
            // Check if lead has changed
            if (playerCurrentlyLeads && !playerHadHealthLead && significantDamage)
            {
                playerHadHealthLead = true;
                enemyHadHealthLead = false;
                string narrative = GetRandomNarrative("healthLeadChange").Replace("{name}", playerName);
                narrativeEvents.Add(narrative);
                narrativeEventCount++;
                
                if (settings.EnableNarrativeEvents)
                {
                    triggeredNarratives.Add(narrative);
                }
            }
            else if (enemyCurrentlyLeads && !enemyHadHealthLead && significantDamage)
            {
                enemyHadHealthLead = true;
                playerHadHealthLead = false;
                string narrative = GetRandomNarrative("healthLeadChange").Replace("{name}", enemyName);
                narrativeEvents.Add(narrative);
                narrativeEventCount++;
                
                if (settings.EnableNarrativeEvents)
                {
                    triggeredNarratives.Add(narrative);
                }
            }
        }



        /// <summary>
        /// Checks for taunt opportunities and triggers appropriate location-aware taunts
        /// </summary>
        private void CheckTauntSystem(BattleEvent evt, List<string> triggeredNarratives, GameSettings settings)
        {
            // Track action counts
            if (evt.Actor == playerName)
            {
                playerActionCount++;
            }
            else if (evt.Actor == enemyName)
            {
                enemyActionCount++;
            }

            // Player taunts (only 1-2 times per battle, after significant action counts)
            if (evt.Actor == playerName && playerTauntCount < 2)
            {
                // First taunt after 8-12 actions, second taunt after 15-20 actions
                int[] tauntThresholds = { 8 + (int)(settings.NarrativeBalance * 4), 15 + (int)(settings.NarrativeBalance * 5) };
                
                if (playerActionCount >= tauntThresholds[playerTauntCount])
                {
                    string taunt = GetLocationSpecificTaunt("player", playerName, enemyName);
                    narrativeEvents.Add(taunt);
                    narrativeEventCount++;
                    playerTauntCount++;
                    
                    if (settings.EnableNarrativeEvents)
                    {
                        triggeredNarratives.Add(taunt);
                    }
                }
            }
            // Enemy taunts (only 1-2 times per battle, after significant action counts)
            else if (evt.Actor == enemyName && enemyTauntCount < 2)
            {
                // First taunt after 6-10 actions, second taunt after 12-18 actions
                int[] tauntThresholds = { 6 + (int)(settings.NarrativeBalance * 4), 12 + (int)(settings.NarrativeBalance * 6) };
                
                if (enemyActionCount >= tauntThresholds[enemyTauntCount])
                {
                    string taunt = GetLocationSpecificTaunt("enemy", enemyName, playerName);
                    narrativeEvents.Add(taunt);
                    narrativeEventCount++;
                    enemyTauntCount++;
                    
                    if (settings.EnableNarrativeEvents)
                    {
                        triggeredNarratives.Add(taunt);
                    }
                }
            }
        }

        /// <summary>
        /// Gets location-specific taunt text based on the current environment
        /// </summary>
        private string GetLocationSpecificTaunt(string taunterType, string taunterName, string targetName)
        {
            // Determine location type from current location
            string locationType = GetLocationType(currentLocation);
            
            // Get location-specific taunt
            string tauntKey = $"{taunterType}Taunt_{locationType}";
            string taunt = GetRandomNarrative(tauntKey);
            
            // If no location-specific taunt exists, fall back to generic
            if (taunt == GetFallbackNarrative(tauntKey))
            {
                tauntKey = $"{taunterType}Taunt";
                taunt = GetRandomNarrative(tauntKey);
            }
            
            // Replace placeholders
            taunt = taunt.Replace("{name}", taunterName);
            if (taunterType == "player")
            {
                taunt = taunt.Replace("{enemy}", targetName);
            }
            else
            {
                taunt = taunt.Replace("{player}", targetName);
            }
            
            return taunt;
        }

        /// <summary>
        /// Determines the location type from the environment name
        /// </summary>
        private string GetLocationType(string environmentName)
        {
            if (string.IsNullOrEmpty(environmentName))
                return "generic";
                
            string lowerEnv = environmentName.ToLower();
            
            if (lowerEnv.Contains("library") || lowerEnv.Contains("study") || lowerEnv.Contains("archive"))
                return "library";
            else if (lowerEnv.Contains("water") || lowerEnv.Contains("ocean") || lowerEnv.Contains("sea") || lowerEnv.Contains("underwater"))
                return "underwater";
            else if (lowerEnv.Contains("lava") || lowerEnv.Contains("volcano") || lowerEnv.Contains("fire"))
                return "lava";
            else if (lowerEnv.Contains("crypt") || lowerEnv.Contains("tomb") || lowerEnv.Contains("grave"))
                return "crypt";
            else if (lowerEnv.Contains("crystal") || lowerEnv.Contains("cave"))
                return "crystal";
            else if (lowerEnv.Contains("temple") || lowerEnv.Contains("sanctuary"))
                return "temple";
            else if (lowerEnv.Contains("forest") || lowerEnv.Contains("grove"))
                return "forest";
            else
                return "generic";
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
                return $"Total damage dealt: {totalPlayerDamage} vs {totalEnemyDamage} received.\n" +
                       $"Combos executed: {playerComboCount} vs {enemyComboCount}.";
            }
            else if (enemyWon)
            {
                return $"{enemyName} defeats {playerName}!\nTotal damage dealt: {totalEnemyDamage} vs {totalPlayerDamage} received.\n" +
                       $"Combos executed: {enemyComboCount} vs {playerComboCount}.";
            }
            else
            {
                return $"Battle ends in a stalemate. {playerName} dealt {totalPlayerDamage} damage, {enemyName} dealt {totalEnemyDamage} damage.\n" +
                       $"Combos: {playerComboCount} vs {enemyComboCount}.";
            }
        }
        
        // ===== NEW COLORED TEXT SYSTEM WRAPPERS =====
        
        /// <summary>
        /// Formats first blood narrative using the new ColoredText system
        /// </summary>
        public List<UI.ColorSystem.ColoredText> FormatFirstBloodColored(string narrativeText)
        {
            return BattleNarrativeColoredText.FormatFirstBloodColored(narrativeText);
        }
        
        /// <summary>
        /// Formats critical hit narrative using the new ColoredText system
        /// </summary>
        public List<UI.ColorSystem.ColoredText> FormatCriticalHitColored(string actorName, string narrativeText)
        {
            return BattleNarrativeColoredText.FormatCriticalHitColored(actorName, narrativeText);
        }
        
        /// <summary>
        /// Formats critical miss narrative using the new ColoredText system
        /// </summary>
        public List<UI.ColorSystem.ColoredText> FormatCriticalMissColored(string actorName, string narrativeText)
        {
            return BattleNarrativeColoredText.FormatCriticalMissColored(actorName, narrativeText);
        }
        
        /// <summary>
        /// Formats environmental action narrative using the new ColoredText system
        /// </summary>
        public List<UI.ColorSystem.ColoredText> FormatEnvironmentalActionColored(string effectDescription, string narrativeText)
        {
            return BattleNarrativeColoredText.FormatEnvironmentalActionColored(effectDescription, narrativeText);
        }
        
        /// <summary>
        /// Formats health recovery narrative using the new ColoredText system
        /// </summary>
        public List<UI.ColorSystem.ColoredText> FormatHealthRecoveryColored(string targetName, string narrativeText)
        {
            return BattleNarrativeColoredText.FormatHealthRecoveryColored(targetName, narrativeText);
        }
        
        /// <summary>
        /// Formats health lead change narrative using the new ColoredText system
        /// </summary>
        public List<UI.ColorSystem.ColoredText> FormatHealthLeadChangeColored(string leaderName, string narrativeText, bool isPlayer)
        {
            return BattleNarrativeColoredText.FormatHealthLeadChangeColored(leaderName, narrativeText, isPlayer);
        }
        
        /// <summary>
        /// Formats below 50% health narrative using the new ColoredText system
        /// </summary>
        public List<UI.ColorSystem.ColoredText> FormatBelow50PercentColored(string entityName, string narrativeText, bool isPlayer)
        {
            return BattleNarrativeColoredText.FormatBelow50PercentColored(entityName, narrativeText, isPlayer);
        }
        
        /// <summary>
        /// Formats below 10% health narrative using the new ColoredText system
        /// </summary>
        public List<UI.ColorSystem.ColoredText> FormatBelow10PercentColored(string entityName, string narrativeText, bool isPlayer)
        {
            return BattleNarrativeColoredText.FormatBelow10PercentColored(entityName, narrativeText, isPlayer);
        }
        
        /// <summary>
        /// Formats intense battle narrative using the new ColoredText system
        /// </summary>
        public List<UI.ColorSystem.ColoredText> FormatIntenseBattleColored(string narrativeText)
        {
            return BattleNarrativeColoredText.FormatIntenseBattleColored(playerName, enemyName, narrativeText);
        }
        
        /// <summary>
        /// Formats good combo narrative using the new ColoredText system
        /// </summary>
        public List<UI.ColorSystem.ColoredText> FormatGoodComboColored(string actorName, string targetName, bool isPlayerCombo)
        {
            return BattleNarrativeColoredText.FormatGoodComboColored(actorName, targetName, isPlayerCombo);
        }
        
        /// <summary>
        /// Formats player defeated narrative using the new ColoredText system
        /// </summary>
        public List<UI.ColorSystem.ColoredText> FormatPlayerDefeatedColored(string narrativeText)
        {
            return BattleNarrativeColoredText.FormatPlayerDefeatedColored(enemyName, narrativeText);
        }
        
        /// <summary>
        /// Formats enemy defeated narrative using the new ColoredText system
        /// </summary>
        public List<UI.ColorSystem.ColoredText> FormatEnemyDefeatedColored(string narrativeText)
        {
            return BattleNarrativeColoredText.FormatEnemyDefeatedColored(enemyName, playerName, narrativeText);
        }
        
        /// <summary>
        /// Formats player taunt narrative using the new ColoredText system
        /// </summary>
        public List<UI.ColorSystem.ColoredText> FormatPlayerTauntColored(string tauntText)
        {
            return BattleNarrativeColoredText.FormatPlayerTauntColored(playerName, enemyName, tauntText);
        }
        
        /// <summary>
        /// Formats enemy taunt narrative using the new ColoredText system
        /// </summary>
        public List<UI.ColorSystem.ColoredText> FormatEnemyTauntColored(string tauntText)
        {
            return BattleNarrativeColoredText.FormatEnemyTauntColored(enemyName, playerName, tauntText);
        }
        
        /// <summary>
        /// Formats a generic narrative message using the new ColoredText system
        /// </summary>
        public List<UI.ColorSystem.ColoredText> FormatGenericNarrativeColored(string narrativeText, UI.ColorSystem.ColorPalette primaryColor = UI.ColorSystem.ColorPalette.Info)
        {
            return BattleNarrativeColoredText.FormatGenericNarrativeColored(narrativeText, primaryColor);
        }
    }
} 