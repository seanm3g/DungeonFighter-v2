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
            AnalyzeEventForNarratives(evt);
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
        /// </summary>
        /// <returns>List of triggered narrative messages</returns>
        public List<string> GetTriggeredNarratives()
        {
            var triggeredNarratives = new List<string>();

            if (events.Count > 0)
            {
                // Convert to list to access last element (ConcurrentBag doesn't support indexing)
                var eventsList = events.ToList();
                var lastEvent = eventsList[eventsList.Count - 1];
                triggeredNarratives = AnalyzeEventForNarratives(lastEvent);
            }

            return triggeredNarratives;
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





        public string GenerateInformationalSummary(bool showDamageWhenEnemyKilled = false)
        {
            var stats = BattleNarrativeGenerator.CalculateStatistics(events.ToList(), playerName, enemyName);
            bool playerWon = finalEnemyHealth <= 0;
            bool enemyWon = finalPlayerHealth <= 0;
            
            return BattleNarrativeGenerator.GenerateInformationalSummary(
                stats.TotalPlayerDamage, 
                stats.TotalEnemyDamage, 
                stats.PlayerComboCount, 
                stats.EnemyComboCount, 
                playerWon,
                enemyWon,
                playerName,
                enemyName,
                showDamageWhenEnemyKilled);
        }

        public string GenerateInformationalSummary(int totalPlayerDamage, int totalEnemyDamage, int playerComboCount, int enemyComboCount, bool showDamageWhenEnemyKilled = false)
        {
            bool playerWon = finalEnemyHealth <= 0;
            bool enemyWon = finalPlayerHealth <= 0;
            
            return BattleNarrativeGenerator.GenerateInformationalSummary(
                totalPlayerDamage, 
                totalEnemyDamage, 
                playerComboCount, 
                enemyComboCount, 
                playerWon,
                enemyWon,
                playerName,
                enemyName,
                showDamageWhenEnemyKilled);
        }
        
        // ===== NEW COLORED TEXT SYSTEM WRAPPERS =====
        
        // NOTE: Formatting methods have been moved to BattleNarrativeFormatter
        // Keeping these methods for backwards compatibility - they delegate to the formatter
        
        /// <summary>
        /// Formats first blood narrative using the new ColoredText system
        /// </summary>
        public List<UI.ColorSystem.ColoredText> FormatFirstBloodColored(string narrativeText)
        {
            return BattleNarrativeFormatter.FormatFirstBlood(narrativeText);
        }
        
        /// <summary>
        /// Formats critical hit narrative using the new ColoredText system
        /// </summary>
        public List<UI.ColorSystem.ColoredText> FormatCriticalHitColored(string actorName, string narrativeText)
        {
            return BattleNarrativeFormatter.FormatCriticalHit(actorName, narrativeText);
        }
        
        /// <summary>
        /// Formats critical miss narrative using the new ColoredText system
        /// </summary>
        public List<UI.ColorSystem.ColoredText> FormatCriticalMissColored(string actorName, string narrativeText)
        {
            return BattleNarrativeFormatter.FormatCriticalMiss(actorName, narrativeText);
        }
        
        /// <summary>
        /// Formats environmental action narrative using the new ColoredText system
        /// </summary>
        public List<UI.ColorSystem.ColoredText> FormatEnvironmentalActionColored(string effectDescription, string narrativeText)
        {
            return BattleNarrativeFormatter.FormatEnvironmentalAction(effectDescription, narrativeText);
        }
        
        /// <summary>
        /// Formats health recovery narrative using the new ColoredText system
        /// </summary>
        public List<UI.ColorSystem.ColoredText> FormatHealthRecoveryColored(string targetName, string narrativeText)
        {
            return BattleNarrativeFormatter.FormatHealthRecovery(targetName, narrativeText);
        }
        
        /// <summary>
        /// Formats health lead change narrative using the new ColoredText system
        /// </summary>
        public List<UI.ColorSystem.ColoredText> FormatHealthLeadChangeColored(string leaderName, string narrativeText, bool isPlayer)
        {
            return BattleNarrativeFormatter.FormatHealthLeadChange(leaderName, narrativeText, isPlayer);
        }
        
        /// <summary>
        /// Formats below 50% health narrative using the new ColoredText system
        /// </summary>
        public List<UI.ColorSystem.ColoredText> FormatBelow50PercentColored(string entityName, string narrativeText, bool isPlayer)
        {
            return BattleNarrativeFormatter.FormatBelow50Percent(entityName, narrativeText, isPlayer);
        }
        
        /// <summary>
        /// Formats below 10% health narrative using the new ColoredText system
        /// </summary>
        public List<UI.ColorSystem.ColoredText> FormatBelow10PercentColored(string entityName, string narrativeText, bool isPlayer)
        {
            return BattleNarrativeFormatter.FormatBelow10Percent(entityName, narrativeText, isPlayer);
        }
        
        /// <summary>
        /// Formats intense battle narrative using the new ColoredText system
        /// </summary>
        public List<UI.ColorSystem.ColoredText> FormatIntenseBattleColored(string narrativeText)
        {
            return BattleNarrativeFormatter.FormatIntenseBattle(playerName, enemyName, narrativeText);
        }
        
        /// <summary>
        /// Formats good combo narrative using the new ColoredText system
        /// </summary>
        public List<UI.ColorSystem.ColoredText> FormatGoodComboColored(string actorName, string targetName, bool isPlayerCombo)
        {
            return BattleNarrativeFormatter.FormatGoodCombo(actorName, targetName, isPlayerCombo);
        }
        
        /// <summary>
        /// Formats player defeated narrative using the new ColoredText system
        /// </summary>
        public List<UI.ColorSystem.ColoredText> FormatPlayerDefeatedColored(string narrativeText)
        {
            return BattleNarrativeFormatter.FormatPlayerDefeated(enemyName, narrativeText);
        }
        
        /// <summary>
        /// Formats enemy defeated narrative using the new ColoredText system
        /// </summary>
        public List<UI.ColorSystem.ColoredText> FormatEnemyDefeatedColored(string narrativeText)
        {
            return BattleNarrativeFormatter.FormatEnemyDefeated(enemyName, playerName, narrativeText);
        }
        
        /// <summary>
        /// Formats player taunt narrative using the new ColoredText system
        /// </summary>
        public List<UI.ColorSystem.ColoredText> FormatPlayerTauntColored(string tauntText)
        {
            return BattleNarrativeFormatter.FormatPlayerTaunt(playerName, enemyName, tauntText);
        }
        
        /// <summary>
        /// Formats enemy taunt narrative using the new ColoredText system
        /// </summary>
        public List<UI.ColorSystem.ColoredText> FormatEnemyTauntColored(string tauntText)
        {
            return BattleNarrativeFormatter.FormatEnemyTaunt(enemyName, playerName, tauntText);
        }
        
        /// <summary>
        /// Formats a generic narrative message using the new ColoredText system
        /// </summary>
        public List<UI.ColorSystem.ColoredText> FormatGenericNarrativeColored(string narrativeText, UI.ColorSystem.ColorPalette primaryColor = UI.ColorSystem.ColorPalette.Info)
        {
            return BattleNarrativeFormatter.FormatGenericNarrative(narrativeText, primaryColor);
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