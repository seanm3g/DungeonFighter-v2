using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Handles combat calculations for characters including combo amplification, attack speed, and combat bonuses
    /// Extracted from Character class to follow Single Responsibility Principle
    /// </summary>
    public class CharacterCombatCalculator
    {
        private readonly Character character;

        public CharacterCombatCalculator(Character character)
        {
            this.character = character;
        }

        /// <summary>
        /// Gets the base combo amplifier based on Technique stat
        /// </summary>
        /// <returns>Base combo amplifier value</returns>
        public double GetComboAmplifier()
        {
            var tuning = GameConfiguration.Instance;

            // Clamp Technique to valid range
            int clampedTech = Math.Max(1, Math.Min(tuning.ComboSystem.ComboAmplifierMaxTech, character.Technique));

            // Linear scaling from 1.01 at Technique 1 to ComboAmplifierAtTech5 at Technique 5
            if (clampedTech <= 5)
            {
                double lowTechProgress = (clampedTech - 1) / 4.0; // Scale from 1 to 5 (4 point range)
                double baseAmplifier = 1.01; // Start at 1.01x for Technique 1
                double lowAmpRange = tuning.ComboSystem.ComboAmplifierAtTech5 - baseAmplifier;
                return baseAmplifier + (lowAmpRange * lowTechProgress);
            }

            // Linear interpolation between ComboAmplifierAtTech5 and ComboAmplifierMax for Technique > 5
            double techRange = tuning.ComboSystem.ComboAmplifierMaxTech - 5;
            double highAmpRange = tuning.ComboSystem.ComboAmplifierMax - tuning.ComboSystem.ComboAmplifierAtTech5;
            double highTechProgress = (clampedTech - 5) / techRange;
            return tuning.ComboSystem.ComboAmplifierAtTech5 + (highAmpRange * highTechProgress);
        }

        /// <summary>
        /// Gets the current combo amplification that will be applied
        /// Step 0 adds no bonus (1.0x), bonus starts at Step 1+
        /// </summary>
        /// <returns>Current combo amplification</returns>
        public double GetCurrentComboAmplification()
        {
            var comboActions = character.GetComboActions();
            if (comboActions.Count == 0) return GetComboAmplifier();

            int currentStep = character.ComboStep % comboActions.Count;
            double baseAmp = GetComboAmplifier();
            // Step 0 adds no bonus, bonus starts at Step 1+
            int amplificationStep = currentStep;
            return Math.Pow(baseAmp, amplificationStep);
        }

        /// <summary>
        /// Gets the amplification that will be applied when the next combo action is executed
        /// This is used for display purposes to show what amplification the player will get
        /// Step 0 adds no bonus (1.0x), bonus starts at Step 1+
        /// </summary>
        /// <returns>Next combo amplification</returns>
        public double GetNextComboAmplification()
        {
            var comboActions = character.GetComboActions();
            if (comboActions.Count == 0) return GetComboAmplifier();

            int currentStep = character.ComboStep % comboActions.Count;
            double baseAmp = GetComboAmplifier();
            // Show what amplification will be applied when the combo executes
            // Step 0 adds no bonus, bonus starts at Step 1+
            int nextStep = (currentStep + 1) % comboActions.Count;
            int amplificationStep = nextStep;
            return Math.Pow(baseAmp, amplificationStep);
        }

        /// <summary>
        /// Gets combo information as a formatted string
        /// </summary>
        /// <returns>Formatted combo information</returns>
        public string GetComboInfo()
        {
            var comboActions = character.GetComboActions();
            if (comboActions.Count == 0) return "No combo actions available";

            int currentStep = character.ComboStep % comboActions.Count;
            double baseAmp = GetComboAmplifier();
            // Step 0 adds no bonus, bonus starts at Step 1+
            double currentAmp = Math.Pow(baseAmp, currentStep);

            return $"Amplification: {currentAmp:F2}x";
        }

        /// <summary>
        /// Calculates how many attacks the character can perform per turn
        /// </summary>
        /// <returns>Number of attacks per turn</returns>
        public int GetAttacksPerTurn()
        {
            double attackTime = GetTotalAttackSpeed();
            // Attack time is in seconds, so we need to calculate how many attacks fit in a reasonable turn duration
            // Assuming a turn is roughly 10 seconds, calculate attacks per turn
            double turnDuration = 10.0; // 10 second turns
            int attacks = (int)Math.Floor(turnDuration / attackTime);
            return Math.Max(1, attacks); // Always at least 1 attack
        }

        /// <summary>
        /// Gets the total attack speed including all bonuses and modifications
        /// </summary>
        /// <returns>Total attack speed in seconds</returns>
        public double GetTotalAttackSpeed()
        {
            // Use shared attack speed calculation logic
            return CombatCalculator.CalculateAttackSpeed(character);
        }

        /// <summary>
        /// Calculates the intelligence roll bonus
        /// </summary>
        /// <returns>Intelligence roll bonus</returns>
        public int GetIntelligenceRollBonus()
        {
            var tuning = GameConfiguration.Instance;
            return character.Intelligence / tuning.Attributes.IntelligenceRollBonusPer; // Every X points of INT gives +1 to rolls
        }

        /// <summary>
        /// Calculates total damage including all bonuses
        /// </summary>
        /// <returns>Total damage value</returns>
        public int GetTotalDamage()
        {
            int weaponDamage = (character.Weapon is WeaponItem w) ? w.GetTotalDamage() : 0;
            int equipmentDamageBonus = character.Equipment.GetEquipmentDamageBonus();
            int modificationDamageBonus = character.Equipment.GetModificationDamageBonus();
            return character.GetEffectiveStrength() + weaponDamage + equipmentDamageBonus + modificationDamageBonus;
        }

        /// <summary>
        /// Calculates total roll bonus from all sources
        /// </summary>
        /// <returns>Total roll bonus</returns>
        public int GetTotalRollBonus()
        {
            return GetIntelligenceRollBonus() + character.Equipment.GetModificationRollBonus() + character.Equipment.GetEquipmentRollBonus();
        }

        /// <summary>
        /// Calculates total armor from all sources
        /// </summary>
        /// <returns>Total armor value</returns>
        public int GetTotalArmor()
        {
            return character.Equipment.GetTotalArmor();
        }

        /// <summary>
        /// Gets magic find bonus from equipment
        /// </summary>
        /// <returns>Magic find bonus</returns>
        public int GetMagicFind()
        {
            return character.Equipment.GetMagicFind();
        }

        /// <summary>
        /// Checks if the character meets a stat threshold
        /// </summary>
        /// <param name="statType">Type of stat to check</param>
        /// <param name="threshold">Threshold value</param>
        /// <returns>True if stat meets threshold</returns>
        public bool MeetsStatThreshold(string statType, double threshold)
        {
            return character.Stats.MeetsStatThreshold(statType, threshold, character.Equipment.GetEquipmentStatBonus(statType), character.Equipment.GetModificationGodlikeBonus());
        }

        /// <summary>
        /// Sets technique for testing purposes
        /// </summary>
        /// <param name="value">Technique value to set</param>
        public void SetTechniqueForTesting(int value)
        {
            character.Stats.SetTechniqueForTesting(value);
        }

        /// <summary>
        /// Gets the character's combat description
        /// </summary>
        /// <returns>Combat description string</returns>
        public string GetCombatDescription()
        {
            return $"Level {character.Level} (Health: {character.CurrentHealth}/{character.MaxHealth}) (STR: {character.Strength}, AGI: {character.Agility}, TEC: {character.Technique}, INT: {character.Intelligence})";
        }

        /// <summary>
        /// Gets detailed combat stats for display
        /// </summary>
        /// <returns>Detailed combat stats string</returns>
        public string GetDetailedCombatStats()
        {
            int weaponDamage = (character.Weapon is WeaponItem w) ? w.GetTotalDamage() : 0;
            int equipmentDamageBonus = character.Equipment.GetEquipmentDamageBonus();
            int modificationDamageBonus = character.Equipment.GetModificationDamageBonus();
            int damage = GetTotalDamage();
            double attackSpeed = GetTotalAttackSpeed();
            int armor = GetTotalArmor();
            int totalRollBonus = GetTotalRollBonus();
            double nextAmplification = GetNextComboAmplification();
            int magicFind = GetMagicFind();

            string stats = $"Damage: {damage} (STR: {character.GetEffectiveStrength()} + Weapon: {weaponDamage} + Equipment: {equipmentDamageBonus} + Mods: {modificationDamageBonus})  Attack Time: {attackSpeed:0.00}s  Amplification: {nextAmplification:F2}x  Roll Bonus: +{totalRollBonus}  Armor: {armor}";
            
            if (magicFind > 0)
            {
                stats += $"  Magic Find: +{magicFind} (improves rare item drop chances)";
            }

            return stats;
        }
    }
}
