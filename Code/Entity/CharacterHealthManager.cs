using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

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
        private int _currentHealth;
        public int CurrentHealth 
        { 
            get => _currentHealth;
            set 
            {
                int maxHealth = GetEffectiveMaxHealth();
                if (RPGGame.Tuning.DeveloperSimMode.ContinuePastZeroHp)
                    _currentHealth = Math.Min(value, maxHealth);
                else
                    _currentHealth = Math.Max(0, Math.Min(value, maxHealth));
            }
        }
        public int MaxHealth { get; set; }

        /// <summary>
        /// Effective armor for display/compat. Armor is flat damage reduction from gear and combat
        /// modifiers; it is not a depleting pool. Prefer <see cref="GetMaxArmor"/> /
        /// <see cref="GetEffectiveArmor"/> for new code.
        /// </summary>
        public int CurrentArmor
        {
            get => GetEffectiveArmor();
            set { /* Armor is derived from gear/effects; kept for save/API compatibility. */ }
        }

        /// <summary>Effective flat armor reduction (equipment + fortify − armor break / expose).</summary>
        public int GetMaxArmor() => GetEffectiveArmor();

        /// <summary>Effective flat armor reduction (equipment + fortify − armor break / expose).</summary>
        public int GetEffectiveArmor()
        {
            int max = character.GetTotalArmor();
            if (character.FortifyArmorBonus is int fortifyBonus && fortifyBonus > 0)
                max += fortifyBonus;
            if (character.ArmorBreakReduction is int armorBreak && armorBreak > 0)
                max = Math.Max(0, max - armorBreak);
            if (character.ExposeArmorReduction is int expose && expose > 0)
                max = Math.Max(0, max - expose);
            return max;
        }

        /// <summary>
        /// No-op retained for call-site compatibility. Armor is flat DR and does not refresh per room.
        /// </summary>
        public void RefreshRoomArmor() { }

        /// <summary>No-op retained for call-site compatibility (armor is not a stored pool).</summary>
        public void EnsureCurrentArmorWithinMax() { }

        /// <summary>
        /// Makes the character take damage with shield and damage reduction calculations
        /// </summary>
        /// <param name="amount">Base damage amount</param>
        public void TakeDamage(int amount)
        {
            TakeDamageWithNotifications(amount);
        }

        /// <summary>
        /// Makes the character take damage and returns notification messages.
        /// Attack armor mitigation is applied in <see cref="Combat.Calculators.DamageCalculator"/>;
        /// this path still applies shield and percent damage reduction for non-attack damage sources.
        /// </summary>
        /// <param name="amount">Base damage amount</param>
        /// <returns>List of notification messages</returns>
        public List<string> TakeDamageWithNotifications(int amount)
        {
            int originalAmount = amount;
            bool shieldUsed = false;
            character.Effects.LastArmorAbsorbed = 0;

            // Apply shield damage reduction (50% reduction) if shield is active
            if (character.Effects.HasShield)
            {
                int reducedAmount = (int)(amount * 0.5); // Reduce damage by half
                int shieldReduction = amount - reducedAmount;
                amount = reducedAmount;
                character.Effects.HasShield = false; // Consume the shield
                shieldUsed = true;

                // Display shield message using ColoredText
                var shieldBuilder = new ColoredTextBuilder();
                shieldBuilder.Add("[", Colors.White);
                shieldBuilder.Add(character.Name, ColorPalette.Player);
                shieldBuilder.Add("]'s Arcane Shield reduces damage by ", Colors.White);
                shieldBuilder.Add(shieldReduction.ToString(), ColorPalette.Success);
                shieldBuilder.Add("!", Colors.White);
                // Pass character for multi-character support
                TextDisplayIntegration.DisplayCombatAction(shieldBuilder.Build(), new List<ColoredText>(), null, null, character);
            }

            // Apply damage reduction if active
            if (character.DamageReduction > 0)
            {
                amount = (int)(amount * (1.0 - character.DamageReduction));
            }

            if (amount < 0)
            {
                CurrentHealth = Math.Min(GetEffectiveMaxHealth(), CurrentHealth - amount);
                return new List<string>();
            }

            if (RPGGame.Tuning.DeveloperSimMode.ContinuePastZeroHp)
                CurrentHealth -= amount;
            else
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

            int armor = GetEffectiveArmor();
            if (amount > 0 && armor > 0)
                amount = Math.Max(0, amount - armor);

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
            return MaxHealth + character.Equipment.GetEquipmentHealthBonus(character);
        }

        /// <summary>
        /// Gets the current health percentage
        /// </summary>
        /// <returns>Health percentage as a decimal (0.0 to 1.0)</returns>
        public double GetHealthPercentage()
        {
            int effectiveMaxHealth = GetEffectiveMaxHealth();
            // Handle division by zero when max health is 0
            if (effectiveMaxHealth <= 0)
            {
                return 0.0;
            }
            return (double)CurrentHealth / effectiveMaxHealth;
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
                // Set directly to avoid clamping issues during equipment change
                _currentHealth = newMaxHealth;
            }
            else if (newMaxHealth < oldMaxHealth)
            {
                // If max health decreased, maintain health percentage but cap at new max
                double healthPercentage = (double)_currentHealth / oldMaxHealth;
                int newCurrentHealth = (int)(newMaxHealth * healthPercentage);
                // Set directly to avoid clamping issues during equipment change
                _currentHealth = Math.Max(0, Math.Min(newCurrentHealth, newMaxHealth));
            }
            // If equal, no adjustment needed
        }

        /// <summary>
        /// Processes health regeneration for the character
        /// </summary>
        public void ProcessHealthRegeneration()
        {
            int healthRegen = character.Equipment.GetEquipmentHealthRegenBonus(character);
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
                    UIManager.WriteLine(""); // Add blank line after regeneration message
                }
            }
        }
    }
}
