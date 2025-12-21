using System;

namespace RPGGame
{
    /// <summary>
    /// Roll modification properties for actions
    /// </summary>
    public class RollModificationProperties
    {
        private int _additive = 0;
        private double _multiplier = 1.0;
        private int _min = 1;
        private int _max = 20;
        private double _rerollChance = 0.0;
        private int _explodingDiceThreshold = 20;
        private int _multipleDiceCount = 1;
        private int _criticalMissThresholdOverride = 0;
        private int _criticalHitThresholdOverride = 0;
        private int _comboThresholdOverride = 0;
        private int _hitThresholdOverride = 0;
        
        // Threshold adjustments (adds to current/default threshold)
        private int _criticalMissThresholdAdjustment = 0;
        private int _criticalHitThresholdAdjustment = 0;
        private int _comboThresholdAdjustment = 0;
        private int _hitThresholdAdjustment = 0;
        
        // Whether to apply threshold adjustments to both source and target
        private bool _applyThresholdAdjustmentsToBoth = false;

        public int Additive
        {
            get => _additive;
            set => _additive = value; // No range restriction for additive
        }

        public double Multiplier    
        {
            get => _multiplier;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(Multiplier), "Multiplier cannot be negative");
                _multiplier = value;
            }
        }

        public int Min
        {
            get => _min;
            set
            {
                if (value < 1 || value > 20)
                    throw new ArgumentOutOfRangeException(nameof(Min), "Min must be between 1 and 20");
                if (value > _max)
                    throw new ArgumentException("Min cannot be greater than Max");
                _min = value;
            }
        }

        public int Max
        {
            get => _max;
            set
            {
                if (value < 1 || value > 20)
                    throw new ArgumentOutOfRangeException(nameof(Max), "Max must be between 1 and 20");
                if (value < _min)
                    throw new ArgumentException("Max cannot be less than Min");
                _max = value;
            }
        }

        public bool AllowReroll { get; set; } = false;

        public double RerollChance
        {
            get => _rerollChance;
            set
            {
                if (value < 0.0 || value > 1.0)
                    throw new ArgumentOutOfRangeException(nameof(RerollChance), "RerollChance must be between 0.0 and 1.0");
                _rerollChance = value;
            }
        }

        public bool ExplodingDice { get; set; } = false;

        public int ExplodingDiceThreshold
        {
            get => _explodingDiceThreshold;
            set
            {
                if (value < 1 || value > 20)
                    throw new ArgumentOutOfRangeException(nameof(ExplodingDiceThreshold), "ExplodingDiceThreshold must be between 1 and 20");
                _explodingDiceThreshold = value;
            }
        }

        public int MultipleDiceCount
        {
            get => _multipleDiceCount;
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(MultipleDiceCount), "MultipleDiceCount must be at least 1");
                _multipleDiceCount = value;
            }
        }

        public string MultipleDiceMode { get; set; } = "Sum";

        public int CriticalHitThresholdOverride
        {
            get => _criticalHitThresholdOverride;
            set
            {
                if (value != 0 && (value < 1 || value > 20))
                    throw new ArgumentOutOfRangeException(nameof(CriticalHitThresholdOverride), "CriticalHitThresholdOverride must be 0 (use default) or between 1 and 20");
                _criticalHitThresholdOverride = value;
            }
        }

        public int ComboThresholdOverride
        {
            get => _comboThresholdOverride;
            set
            {
                if (value != 0 && (value < 1 || value > 20))
                    throw new ArgumentOutOfRangeException(nameof(ComboThresholdOverride), "ComboThresholdOverride must be 0 (use default) or between 1 and 20");
                _comboThresholdOverride = value;
            }
        }

        public int HitThresholdOverride
        {
            get => _hitThresholdOverride;
            set
            {
                if (value != 0 && (value < 1 || value > 20))
                    throw new ArgumentOutOfRangeException(nameof(HitThresholdOverride), "HitThresholdOverride must be 0 (use default) or between 1 and 20");
                _hitThresholdOverride = value;
            }
        }

        public int CriticalMissThresholdOverride
        {
            get => _criticalMissThresholdOverride;
            set
            {
                if (value != 0 && (value < 1 || value > 20))
                    throw new ArgumentOutOfRangeException(nameof(CriticalMissThresholdOverride), "CriticalMissThresholdOverride must be 0 (use default) or between 1 and 20");
                _criticalMissThresholdOverride = value;
            }
        }

        /// <summary>
        /// Adjustment to critical miss threshold (adds to current/default)
        /// </summary>
        public int CriticalMissThresholdAdjustment
        {
            get => _criticalMissThresholdAdjustment;
            set => _criticalMissThresholdAdjustment = value;
        }

        /// <summary>
        /// Adjustment to critical hit threshold (adds to current/default)
        /// </summary>
        public int CriticalHitThresholdAdjustment
        {
            get => _criticalHitThresholdAdjustment;
            set => _criticalHitThresholdAdjustment = value;
        }

        /// <summary>
        /// Adjustment to combo threshold (adds to current/default)
        /// </summary>
        public int ComboThresholdAdjustment
        {
            get => _comboThresholdAdjustment;
            set => _comboThresholdAdjustment = value;
        }

        /// <summary>
        /// Adjustment to hit threshold (adds to current/default)
        /// Positive values increase miss chance (harder to hit)
        /// Negative values decrease miss chance (easier to hit)
        /// </summary>
        public int HitThresholdAdjustment
        {
            get => _hitThresholdAdjustment;
            set => _hitThresholdAdjustment = value;
        }

        /// <summary>
        /// If true, threshold adjustments apply to both source and target actors
        /// </summary>
        public bool ApplyThresholdAdjustmentsToBoth
        {
            get => _applyThresholdAdjustmentsToBoth;
            set => _applyThresholdAdjustmentsToBoth = value;
        }

        /// <summary>
        /// Validates all properties for consistency
        /// </summary>
        public void Validate()
        {
            if (_min > _max)
                throw new ArgumentException("Min cannot be greater than Max");
            if (_rerollChance > 0 && !AllowReroll)
                throw new ArgumentException("RerollChance is set but AllowReroll is false");
        }
    }
}

