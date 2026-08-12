using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using RPGGame.Combat;

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
        public int NaturalRoll { get; set; }
        public int Difficulty { get; set; }
        public bool IsCritical { get; set; }

        public BattleEvent()
        {
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Refactored BattleNarrative facade using specialized managers for better maintainability.
    /// Coordinates narrative generation across multiple specialized systems.
    /// Thread-safe for parallel battle testing.
    /// </summary>
    public class BattleNarrative
    {
        // Core data - use thread-safe collections for parallel battle testing
        private readonly ConcurrentBag<BattleEvent> events;
        private readonly string playerName;
        private readonly string enemyName;
        private readonly string currentLocation;
        private readonly int initialPlayerHealth;
        private readonly int initialEnemyHealth;
        private int finalPlayerHealth;
        private int finalEnemyHealth;

        // Specialized managers
        private readonly NarrativeStateManager stateManager;
        private readonly NarrativeTextProvider textProvider;
        private readonly TauntSystem tauntSystem;
        private readonly BattleEventAnalyzer eventAnalyzer;
        private FunMomentTracker? funMomentTracker;

        // Narrative tracking - use thread-safe collections for parallel testing
        private readonly ConcurrentBag<string> narrativeEvents;
        private readonly ConcurrentBag<string> pendingNarrativeEvents;
        
        // Cache narratives for the last event to prevent re-analysis
        private readonly BattleNarrativeCache narrativeCache;
        private BattleEvent? lastAddedEvent;
        private List<string> lastEventNarratives = new();

        public BattleNarrative(string playerName, string enemyName, string environmentName = "", int playerHealth = 0, int enemyHealth = 0)
        {
            this.playerName = playerName;
            this.enemyName = enemyName;
            this.currentLocation = environmentName;
            this.initialPlayerHealth = playerHealth;
            this.initialEnemyHealth = enemyHealth;
            this.finalPlayerHealth = playerHealth;
            this.finalEnemyHealth = enemyHealth;

            // Initialize collections - use thread-safe collections for parallel testing
            this.events = new ConcurrentBag<BattleEvent>();
            this.narrativeEvents = new ConcurrentBag<string>();
            this.pendingNarrativeEvents = new ConcurrentBag<string>();
            this.narrativeCache = new BattleNarrativeCache();

            // Initialize specialized managers
            this.stateManager = new NarrativeStateManager();
            this.textProvider = new NarrativeTextProvider();
            this.tauntSystem = new TauntSystem(textProvider);
            this.eventAnalyzer = new BattleEventAnalyzer(textProvider, stateManager, tauntSystem);

            // Initialize analyzer with context
            eventAnalyzer.Initialize(playerName, enemyName, environmentName, playerHealth, enemyHealth);
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

            // Update analyzer with latest health values
            eventAnalyzer.UpdateFinalHealth(finalPlayerHealth, finalEnemyHealth);

            // Notify fun moment tracker
            if (funMomentTracker != null)
            {
                funMomentTracker.RecordEvent(evt, finalPlayerHealth, finalEnemyHealth);
            }

            // Check for significant events that trigger narrative
            lastAddedEvent = evt;
            lastEventNarratives = AnalyzeEventForNarratives(evt);
            narrativeCache.CacheNarratives(evt, lastEventNarratives, events.Count - 1);
        }

        /// <summary>
        /// Adds an environmental action event to the narrative
        /// </summary>
        public void AddEnvironmentalAction(string effectDescription)
        {
            var envEvent = new BattleEvent
            {
                Actor = currentLocation,
                Target = "both",
                Action = "Environmental Effect",
                EnvironmentEffect = effectDescription,
                IsSuccess = true
            };
            
            AddEvent(envEvent);
        }
        
        /// <summary>
        /// Gets the narratives that were triggered by the last event
        /// Returns cached narratives to prevent duplicate analysis
        /// </summary>
        /// <returns>List of triggered narrative messages</returns>
        public List<string> GetTriggeredNarratives()
        {
            if (lastAddedEvent == null)
            {
                return new List<string>();
            }

            return new List<string>(lastEventNarratives);
        }

        /// <summary>
        /// Gets only the significant narratives that should be displayed for the last event
        /// Filters out narratives that shouldn't be shown (like every critical hit)
        /// Only returns narratives for events that haven't been displayed yet
        /// </summary>
        /// <returns>List of significant narrative messages that should be displayed</returns>
        public List<string> GetTriggeredNarrativesIfSignificant()
        {
            if (lastAddedEvent == null || lastEventNarratives.Count == 0)
            {
                return new List<string>();
            }

            // Display this instance if AnalyzeEvent produced text for it (captured before
            // one-shot Has*Occurred flags mutated). Do not re-analyze — that would see
            // the flags already set and generate nothing.
            if (!ShouldDisplayNarrativesForEvent(lastAddedEvent))
            {
                return new List<string>();
            }

            var displayed = new List<string>();
            foreach (var narrative in lastEventNarratives)
            {
                if (!string.IsNullOrEmpty(narrative))
                    displayed.Add(narrative);
            }
            return displayed;
        }

        /// <summary>
        /// Determines if narratives should be displayed for a given event
        /// </summary>
        public bool ShouldDisplayNarrativesForEvent(BattleEvent evt)
        {
            var settings = GameSettings.Instance;
            return eventAnalyzer.IsSignificantEvent(evt, settings);
        }

        /// <summary>
        /// Analyzes an event for significant narrative triggers using the event analyzer
        /// </summary>
        private List<string> AnalyzeEventForNarratives(BattleEvent evt)
        {
            var context = new BattleNarrativeContext
            {
                EventAnalyzer = eventAnalyzer,
                PlayerName = playerName,
                EnemyName = enemyName,
                CurrentLocation = currentLocation
            };
            
            var triggeredNarratives = BattleNarrativeGenerator.GenerateNarratives(evt, context);

            // Add all triggered narratives to the permanent log
            foreach (var narrative in triggeredNarratives)
            {
                narrativeEvents.Add(narrative);
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





        public string GenerateInformationalSummary()
        {
            var stats = BattleNarrativeGenerator.CalculateStatistics(events.ToList(), playerName, enemyName);
            bool playerWon = finalEnemyHealth <= 0;
            bool enemyWon = finalPlayerHealth <= 0;
            
            return BattleNarrativeGenerator.GenerateInformationalSummary(
                stats.TotalPlayerDamage, 
                stats.TotalEnemyDamage, 
                playerWon,
                enemyWon,
                playerName,
                enemyName);
        }

        /// <summary>
        /// Gets all battle events for analysis
        /// </summary>
        public List<BattleEvent> GetAllEvents()
        {
            return events.ToList();
        }

        /// <summary>
        /// Sets the fun moment tracker for this battle
        /// </summary>
        public void SetFunMomentTracker(FunMomentTracker tracker)
        {
            funMomentTracker = tracker;
        }

        /// <summary>
        /// Gets the fun moment tracker for this battle
        /// </summary>
        public FunMomentTracker? GetFunMomentTracker()
        {
            return funMomentTracker;
        }
    }
} 