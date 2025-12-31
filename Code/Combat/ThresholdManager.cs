using System;
using System.Collections.Generic;
using RPGGame;

namespace RPGGame.Combat
{
    /// <summary>
    /// Manages dynamic threshold adjustment for combat rolls
    /// Allows actions to modify critical hit, combo, and hit thresholds
    /// </summary>
    public class ThresholdManager
    {
        private readonly Dictionary<Actor, ThresholdModifiers> _actorThresholds = new Dictionary<Actor, ThresholdModifiers>();

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
                CriticalHitThreshold = config.Combat.CriticalHitThreshold;
                ComboThreshold = config.RollSystem.ComboThreshold.Min;
                // Hit threshold should be MissThreshold.Max (so that MissThreshold.Max + 1 is the minimum roll to hit)
                int missThresholdMax = config.RollSystem.MissThreshold.Max;
                HitThreshold = missThresholdMax > 0 ? missThresholdMax : 5;
            }
        }

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
            
            return GameConfiguration.Instance.Combat.CriticalHitThreshold;
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
            
            return GameConfiguration.Instance.RollSystem.ComboThreshold.Min;
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
            // BasicAttackThreshold.Min (6) is the minimum roll for a basic attack, not the hit threshold
            int missThresholdMax = GameConfiguration.Instance.RollSystem.MissThreshold.Max;
            return missThresholdMax > 0 ? missThresholdMax : 5;
        }

        /// <summary>
        /// Sets the critical miss threshold for an actor
        /// </summary>
        public void SetCriticalMissThreshold(Actor actor, int threshold)
        {
            if (!_actorThresholds.ContainsKey(actor))
            {
                _actorThresholds[actor] = new ThresholdModifiers();
            }
            _actorThresholds[actor].CriticalMissThreshold = threshold;
        }

        /// <summary>
        /// Adjusts the critical miss threshold for an actor (adds to current/default)
        /// </summary>
        public void AdjustCriticalMissThreshold(Actor actor, int adjustment)
        {
            int current = GetCriticalMissThreshold(actor);
            SetCriticalMissThreshold(actor, current + adjustment);
        }

        /// <summary>
        /// Sets the critical hit threshold for an actor
        /// </summary>
        public void SetCriticalHitThreshold(Actor actor, int threshold)
        {
            if (!_actorThresholds.ContainsKey(actor))
            {
                _actorThresholds[actor] = new ThresholdModifiers();
            }
            _actorThresholds[actor].CriticalHitThreshold = threshold;
        }

        /// <summary>
        /// Adjusts the critical hit threshold for an actor (adds to current/default)
        /// </summary>
        public void AdjustCriticalHitThreshold(Actor actor, int adjustment)
        {
            int current = GetCriticalHitThreshold(actor);
            SetCriticalHitThreshold(actor, current + adjustment);
        }

        /// <summary>
        /// Sets the combo threshold for an actor
        /// </summary>
        public void SetComboThreshold(Actor actor, int threshold)
        {
            if (!_actorThresholds.ContainsKey(actor))
            {
                _actorThresholds[actor] = new ThresholdModifiers();
            }
            _actorThresholds[actor].ComboThreshold = threshold;
        }

        /// <summary>
        /// Adjusts the combo threshold for an actor (adds to current/default)
        /// </summary>
        public void AdjustComboThreshold(Actor actor, int adjustment)
        {
            int current = GetComboThreshold(actor);
            SetComboThreshold(actor, current + adjustment);
        }

        /// <summary>
        /// Sets the hit threshold for an actor
        /// </summary>
        public void SetHitThreshold(Actor actor, int threshold)
        {
            if (!_actorThresholds.ContainsKey(actor))
            {
                _actorThresholds[actor] = new ThresholdModifiers();
            }
            _actorThresholds[actor].HitThreshold = threshold;
        }

        /// <summary>
        /// Adjusts the hit threshold for an actor (adds to current/default)
        /// </summary>
        public void AdjustHitThreshold(Actor actor, int adjustment)
        {
            int current = GetHitThreshold(actor);
            SetHitThreshold(actor, current + adjustment);
        }

        /// <summary>
        /// Resets all thresholds for an actor to defaults
        /// </summary>
        public void ResetThresholds(Actor actor)
        {
            _actorThresholds.Remove(actor);
        }

        /// <summary>
        /// Clears all threshold modifications (useful for testing)
        /// </summary>
        public void Clear()
        {
            _actorThresholds.Clear();
        }
    }
}

