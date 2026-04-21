using System;
using System.Collections.Concurrent;
using RPGGame;

namespace RPGGame.Combat
{
    /// <summary>
    /// Manages dynamic threshold adjustment for combat rolls
    /// Allows actions to modify critical hit, combo, and hit thresholds.
    /// Hierarchy (highest to lowest): CRIT &gt; COMBO &gt; HIT &gt; MISS &gt; CRIT MISS.
    /// When a higher threshold is modified to go lower, the next lower threshold cascades with it.
    /// Example: CRIT 19→12 cascades COMBO to 12; COMBO 14→4 cascades HIT to 3.
    /// When two thresholds share the same value, the higher category is triggered.
    /// </summary>
    public class ThresholdManager
    {
        private readonly ConcurrentDictionary<Actor, ThresholdModifiers> _actorThresholds = new ConcurrentDictionary<Actor, ThresholdModifiers>();

        /// <summary>
        /// Threshold modifiers for an actor
        /// </summary>
        public class ThresholdModifiers
        {
            public int? CriticalMissThreshold { get; set; } // Default: 1 (natural 1)
            public int? CriticalHitThreshold { get; set; } // Default: 20
            public int? ComboThreshold { get; set; } // Default: 14
            public int? HitThreshold { get; set; } // Default: 5

            public ThresholdModifiers()
            {
                // Initialize with defaults from GameConfiguration
                var config = GameConfiguration.Instance;
                CriticalMissThreshold = 1; // Natural 1 is always critical miss
                
                // Use default 20 if config value is 0 or invalid
                int criticalHitThreshold = config.Combat.CriticalHitThreshold;
                CriticalHitThreshold = criticalHitThreshold > 0 ? criticalHitThreshold : 20;
                
                // Use default 14 if config value is 0 or invalid
                int comboThreshold = config.RollSystem.ComboThreshold.Min;
                ComboThreshold = comboThreshold > 0 ? comboThreshold : 14;
                
                // Hit threshold should be MissThreshold.Max (so that MissThreshold.Max + 1 is the minimum roll to hit)
                // Use default 5 if config value is 0 or invalid
                int missThresholdMax = config.RollSystem.MissThreshold.Max;
                HitThreshold = missThresholdMax > 0 ? missThresholdMax : 5;
            }
        }

        private ThresholdModifiers GetOrCreateModifiers(Actor actor) =>
            _actorThresholds.GetOrAdd(actor, _ => new ThresholdModifiers());

        /// <summary>
        /// Gets the critical miss threshold for an actor
        /// </summary>
        public int GetCriticalMissThreshold(Actor actor)
        {
            if (_actorThresholds.TryGetValue(actor, out var modifiers) && modifiers.CriticalMissThreshold.HasValue)
            {
                return modifiers.CriticalMissThreshold.Value;
            }
            
            return 1; // Default: natural 1 is critical miss
        }

        /// <summary>
        /// Gets the critical hit threshold for an actor
        /// </summary>
        public int GetCriticalHitThreshold(Actor actor)
        {
            if (_actorThresholds.TryGetValue(actor, out var modifiers) && modifiers.CriticalHitThreshold.HasValue)
            {
                return modifiers.CriticalHitThreshold.Value;
            }
            
            // Use default 20 if config value is 0 or invalid
            int threshold = GameConfiguration.Instance.Combat.CriticalHitThreshold;
            return threshold > 0 ? threshold : 20;
        }

        /// <summary>
        /// Gets the combo threshold for an actor
        /// </summary>
        public int GetComboThreshold(Actor actor)
        {
            if (_actorThresholds.TryGetValue(actor, out var modifiers) && modifiers.ComboThreshold.HasValue)
            {
                return modifiers.ComboThreshold.Value;
            }
            
            // Use default 14 if config value is 0 or invalid
            int threshold = GameConfiguration.Instance.RollSystem.ComboThreshold.Min;
            return threshold > 0 ? threshold : 14;
        }

        /// <summary>
        /// Gets the hit threshold for an actor
        /// Hit threshold is the maximum miss value (so 6+ hits when threshold is 5)
        /// </summary>
        public int GetHitThreshold(Actor actor)
        {
            if (_actorThresholds.TryGetValue(actor, out var modifiers) && modifiers.HitThreshold.HasValue)
            {
                return modifiers.HitThreshold.Value;
            }
            
            // Hit threshold should be MissThreshold.Max (5), meaning 6+ hits
            // Normal attack threshold (6) is the minimum roll for normal attacks, not the hit threshold
            int missThresholdMax = GameConfiguration.Instance.RollSystem.MissThreshold.Max;
            return missThresholdMax > 0 ? missThresholdMax : 5;
        }

        /// <summary>
        /// Sets the critical miss threshold for an actor.
        /// Cascades: CRIT_MISS is capped by HIT (cannot exceed max miss value).
        /// </summary>
        public void SetCriticalMissThreshold(Actor actor, int threshold)
        {
            var mod = GetOrCreateModifiers(actor);
            int hit = GetHitThreshold(actor);
            mod.CriticalMissThreshold = Math.Min(threshold, hit);
        }

        /// <summary>
        /// Adjusts the critical miss threshold for an actor.
        /// Positive adjustment RAISES the threshold (more crit misses, e.g. +2 means 1-3 are crit misses).
        /// Negative adjustment LOWERS the threshold (fewer crit misses).
        /// </summary>
        public void AdjustCriticalMissThreshold(Actor actor, int adjustment)
        {
            int current = GetCriticalMissThreshold(actor);
            SetCriticalMissThreshold(actor, current + adjustment);
        }

        /// <summary>
        /// Sets the critical hit threshold for an actor.
        /// Cascades: if new CRIT is lower than COMBO, COMBO moves down to match; then HIT cascades if needed.
        /// </summary>
        public void SetCriticalHitThreshold(Actor actor, int threshold)
        {
            var mod = GetOrCreateModifiers(actor);
            mod.CriticalHitThreshold = threshold;
            // Cascade: COMBO cannot exceed CRIT; when CRIT goes lower, COMBO moves with it
            int combo = GetComboThreshold(actor);
            if (combo > threshold)
            {
                mod.ComboThreshold = threshold;
                // Cascade HIT: must be &lt; COMBO
                int hit = GetHitThreshold(actor);
                int newHit = Math.Max(0, threshold - 1);
                if (hit > newHit)
                {
                    mod.HitThreshold = newHit;
                    // Cascade CRIT_MISS: cannot exceed HIT
                    int critMiss = GetCriticalMissThreshold(actor);
                    if (critMiss > newHit)
                        mod.CriticalMissThreshold = newHit;
                }
            }
        }

        /// <summary>
        /// Adjusts the critical hit threshold for an actor.
        /// Positive adjustment REDUCES the threshold (easier to crit, e.g. +5 means need 14 instead of 19).
        /// Negative adjustment RAISES the threshold (harder to crit).
        /// </summary>
        public void AdjustCriticalHitThreshold(Actor actor, int adjustment)
        {
            int current = GetCriticalHitThreshold(actor);
            SetCriticalHitThreshold(actor, current - adjustment);
        }

        /// <summary>
        /// Sets the combo threshold for an actor.
        /// Cascades: COMBO is capped by CRIT; when COMBO goes lower, HIT moves to COMBO - 1.
        /// </summary>
        public void SetComboThreshold(Actor actor, int threshold)
        {
            var mod = GetOrCreateModifiers(actor);
            int crit = GetCriticalHitThreshold(actor);
            mod.ComboThreshold = Math.Min(threshold, crit);
            // Cascade: when COMBO goes lower, HIT moves with it (HIT = COMBO - 1)
            int newHit = Math.Max(0, mod.ComboThreshold.Value - 1);
            int hit = GetHitThreshold(actor);
            if (hit > newHit)
            {
                mod.HitThreshold = newHit;
                // Cascade CRIT_MISS: cannot exceed HIT
                int critMiss = GetCriticalMissThreshold(actor);
                if (critMiss > newHit)
                    mod.CriticalMissThreshold = newHit;
            }
        }

        /// <summary>
        /// Adjusts the combo threshold for an actor.
        /// Positive adjustment REDUCES the threshold (easier combo, e.g. +3 means need 11 instead of 14).
        /// Negative adjustment RAISES the threshold (harder combo).
        /// </summary>
        public void AdjustComboThreshold(Actor actor, int adjustment)
        {
            int current = GetComboThreshold(actor);
            SetComboThreshold(actor, current - adjustment);
        }

        /// <summary>
        /// Sets the hit threshold for an actor.
        /// Cascades: HIT is capped by COMBO - 1; when HIT goes lower, CRIT_MISS moves with it.
        /// </summary>
        public void SetHitThreshold(Actor actor, int threshold)
        {
            var mod = GetOrCreateModifiers(actor);
            int combo = GetComboThreshold(actor);
            int maxHit = Math.Max(0, combo - 1);
            mod.HitThreshold = Math.Min(threshold, maxHit);
            // Cascade: CRIT_MISS cannot exceed HIT; when HIT goes lower, CRIT_MISS moves with it
            int critMiss = GetCriticalMissThreshold(actor);
            if (critMiss > mod.HitThreshold)
                mod.CriticalMissThreshold = mod.HitThreshold.Value;
        }

        /// <summary>
        /// Adjusts the hit threshold for an actor.
        /// Positive adjustment REDUCES the threshold (easier to hit, e.g. +3 means need 4+ instead of 6+).
        /// Negative adjustment RAISES the threshold (harder to hit).
        /// </summary>
        public void AdjustHitThreshold(Actor actor, int adjustment)
        {
            int current = GetHitThreshold(actor);
            SetHitThreshold(actor, current - adjustment);
        }

        /// <summary>
        /// Resets all thresholds for an actor to defaults
        /// </summary>
        public void ResetThresholds(Actor actor) =>
            _actorThresholds.TryRemove(actor, out _);

        /// <summary>
        /// Clears all threshold modifications (useful for testing)
        /// </summary>
        public void Clear()
        {
            _actorThresholds.Clear();
        }
    }
}

