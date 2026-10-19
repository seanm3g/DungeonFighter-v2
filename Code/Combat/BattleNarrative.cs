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
        // FIFO so "last event" is the swing just recorded (ConcurrentBag is unordered / LIFO).
        private readonly ConcurrentQueue<BattleEvent> events;
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
        private readonly object lastEventLock = new object();
        private BattleEvent? lastAddedEvent;
        private List<string> lastAddedNarratives = new List<string>();
        private List<TriggeredNarrative> lastAddedTagged = new List<TriggeredNarrative>();
        private bool lastAddedNarrativesConsumed;
        private int eventCount;

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
            this.events = new ConcurrentQueue<BattleEvent>();
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
            events.Enqueue(evt);
            int addedIndex = System.Threading.Interlocked.Increment(ref eventCount) - 1;

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

            // Analyze once per event. Re-analyzing a crit miss later would mint a new random
            // miss line and attach it to whatever swing is currently on screen.
            var tagged = AnalyzeEventForNarrativesTagged(evt);
            var triggeredNarratives = new List<string>(tagged.Count);
            foreach (var entry in tagged)
                triggeredNarratives.Add(entry.Text);

            narrativeCache.CacheNarratives(evt, triggeredNarratives, addedIndex);
            lock (lastEventLock)
            {
                lastAddedEvent = evt;
                lastAddedNarratives = triggeredNarratives;
                lastAddedTagged = tagged;
                lastAddedNarrativesConsumed = false;
            }
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
        /// Gets the narratives that were triggered by the last recorded event (no re-analysis).
        /// </summary>
        /// <returns>List of triggered narrative messages</returns>
        public List<string> GetTriggeredNarratives()
        {
            lock (lastEventLock)
            {
                return lastAddedEvent == null
                    ? new List<string>()
                    : new List<string>(lastAddedNarratives);
            }
        }

        /// <summary>
        /// Gets only the significant narratives that should be displayed for the last event
        /// Filters out narratives that shouldn't be shown (like every critical hit)
        /// Only returns narratives for events that haven't been displayed yet
        /// </summary>
        /// <returns>List of significant narrative messages that should be displayed</returns>
        public List<string> GetTriggeredNarrativesIfSignificant()
        {
            BattleEvent? evt;
            List<string> narratives;
            int displayedIndex;
            lock (lastEventLock)
            {
                if (lastAddedEvent == null || lastAddedNarrativesConsumed)
                {
                    return new List<string>();
                }

                evt = lastAddedEvent;
                narratives = lastAddedNarratives;
                displayedIndex = Math.Max(0, eventCount - 1);
                lastAddedNarrativesConsumed = true;
            }

            narrativeCache.MarkAsDisplayed(displayedIndex);

            // Significance is decided once, at generation time, inside
            // BattleEventAnalyzer.AnalyzeEvent: each one-shot bank (first blood, defeats,
            // environmental actions, health thresholds) tests its state flag and then
            // immediately sets it. Re-deriving significance here asked the same question a
            // second time, after the flag had already been consumed, so it always answered
            // false and the generated line was silently discarded before reaching the screen.
            // If generation produced a line for this event, the event was significant by
            // construction - display it.
            return FilterSignificantNarratives(narratives);
        }

        /// <summary>
        /// Determines if narratives should be displayed for a given event.
        /// NOTE: no longer used by the display path. Calling this after
        /// BattleEventAnalyzer.AnalyzeEvent has run returns false for every one-shot
        /// narrative type, because AnalyzeEvent consumes the state flags this check reads.
        /// Kept for callers that want a significance query on a fresh event.
        /// </summary>
        public bool ShouldDisplayNarrativesForEvent(BattleEvent evt)
        {
            var settings = GameSettings.Instance;
            return eventAnalyzer.IsSignificantEvent(evt, settings);
        }

        /// <summary>
        /// Drops empty entries from a generated narrative list. Nothing else is filtered here.
        /// </summary>
        /// <remarks>
        /// This used to single out critical-hit lines by searching the prose for the literal
        /// strings "devastating blow" / "strikes true" and re-checking IsSignificantEvent on
        /// them. That was wrong twice over:
        ///
        /// 1. It read the content to recover the bank. Six of the eight shipped criticalHit
        ///    lines already contained neither phrase, so they skipped the check entirely,
        ///    and any rewrite of the bank silently changed display behaviour.
        /// 2. For the two lines it did catch, the check could only fail.
        ///    BattleEventAnalyzer.AnalyzeEvent calls SetRecentCriticalHitNarrative() at the
        ///    moment it generates the line, which sets the cooldown to 3. IsSignificantEvent's
        ///    critical-hit branch requires !HasRecentCriticalHitNarrative, so by the time the
        ///    display path asked, the answer was already false and the line was discarded.
        ///
        /// Removing the sniff does not make critical-hit flavor fire on every crit: the real
        /// gate - (NarrativeBalance >= 0.7 || Roll >= 18) and a 3-crit cooldown - lives in
        /// AnalyzeEvent and still runs before a line is ever produced. Significance is decided
        /// once, at generation. If a line exists, it is meant to be seen.
        /// </remarks>
        private static List<string> FilterSignificantNarratives(List<string> narratives)
        {
            if (narratives == null || narratives.Count == 0)
            {
                return new List<string>();
            }

            return narratives.Where(n => !string.IsNullOrEmpty(n)).ToList();
        }

        /// <summary>
        /// Analyzes an event for significant narrative triggers using the event analyzer
        /// </summary>
        private List<TriggeredNarrative> AnalyzeEventForNarrativesTagged(BattleEvent evt)
        {
            var settings = GameSettings.Instance;
            if (!settings.EnableNarrativeEvents)
            {
                return new List<TriggeredNarrative>();
            }

            var tagged = eventAnalyzer.AnalyzeEventTagged(evt, settings);
            foreach (var entry in tagged)
                narrativeEvents.Add(entry.Text);

            return tagged;
        }

        /// <summary>
        /// The last event's narratives with the bank each came from, for tier-aware display.
        /// Does not consume the one-shot flag - callers that display must still call
        /// <see cref="GetTriggeredNarrativesIfSignificant"/> (or consume it themselves).
        /// </summary>
        public List<TriggeredNarrative> GetTriggeredNarrativesTagged()
        {
            lock (lastEventLock)
            {
                return lastAddedEvent == null
                    ? new List<TriggeredNarrative>()
                    : new List<TriggeredNarrative>(lastAddedTagged);
            }
        }

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