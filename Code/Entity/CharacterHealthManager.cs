using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Manages character health, healing, damage, and health-related calculations
    /// Extracted from Character class to follow Single Responsibility Principle
    /// </summary>
    public class CharacterHealthManager
    {
        private readonly Character character;

        public CharacterHealthManager(Character character)
        {
            this.character = character;
        }

        // Health properties
        public int CurrentHealth { get; set; }
        public int MaxHealth { get; set; }

        /// <summary>
        /// Makes the character take damage with shield and damage reduction calculations
        /// </summary>
        /// <param name="amount">Base damage amount</param>
        public void TakeDamage(int amount)
        {
            TakeDamageWithNotifications(amount);
        }

        /// <summary>
        /// Makes the character take damage and returns notification messages
        /// </summary>
        /// <param name="amount">Base damage amount</param>
        /// <returns>List of notification messages</returns>
        public List<string> TakeDamageWithNotifications(int amount)
        {
            int originalAmount = amount;
            bool shieldUsed = false;

            // Apply shield damage reduction (50% reduction) if shield is active
            if (character.Effects.HasShield)
            {
                int reducedAmount = (int)(amount * 0.5); // Reduce damage by half
                int shieldReduction = amount - reducedAmount;
                amount = reducedAmount;
                character.Effects.HasShield = false; // Consume the shield
                shieldUsed = true;

                // Display shield message
                // Use TextDisplayIntegration for consistent entity tracking
                string shieldMessage = $"[{character.Name}]'s Arcane Shield reduces damage by {shieldReduction}!";
                TextDisplayIntegration.DisplayCombatAction(shieldMessage, new List<string>(), new List<string>(), character.Name);
            }

            // Apply damage reduction if active
            if (character.DamageReduction > 0)
            {
                amount = (int)(amount * (1.0 - character.DamageReduction));
            }

            CurrentHealth = Math.Max(0, CurrentHealth - amount);

            // Store shield usage for display purposes
            if (shieldUsed)
            {
                character.Effects.LastShieldReduction = originalAmount - amount;
            }

            // Check for health milestones and leadership changes
            // Note: Health milestone checking is now handled by CombatManager
            return new List<string>(); // Return empty list since milestone checking moved to CombatManager
        }

        /// <summary>
        /// Calculates damage with shield reduction without actually applying it
        /// </summary>
        /// <param name="amount">Base damage amount</param>
        /// <returns>Tuple of (final damage, shield reduction, shield used)</returns>
        public (int finalDamage, int shieldReduction, bool shieldUsed) CalculateDamageWithShield(int amount)
        {
            int originalAmount = amount;
            bool shieldUsed = false;
            int shieldReduction = 0;

            // Apply shield damage reduction (50% reduction) if shield is active
            if (character.Effects.HasShield)
            {
                int reducedAmount = (int)(amount * 0.5); // Reduce damage by half
                shieldReduction = amount - reducedAmount;
                amount = reducedAmount;
                shieldUsed = true;
            }

            // Apply damage reduction if active
            if (character.DamageReduction > 0)
            {
                amount = (int)(amount * (1.0 - character.DamageReduction));
            }

            return (amount, shieldReduction, shieldUsed);
        }

        /// <summary>
        /// Heals the character by the specified amount
        /// </summary>
        /// <param name="amount">Amount to heal</param>
        public void Heal(int amount)
        {
            CurrentHealth = Math.Min(GetEffectiveMaxHealth(), CurrentHealth + amount);
        }

        /// <summary>
        /// Checks if the character is alive
        /// </summary>
        public bool IsAlive => CurrentHealth > 0;

        /// <summary>
        /// Gets the effective maximum health including equipment bonuses
        /// </summary>
        /// <returns>Effective maximum health</returns>
        public int GetEffectiveMaxHealth()
        {
            return MaxHealth + character.Equipment.GetEquipmentHealthBonus();
        }

        /// <summary>
        /// Gets the current health percentage
        /// </summary>
        /// <returns>Health percentage as a decimal (0.0 to 1.0)</returns>
        public double GetHealthPercentage()
        {
            return (double)CurrentHealth / GetEffectiveMaxHealth();
        }

        /// <summary>
        /// Checks if the character meets a health threshold
        /// </summary>
        /// <param name="threshold">Health threshold (0.0 to 1.0)</param>
        /// <returns>True if health percentage is at or below threshold</returns>
        public bool MeetsHealthThreshold(double threshold)
        {
            return GetHealthPercentage() <= threshold;
        }

        /// <summary>
        /// Applies a health multiplier to maximum health
        /// </summary>
        /// <param name="multiplier">Health multiplier</param>
        public void ApplyHealthMultiplier(double multiplier)
        {
            MaxHealth = (int)(MaxHealth * multiplier);
            CurrentHealth = MaxHealth;
        }

        /// <summary>
        /// Adjusts current health when equipment changes affect maximum health
        /// </summary>
        /// <param name="oldMaxHealth">Previous maximum health</param>
        /// <param name="newMaxHealth">New maximum health</param>
        public void AdjustHealthForMaxHealthChange(int oldMaxHealth, int newMaxHealth)
        {
            if (newMaxHealth > oldMaxHealth)
            {
                // If max health increased, heal to full health
                CurrentHealth = newMaxHealth;
            }
            else if (newMaxHealth < oldMaxHealth)
            {
                // If max health decreased, maintain health percentage but cap at new max
                double healthPercentage = (double)CurrentHealth / oldMaxHealth;
                int newCurrentHealth = (int)(newMaxHealth * healthPercentage);
                CurrentHealth = Math.Min(newCurrentHealth, newMaxHealth);
            }
        }

        /// <summary>
        /// Processes health regeneration for the character
        /// </summary>
        public void ProcessHealthRegeneration()
        {
            int healthRegen = character.Equipment.GetEquipmentHealthRegenBonus();
            if (healthRegen > 0 && CurrentHealth < GetEffectiveMaxHealth())
            {
                int oldHealth = CurrentHealth;
                // Use negative damage to heal (TakeDamage with negative value heals)
                TakeDamage(-healthRegen);
                // Cap at max health
                if (CurrentHealth > GetEffectiveMaxHealth())
                {
                    TakeDamage(CurrentHealth - GetEffectiveMaxHealth());
                }
                int actualRegen = CurrentHealth - oldHealth;
                if (actualRegen > 0 && !CombatManager.DisableCombatUIOutput)
                {
                    UIManager.WriteLine($"[{character.Name}] regenerates {actualRegen} health ({CurrentHealth}/{GetEffectiveMaxHealth()})");
                }
            }
        }
    }
}
