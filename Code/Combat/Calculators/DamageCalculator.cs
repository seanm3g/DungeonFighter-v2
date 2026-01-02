using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Actions.RollModification;

namespace RPGGame.Combat.Calculators
{
    /// <summary>
    /// Handles damage calculations including raw damage, final damage, and damage reduction
    /// Includes caching layer for performance optimization
    /// </summary>
    public static class DamageCalculator
    {
        // Cache for raw damage calculations
        private static readonly Dictionary<(Actor, Action?, double, double, int), int> _rawDamageCache = new();
        
        // Cache for final damage calculations (includes target armor)
        private static readonly Dictionary<(Actor, Actor, Action?, double, double, int, int), int> _finalDamageCache = new();
        
        // Lock object for thread-safe access to caches
        private static readonly object _cacheLock = new object();
        
        // Cache statistics for monitoring
        private static int _rawCacheHits = 0;
        private static int _rawCacheMisses = 0;
        private static int _finalCacheHits = 0;
        private static int _finalCacheMisses = 0;
        
        // Maximum cache size to prevent memory bloat
        private const int MaxCacheSize = 1000;
        
        /// <summary>
        /// Invalidates all cache entries for a specific actor
        /// Call this when actor stats, equipment, or modifications change
        /// </summary>
        public static void InvalidateCache(Actor actor)
        {
            lock (_cacheLock)
            {
                // Make a copy of keys to avoid enumeration issues during concurrent modifications
                var keysToRemove = _rawDamageCache.Keys
                    .Where(key => key.Item1 == actor)
                    .ToList();
                
                foreach (var key in keysToRemove)
                {
                    _rawDamageCache.Remove(key);
                }
                
                var finalKeysToRemove = _finalDamageCache.Keys
                    .Where(key => key.Item1 == actor || key.Item2 == actor)
                    .ToList();
                
                foreach (var key in finalKeysToRemove)
                {
                    _finalDamageCache.Remove(key);
                }
            }
        }
        
        /// <summary>
        /// Clears all caches (useful for testing or memory management)
        /// </summary>
        public static void ClearAllCaches()
        {
            lock (_cacheLock)
            {
                _rawDamageCache.Clear();
                _finalDamageCache.Clear();
                _rawCacheHits = 0;
                _rawCacheMisses = 0;
                _finalCacheHits = 0;
                _finalCacheMisses = 0;
            }
        }
        
        /// <summary>
        /// Gets cache statistics for monitoring
        /// </summary>
        public static (int rawHits, int rawMisses, int finalHits, int finalMisses, double rawHitRate, double finalHitRate) GetCacheStats()
        {
            double rawHitRate = (_rawCacheHits + _rawCacheMisses) > 0 
                ? (double)_rawCacheHits / (_rawCacheHits + _rawCacheMisses) 
                : 0.0;
            double finalHitRate = (_finalCacheHits + _finalCacheMisses) > 0 
                ? (double)_finalCacheHits / (_finalCacheHits + _finalCacheMisses) 
                : 0.0;
            
            return (_rawCacheHits, _rawCacheMisses, _finalCacheHits, _finalCacheMisses, rawHitRate, finalHitRate);
        }
        /// <summary>
        /// Calculates raw damage before armor reduction
        /// Uses caching for performance optimization
        /// </summary>
        public static int CalculateRawDamage(Actor attacker, Action? action = null, double comboAmplifier = 1.0, double damageMultiplier = 1.0, int roll = 0)
        {
            // Check cache first
            var cacheKey = (attacker, action, comboAmplifier, damageMultiplier, roll);
            int cachedDamage;
            bool cacheHit;
            
            lock (_cacheLock)
            {
                cacheHit = _rawDamageCache.TryGetValue(cacheKey, out cachedDamage);
                if (cacheHit)
                {
                    _rawCacheHits++;
                    return cachedDamage;
                }
                _rawCacheMisses++;
            }
            // Get base damage from attacker
            int baseDamage = 0;
            if (attacker is Character character)
            {
                baseDamage = character.GetEffectiveStrength();
                
                // Add weapon damage if attacker has a weapon
                if (character.Weapon is WeaponItem weapon)
                {
                    baseDamage += weapon.GetTotalDamage();
                }
                
                // Add equipment damage bonus
                baseDamage += character.GetEquipmentDamageBonus();
                
                // Add modification damage bonus
                baseDamage += character.GetModificationDamageBonus();
            }
            else if (attacker is Enemy enemy)
            {
                // For enemies, use strength + weapon damage (same as heroes)
                baseDamage = enemy.GetEffectiveStrength();
                
                // Add weapon damage if enemy has a weapon
                if (enemy.Weapon is WeaponItem weapon)
                {
                    baseDamage += weapon.GetTotalDamage();
                }
            }
            
            // Apply action damage multiplier if action is provided
            double actionMultiplier = action?.DamageMultiplier ?? 1.0;
            
            // Check for conditional damage multiplier based on health threshold
            if (action != null && action.Advanced.HealthThreshold > 0.0 && action.Advanced.ConditionalDamageMultiplier > 1.0)
            {
                // Check if attacker's health meets the threshold (works for both Character and Enemy since Enemy inherits from Character)
                if (attacker is Character attackerCharacter && attackerCharacter.MeetsHealthThreshold(action.Advanced.HealthThreshold))
                {
                    actionMultiplier *= action.Advanced.ConditionalDamageMultiplier;
                }
            }
            
            // Calculate total damage before armor
            double totalDamage = (baseDamage * actionMultiplier * comboAmplifier * damageMultiplier);
            
            // Get combat configuration
            var combatConfig = GameConfiguration.Instance.Combat;
            var combatBalance = GameConfiguration.Instance.CombatBalance;
            
            // Apply roll-based damage scaling
            if (roll > 0)
            {
                // Get threshold from threshold manager if available, otherwise use config
                int criticalThreshold = combatConfig.CriticalHitThreshold;
                if (attacker != null)
                {
                    criticalThreshold = RollModificationManager.GetThresholdManager().GetCriticalHitThreshold(attacker);
                }
                
                // Critical hit on total roll of threshold or higher - FIXED: Allow 20+
                if (roll >= criticalThreshold)
                {
                    if (GameConfiguration.IsDebugEnabled)
                    {
                        if (!ActionExecutor.DisableCombatDebugOutput)
                        {
                        }
                    }
                    totalDamage *= combatBalance.CriticalHitDamageMultiplier;
                    if (GameConfiguration.IsDebugEnabled)
                    {
                        if (!ActionExecutor.DisableCombatDebugOutput)
                        {
                        }
                    }
                }
                else
                {
                    // Enhanced damage scaling based on roll using RollSystem configuration
                    // Only apply if not a critical hit (roll < 20)
                    var rollSystem = GameConfiguration.Instance.RollSystem;
                    var rollMultipliers = combatBalance.RollDamageMultipliers;
                    if (roll >= rollSystem.ComboThreshold.Min)
                    {
                        totalDamage *= rollMultipliers.ComboRollDamageMultiplier;
                    }
                    else if (roll >= rollSystem.BasicAttackThreshold.Min) // Normal attack range (6-13)
                    {
                        totalDamage *= rollMultipliers.BasicRollDamageMultiplier;
                    }
                }
            }
            
            int result = (int)totalDamage;
            
            // Cache the result (with size limit)
            lock (_cacheLock)
            {
                if (_rawDamageCache.Count < MaxCacheSize)
                {
                    _rawDamageCache[cacheKey] = result;
                }
            }
            
            return result;
        }

        /// <summary>
        /// Calculates damage dealt by an attacker to a target
        /// Uses caching for performance optimization
        /// </summary>
        public static int CalculateDamage(Actor attacker, Actor target, Action? action = null, double comboAmplifier = 1.0, double damageMultiplier = 1.0, int rollBonus = 0, int roll = 0, bool showWeakenedMessage = true)
        {
            // Check cache first (cache key includes target armor state via target reference)
            var cacheKey = (attacker, target, action, comboAmplifier, damageMultiplier, roll, showWeakenedMessage ? 1 : 0);
            int cachedDamage;
            bool cacheHit;
            
            lock (_cacheLock)
            {
                cacheHit = _finalDamageCache.TryGetValue(cacheKey, out cachedDamage);
                if (cacheHit)
                {
                    _finalCacheHits++;
                    return cachedDamage;
                }
                _finalCacheMisses++;
            }
            
            // Calculate raw damage before armor
            int totalDamage = CalculateRawDamage(attacker, action, comboAmplifier, damageMultiplier, roll);
            
            // Apply tag-based damage modification
            if (action != null)
            {
                double tagModifier = Combat.TagDamageCalculator.GetDamageModifier(action, target);
                totalDamage = (int)(totalDamage * tagModifier);
            }
            
            // Get target's armor
            int targetArmor = 0;
            if (target is Enemy targetEnemy)
            {
                // For enemies, use the Armor property directly to avoid inheritance issues
                targetArmor = targetEnemy.Armor;
            }
            else if (target is Character targetCharacter)
            {
                targetArmor = targetCharacter.GetTotalArmor();
            }
            
            // Calculate final damage after armor reduction
            int finalDamage;

            // Get combat configuration
            var combatBalance = GameConfiguration.Instance.CombatBalance;

            // Apply simple armor reduction (flat reduction)
            finalDamage = Math.Max(GameConfiguration.Instance.Combat.MinimumDamage, (int)totalDamage - targetArmor);
            
            // Apply weakened effect if target is weakened
            if (target.IsWeakened && showWeakenedMessage)
            {
                finalDamage = (int)(finalDamage * 1.5); // 50% more damage to weakened targets
            }
            
            // Cache the result (with size limit)
            lock (_cacheLock)
            {
                if (_finalDamageCache.Count < MaxCacheSize)
                {
                    _finalDamageCache[cacheKey] = finalDamage;
                }
            }
            
            return finalDamage;
        }

        /// <summary>
        /// Calculates damage reduction from armor and other sources
        /// </summary>
        public static int ApplyDamageReduction(Actor target, int damage)
        {
            // Get base armor reduction
            int armorReduction = 0;
            if (target is Enemy targetEnemy)
            {
                // For enemies, use the Armor property directly to avoid inheritance issues
                armorReduction = targetEnemy.Armor;
            }
            else if (target is Character targetCharacter)
            {
                armorReduction = targetCharacter.GetTotalArmor();
            }
            
            // Apply damage reduction from effects
            double damageReductionMultiplier = 1.0;
            if (target.DamageReduction > 0)
            {
                damageReductionMultiplier = 1.0 - (target.DamageReduction / 100.0);
            }
            
            // Apply simple armor reduction (flat reduction) with damage reduction multiplier
            int finalDamage = Math.Max(GameConfiguration.Instance.Combat.MinimumDamage, (int)((damage - armorReduction) * damageReductionMultiplier));
            
            return finalDamage;
        }
    }
}

