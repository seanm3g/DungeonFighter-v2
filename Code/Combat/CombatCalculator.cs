using System;
using System.Collections.Generic;
using RPGGame.Actions.RollModification;
using RPGGame.Combat.Calculators;

namespace RPGGame
{
    /// <summary>
    /// Facade for combat calculations - delegates to specialized calculators
    /// Handles combat calculations including damage, hit/miss logic, and roll bonuses
    /// </summary>
    public static class CombatCalculator
    {
        /// <summary>
        /// Calculates raw damage before armor reduction
        /// </summary>
        public static int CalculateRawDamage(Actor attacker, Action? action = null, double comboAmplifier = 1.0, double damageMultiplier = 1.0, int roll = 0)
        {
            return DamageCalculator.CalculateRawDamage(attacker, action, comboAmplifier, damageMultiplier, roll);
        }

        /// <summary>
        /// Calculates damage dealt by an attacker to a target
        /// </summary>
        public static int CalculateDamage(Actor attacker, Actor target, Action? action = null, double comboAmplifier = 1.0, double damageMultiplier = 1.0, int rollBonus = 0, int roll = 0, bool showWeakenedMessage = true)
        {
            return DamageCalculator.CalculateDamage(attacker, target, action, comboAmplifier, damageMultiplier, rollBonus, roll, showWeakenedMessage);
        }

        /// <summary>
        /// Calculates hit/miss based on roll value only
        /// 1-5: Miss, 6-13: Regular attack, 14-19: Combo, 20: Combo + Critical
        /// </summary>
        public static bool CalculateHit(Actor attacker, Actor target, int rollBonus, int roll)
        {
            return HitCalculator.CalculateHit(attacker, target, rollBonus, roll);
        }

        /// <summary>
        /// Calculates total roll bonus for an attack
        /// </summary>
        /// <param name="attacker">The attacking Actor</param>
        /// <param name="action">The action being performed</param>
        /// <param name="comboActions">Current combo actions for scaling</param>
        /// <param name="comboStep">Current combo step</param>
        /// <returns>Total roll bonus</returns>
        public static int CalculateRollBonus(Actor attacker, Action? action, List<Action> comboActions, int comboStep)
        {
            int totalBonus = 0;
            
            // Base action roll bonus
            // Only apply immediate roll bonus if there's no duration (duration-based bonuses are handled separately)
            if (action != null)
            {
                // Only add roll bonus if it doesn't have a duration (duration-based bonuses are set for future rolls)
                if (action.Advanced.RollBonusDuration == 0)
                {
                    totalBonus += action.Advanced.RollBonus;
                }
                
                // Apply combo scaling bonuses
                if (action.Tags.Contains("comboScaling"))
                {
                    // Scale with combo length (METEOR, SHADOW STRIKE)
                    totalBonus += comboActions.Count;
                }
                else if (action.Tags.Contains("comboStepScaling"))
                {
                    // Scale with combo step position (HEROIC STRIKE)
                    totalBonus += (comboStep % comboActions.Count) + 1;
                }
                else                 if (action.Tags.Contains("comboAmplificationScaling"))
                {
                    // Scale with combo amplification (BERSERKER RAGE)
                    if (attacker is Character characterAttacker)
                    {
                        totalBonus += (int)(characterAttacker.GetComboAmplifier() * 2);
                    }
                }
            }
            
            // Add intelligence bonus
            if (attacker is Character character)
            {
                totalBonus += character.GetIntelligenceRollBonus();
                totalBonus += character.GetModificationRollBonus();
                totalBonus += character.GetEquipmentRollBonus();
                
                // Add temporary roll bonus (consumes one turn per use)
                totalBonus += character.Effects.ConsumeTempRollBonus();
            }
            else if (attacker is Enemy enemy)
            {
                totalBonus += enemy.GetIntelligenceRollBonus();
            }
            
            // Apply roll penalty (for effects like Dust Cloud)
            totalBonus -= attacker.RollPenalty;
            
            return totalBonus;
        }

        /// <summary>
        /// Calculates critical hit chance and damage
        /// </summary>
        /// <param name="attacker">The attacking Actor</param>
        /// <param name="roll">The attack roll</param>
        /// <returns>True if critical hit, false otherwise</returns>
        public static bool IsCriticalHit(Actor attacker, int roll)
        {
            // Natural 20 or higher is always a critical hit - FIXED: Allow 20+
            if (roll >= 20)
            {
                return true;
            }
            
            // Check for critical hit chance from equipment/modifications
            double criticalChance = 0.0;
            if (attacker is Character character)
            {
                // For now, use a simple critical hit chance calculation
                // This would need to be implemented based on equipment/modifications
                criticalChance = 0.05; // 5% base critical hit chance
            }
            
            // Roll for critical hit chance
            if (criticalChance > 0.0)
            {
                double rollValue = Dice.Roll(1, 100) / 100.0;
                return rollValue < criticalChance;
            }
            
            return false;
        }

        /// <summary>
        /// Calculates damage reduction from armor and other sources
        /// </summary>
        public static int ApplyDamageReduction(Actor target, int damage)
        {
            return DamageCalculator.ApplyDamageReduction(target, damage);
        }

        /// <summary>
        /// Calculates status effect application chance
        /// </summary>
        public static bool CalculateStatusEffectChance(Action action, Actor attacker, Actor target)
        {
            return StatusEffectCalculator.CalculateStatusEffectChance(action, attacker, target);
        }
        
        /// <summary>
        /// Calculates attack speed for any Actor (shared logic)
        /// </summary>
        public static double CalculateAttackSpeed(Actor actor)
        {
            return SpeedCalculator.CalculateAttackSpeed(actor);
        }

    }
}


