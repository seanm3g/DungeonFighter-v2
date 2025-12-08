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
            public int? CriticalHitThreshold { get; set; } // Default: 20
            public int? ComboThreshold { get; set; } // Default: 14
            public int? HitThreshold { get; set; } // Default: 5

            public ThresholdModifiers()
            {
                // Initialize with defaults from GameConfiguration
                var config = GameConfiguration.Instance;
                CriticalHitThreshold = config.Combat.CriticalHitThreshold;
                ComboThreshold = config.RollSystem.ComboThreshold.Min;
                HitThreshold = config.RollSystem.BasicAttackThreshold.Min;
            }
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
        /// </summary>
        public int GetHitThreshold(Actor actor)
        {
            if (_actorThresholds.TryGetValue(actor, out var modifiers) && modifiers.HitThreshold.HasValue)
            {
                return modifiers.HitThreshold.Value;
            }
            
            return GameConfiguration.Instance.RollSystem.BasicAttackThreshold.Min;
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

