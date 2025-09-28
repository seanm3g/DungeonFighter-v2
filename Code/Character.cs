using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace RPGGame
{
    public interface IComboMemory
    {
        int LastComboActionIdx { get; set; }
    }

    public class Character : Entity, IComboMemory
    {
        // Core components using composition
        public CharacterStats Stats { get; private set; }
        public CharacterEffects Effects { get; private set; }
        public CharacterEquipment Equipment { get; private set; }
        public CharacterProgression Progression { get; private set; }
        public CharacterActions Actions { get; private set; }

        // Health
        public int CurrentHealth { get; protected set; }
        public int MaxHealth { get; protected set; }

        // Turn system constants
        public const double DEFAULT_ACTION_LENGTH = 1.0; // Basic attack length defines one turn

        public Character(string? name = null, int level = 1)
            : base(name ?? FlavorText.GenerateCharacterName())
        {
            // Initialize components
            Stats = new CharacterStats(level);
            Effects = new CharacterEffects();
            Equipment = new CharacterEquipment();
            Progression = new CharacterProgression(level);
            Actions = new CharacterActions();

            // Initialize health based on tuning config
            var tuning = TuningConfig.Instance;
            MaxHealth = tuning.Character.PlayerBaseHealth + (level - 1) * tuning.Character.HealthPerLevel;
            CurrentHealth = MaxHealth;

            // Add default actions
            if (TuningConfig.IsDebugEnabled)
                Console.WriteLine($"DEBUG: Character created, adding default actions...");
            Actions.AddDefaultActions(this);
            if (TuningConfig.IsDebugEnabled)
                Console.WriteLine($"DEBUG: After AddDefaultActions, ActionPool has {ActionPool.Count} actions");
            
            Actions.AddClassActions(this, Progression, null);
            if (TuningConfig.IsDebugEnabled)
                Console.WriteLine($"DEBUG: After AddClassActions, ActionPool has {ActionPool.Count} actions");
        }

        // IComboMemory implementation
        public int LastComboActionIdx 
        { 
            get => Effects.LastComboActionIdx; 
            set => Effects.LastComboActionIdx = value; 
        }

        // Health management
        public void TakeDamage(int amount)
        {
            TakeDamageWithNotifications(amount);
        }
        
        public List<string> TakeDamageWithNotifications(int amount)
        {
            int originalAmount = amount;
            bool shieldUsed = false;
            
            // Apply shield damage reduction (50% reduction) if shield is active
            if (Effects.HasShield)
            {
                int reducedAmount = (int)(amount * 0.5); // Reduce damage by half
                int shieldReduction = amount - reducedAmount;
                amount = reducedAmount;
                Effects.HasShield = false; // Consume the shield
                shieldUsed = true;
                
                // Display shield message
                CombatLogger.Log($"[{Name}]'s Arcane Shield reduces damage by {shieldReduction}!");
            }
            
            // Apply damage reduction if active
            if (DamageReduction > 0)
            {
                amount = (int)(amount * (1.0 - DamageReduction));
            }
            
            CurrentHealth = Math.Max(0, CurrentHealth - amount);
            
            // Store shield usage for display purposes
            if (shieldUsed)
            {
                Effects.LastShieldReduction = originalAmount - amount;
            }
            
            // Check for health milestones and leadership changes
            // Note: Health milestone checking is now handled by CombatManager
            return new List<string>(); // Return empty list since milestone checking moved to CombatManager
        }
        
        public (int finalDamage, int shieldReduction, bool shieldUsed) CalculateDamageWithShield(int amount)
        {
            int originalAmount = amount;
            bool shieldUsed = false;
            int shieldReduction = 0;
            
            // Apply shield damage reduction (50% reduction) if shield is active
            if (Effects.HasShield)
            {
                int reducedAmount = (int)(amount * 0.5); // Reduce damage by half
                shieldReduction = amount - reducedAmount;
                amount = reducedAmount;
                shieldUsed = true;
            }
            
            // Apply damage reduction if active
            if (DamageReduction > 0)
            {
                amount = (int)(amount * (1.0 - DamageReduction));
            }
            
            return (amount, shieldReduction, shieldUsed);
        }

        public void Heal(int amount)
        {
            CurrentHealth = Math.Min(GetEffectiveMaxHealth(), CurrentHealth + amount);
        }

        public bool IsAlive => CurrentHealth > 0;

        public int GetEffectiveMaxHealth()
        {
            return MaxHealth + Equipment.GetEquipmentHealthBonus();
        }

        public double GetHealthPercentage()
        {
            return (double)CurrentHealth / GetEffectiveMaxHealth();
        }

        public bool MeetsHealthThreshold(double threshold)
        {
            return GetHealthPercentage() <= threshold;
        }

        public void ApplyHealthMultiplier(double multiplier)
        {
            MaxHealth = (int)(MaxHealth * multiplier);
            CurrentHealth = MaxHealth;
        }

        // Equipment management
        public Item? EquipItem(Item item, string slot)
        {
            // Store current health percentage before equipping
            double healthPercentage = GetHealthPercentage();
            int oldMaxHealth = GetEffectiveMaxHealth();
            
            Item? previousItem = Equipment.EquipItem(item, slot);
            
            // Check if max health increased and adjust current health accordingly
            int newMaxHealth = GetEffectiveMaxHealth();
            if (newMaxHealth > oldMaxHealth)
            {
                // If max health increased, heal to full health
                CurrentHealth = newMaxHealth;
            }
            else if (newMaxHealth < oldMaxHealth)
            {
                // If max health decreased, maintain health percentage but cap at new max
                int newCurrentHealth = (int)(newMaxHealth * healthPercentage);
                CurrentHealth = Math.Min(newCurrentHealth, newMaxHealth);
            }
            
            // Update actions after equipment change
            UpdateActionsAfterGearChange(previousItem, item, slot);
            
            // Apply roll bonuses from the new item
            Actions.ApplyRollBonusesFromGear(this, item);
            
            // Update reroll charges from Divine modifications
            Effects.RerollCharges = Equipment.GetTotalRerollCharges();
            
            return previousItem;
        }

        public Item? UnequipItem(string slot)
        {
            // Store current health percentage before unequipping
            double healthPercentage = GetHealthPercentage();
            int oldMaxHealth = GetEffectiveMaxHealth();
            
            Item? unequippedItem = Equipment.UnequipItem(slot);
            
            // Check if max health changed and adjust current health accordingly
            int newMaxHealth = GetEffectiveMaxHealth();
            if (newMaxHealth < oldMaxHealth)
            {
                // If max health decreased, maintain health percentage but cap at new max
                int newCurrentHealth = (int)(newMaxHealth * healthPercentage);
                CurrentHealth = Math.Min(newCurrentHealth, newMaxHealth);
            }
            else if (newMaxHealth > oldMaxHealth)
            {
                // If max health increased (unlikely when unequipping), maintain health percentage
                int newCurrentHealth = (int)(newMaxHealth * healthPercentage);
                CurrentHealth = Math.Min(newCurrentHealth, newMaxHealth);
            }
            
            // Remove roll bonuses from the unequipped item
            if (unequippedItem != null)
            {
                Actions.RemoveRollBonusesFromGear(this, unequippedItem);
            }
            
            // Update reroll charges from Divine modifications
            Effects.RerollCharges = Equipment.GetTotalRerollCharges();
            
            // Update actions after equipment change
            UpdateActionsAfterGearChange(unequippedItem, null, slot);
            
            return unequippedItem;
        }

        private void UpdateActionsAfterGearChange(Item? previousItem, Item? newItem, string slot)
        {
            // Remove actions from previous item
            if (previousItem != null)
            {
                if (previousItem is WeaponItem oldWeapon)
                {
                    Actions.RemoveWeaponActions(this, oldWeapon);
                }
                else
                {
                    Actions.RemoveArmorActions(this, previousItem);
                }
            }

            // Add actions from new item
            if (newItem != null)
            {
                if (newItem is WeaponItem weapon)
                {
                    Actions.AddWeaponActions(this, weapon);
                }
                else
                {
                    Actions.AddArmorActions(this, newItem);
                }
            }

            // Update combo sequence after equipment change
            Actions.UpdateComboSequenceAfterGearChange(this);
            
            // If weapon was changed, handle combo sequence intelligently
            if (slot.ToLower() == "weapon")
            {
                // If combo is now empty, initialize default combo
                if (Actions.ComboSequence.Count == 0)
                {
                    Actions.InitializeDefaultCombo(this, Equipment.Weapon as WeaponItem);
                }
            }
        }

        // XP and leveling
        public void AddXP(int amount)
        {
            Progression.AddXP(amount);
            while (Progression.XP >= GetXPToNextLevel())
            {
                Progression.AddXP(-GetXPToNextLevel());
                LevelUp();
            }
        }

        private int GetXPToNextLevel()
        {
            var tuning = TuningConfig.Instance;
            return (int)(tuning.Progression.BaseXPToLevel2 * Math.Pow(tuning.Progression.XPScalingFactor, Progression.Level - 1));
        }

        public void LevelUp()
        {
            Progression.LevelUp();
            Stats.LevelUp((Equipment.Weapon as WeaponItem)?.WeaponType ?? WeaponType.Mace);
            
            var tuning = TuningConfig.Instance;
            MaxHealth += tuning.Character.HealthPerLevel;
            CurrentHealth = MaxHealth;
            
            // Award class point and stat increases based on equipped weapon
            if (Equipment.Weapon is WeaponItem weapon)
            {
                string className = weapon.WeaponType switch
                {
                    WeaponType.Mace => "Barbarian",
                    WeaponType.Sword => "Warrior",
                    WeaponType.Dagger => "Rogue",
                    WeaponType.Wand => "Wizard",
                    _ => "Unknown"
                };
                
                Progression.AwardClassPoint(weapon.WeaponType);
                Console.WriteLine($"\n*** LEVEL UP! ***");
                Console.WriteLine($"You reached level {Progression.Level}!");
                Console.WriteLine($"Gained +1 {className} class point!");
                Console.WriteLine($"Stats increased: {Stats.GetStatIncreaseMessage(weapon.WeaponType)}");
                Console.WriteLine($"Current class: {Progression.GetCurrentClass()}");
                Console.WriteLine($"You are now known as: {Progression.GetFullNameWithQualifier(Name)}");
                
                // Show only classes with points > 0
                var classPointsInfo = new List<string>();
                if (Progression.BarbarianPoints > 0) classPointsInfo.Add($"Barbarian({Progression.BarbarianPoints})");
                if (Progression.WarriorPoints > 0) classPointsInfo.Add($"Warrior({Progression.WarriorPoints})");
                if (Progression.RoguePoints > 0) classPointsInfo.Add($"Rogue({Progression.RoguePoints})");
                if (Progression.WizardPoints > 0) classPointsInfo.Add($"Wizard({Progression.WizardPoints})");
                
                if (classPointsInfo.Count > 0)
                {
                    Console.WriteLine($"Class Points: {string.Join(" ", classPointsInfo)}");
                    Console.WriteLine($"Next Upgrades: {Progression.GetClassUpgradeInfo()}");
                }
                Console.WriteLine();
            }
            else
            {
                Stats.LevelUpNoWeapon();
                
                Console.WriteLine($"\n*** LEVEL UP! ***");
                Console.WriteLine($"You reached level {Progression.Level}!");
                Console.WriteLine("No weapon equipped - equal stat increases (+2 all stats)");
                Console.WriteLine();
            }

            // Re-add class actions when points change
            Actions.AddClassActions(this, Progression, (Equipment.Weapon as WeaponItem)?.WeaponType);
        }

        // Stat accessors
        public int Strength 
        { 
            get => Stats.GetEffectiveStrength(Equipment.GetEquipmentStatBonus("STR"), Equipment.GetModificationGodlikeBonus());
            set => Stats.Strength = value;
        }
        public int Agility 
        { 
            get => Stats.GetEffectiveAgility(Equipment.GetEquipmentStatBonus("AGI"));
            set => Stats.Agility = value;
        }
        public int Technique 
        { 
            get => Stats.GetEffectiveTechnique(Equipment.GetEquipmentStatBonus("TEC"));
            set => Stats.Technique = value;
        }
        public int Intelligence 
        { 
            get => Stats.GetEffectiveIntelligence(Equipment.GetEquipmentStatBonus("INT"));
            set => Stats.Intelligence = value;
        }

        public int Level 
        { 
            get => Progression.Level; 
            set => Progression.Level = value; 
        }
        public int XP 
        { 
            get => Progression.XP; 
            set => Progression.XP = value; 
        }

        // Equipment accessors
        public List<Item> Inventory 
        { 
            get => Equipment.Inventory; 
            set => Equipment.Inventory = value; 
        }
        public Item? Head 
        { 
            get => Equipment.Head; 
            set => Equipment.Head = value; 
        }
        public Item? Body 
        { 
            get => Equipment.Body; 
            set => Equipment.Body = value; 
        }
        public Item? Weapon 
        { 
            get => Equipment.Weapon; 
            set => Equipment.Weapon = value; 
        }
        public Item? Feet 
        { 
            get => Equipment.Feet; 
            set => Equipment.Feet = value; 
        }

        public void AddToInventory(Item item) => Equipment.AddToInventory(item);
        public bool RemoveFromInventory(Item item) => Equipment.RemoveFromInventory(item);

        // Effects accessors
        public int ComboStep 
        { 
            get => Effects.ComboStep; 
            set => Effects.ComboStep = value; 
        }
        public double ComboAmplifier => Effects.ComboAmplifier;
        public int ComboBonus => Effects.ComboBonus;
        public int TempComboBonus => Effects.TempComboBonus;
        public int TempComboBonusTurns => Effects.TempComboBonusTurns;
        public int EnemyRollPenalty 
        { 
            get => Effects.EnemyRollPenalty; 
            set => Effects.EnemyRollPenalty = value; 
        }
        public int EnemyRollPenaltyTurns 
        { 
            get => Effects.EnemyRollPenaltyTurns; 
            set => Effects.EnemyRollPenaltyTurns = value; 
        }
        public double SlowMultiplier => Effects.SlowMultiplier;
        public int SlowTurns => Effects.SlowTurns;
        // Poison and burn properties are now accessed directly from Entity base class
        public bool HasShield => Effects.HasShield;
        public int LastShieldReduction => Effects.LastShieldReduction;
        public bool ComboModeActive => Effects.ComboModeActive;
        public int TempStrengthBonus => Stats.TempStrengthBonus;
        public int TempAgilityBonus => Stats.TempAgilityBonus;
        public int TempTechniqueBonus => Stats.TempTechniqueBonus;
        public int TempIntelligenceBonus => Stats.TempIntelligenceBonus;
        public int TempStatBonusTurns => Stats.TempStatBonusTurns;
        public Action? LastAction => Effects.LastAction;
        public bool SkipNextTurn => Effects.SkipNextTurn;
        public bool GuaranteeNextSuccess => Effects.GuaranteeNextSuccess;
        public int ExtraAttacks => Effects.ExtraAttacks;
        public int ExtraDamage 
        { 
            get => Effects.ExtraDamage; 
            set => Effects.ExtraDamage = value; 
        }
        // DamageReduction is now accessed directly from Entity base class
        public double LengthReduction 
        { 
            get => Effects.LengthReduction; 
            set => Effects.LengthReduction = value; 
        }
        public int LengthReductionTurns 
        { 
            get => Effects.LengthReductionTurns; 
            set => Effects.LengthReductionTurns = value; 
        }
        public double ComboAmplifierMultiplier => Effects.ComboAmplifierMultiplier;
        public int ComboAmplifierTurns => Effects.ComboAmplifierTurns;
        public int RerollCharges => Effects.RerollCharges;
        public bool UsedRerollThisTurn => Effects.UsedRerollThisTurn;

        // Class points accessors
        public int BarbarianPoints => Progression.BarbarianPoints;
        public int WarriorPoints => Progression.WarriorPoints;
        public int RoguePoints => Progression.RoguePoints;
        public int WizardPoints => Progression.WizardPoints;

        // Combo sequence accessor
        public List<Action> ComboSequence => Actions.ComboSequence;

        // Action management
        public List<Action> GetComboActions() => Actions.GetComboActions();
        public List<Action> GetActionPool() => Actions.GetActionPool(this);
        public void AddToCombo(Action action) => Actions.AddToCombo(action);
        public void RemoveFromCombo(Action action) => Actions.RemoveFromCombo(action);
        public void InitializeDefaultCombo() => Actions.InitializeDefaultCombo(this, Equipment.Weapon as WeaponItem);

        // Effective stat methods
        public int GetEffectiveStrength() => Stats.GetEffectiveStrength(Equipment.GetEquipmentStatBonus("STR"), Equipment.GetModificationGodlikeBonus());
        public int GetEffectiveAgility() => Stats.GetEffectiveAgility(Equipment.GetEquipmentStatBonus("AGI"));
        public int GetEffectiveTechnique() => Stats.GetEffectiveTechnique(Equipment.GetEquipmentStatBonus("TEC"));
        public int GetEffectiveIntelligence() => Stats.GetEffectiveIntelligence(Equipment.GetEquipmentStatBonus("INT"));

        // Action length calculation
        public double CalculateTurnsFromActionLength(double actionLength) => Actions.CalculateTurnsFromActionLength(actionLength);
        public void RemoveItemActions() => Actions.RemoveItemActions(this);

        // Progression methods

        // Action methods
        public void ApplyRollBonusesFromGear(Item gear) => Actions.ApplyRollBonusesFromGear(this, gear);

        // Effect management
        public void SetTempComboBonus(int bonus, int turns) => Effects.SetTempComboBonus(bonus, turns);
        public int ConsumeTempComboBonus() => Effects.ConsumeTempComboBonus();
        public void ActivateComboMode() => Effects.ActivateComboMode();
        public void DeactivateComboMode() => Effects.DeactivateComboMode();
        public void ResetCombo() => Effects.ResetCombo();
        public void ApplyStatBonus(int bonus, string statType, int duration) => Stats.ApplyStatBonus(bonus, statType, duration);
        public override void UpdateTempEffects(double actionLength = DEFAULT_ACTION_LENGTH)
        {
            // Update base class effects (stun, weaken, roll penalty, poison, burn, damage reduction)
            base.UpdateTempEffects(actionLength);
            
            // Update character-specific effects
            Stats.UpdateTempEffects(actionLength);
            Effects.UpdateTempEffects(actionLength);
        }
        public void ApplySlow(double slowMultiplier, int duration) => Effects.ApplySlow(slowMultiplier, duration);
        public override void ApplyPoison(int damage, int stacks = 1, bool isBleeding = false) => base.ApplyPoison(damage, stacks, isBleeding);
        public override void ApplyBurn(int damage, int stacks = 1) => base.ApplyBurn(damage, stacks);
        public void ApplyShield() => Effects.ApplyShield();
        public bool ConsumeShield() => Effects.ConsumeShield();
        public override void ApplyWeaken(int turns) => base.ApplyWeaken(turns);
        public override int ProcessPoison(double currentTime) => base.ProcessPoison(currentTime);
        public override int ProcessBurn(double currentTime) => base.ProcessBurn(currentTime);
        public override string GetDamageTypeText() => base.GetDamageTypeText();
        public override void ClearAllTempEffects() 
        {
            // Clear base class effects
            base.ClearAllTempEffects();
            // Clear character-specific effects
            Effects.ClearAllTempEffects();
        }
        public bool UseReroll() => Effects.UseReroll();
        public void ResetRerollUsage() => Effects.ResetRerollUsage();
        public void ResetRerollCharges() => Effects.ResetRerollCharges();
        public int GetRemainingRerollCharges() => Effects.GetRemainingRerollCharges(Equipment.GetTotalRerollCharges());
        public bool UseRerollCharge() => Effects.UseRerollCharge();

        // Equipment bonuses
        public int GetEquipmentDamageBonus() => Equipment.GetEquipmentDamageBonus();
        public int GetEquipmentHealthBonus() => Equipment.GetEquipmentHealthBonus();
        public int GetEquipmentRollBonus() => Equipment.GetEquipmentRollBonus();
        public int GetMagicFind() => Equipment.GetMagicFind();
        public double GetEquipmentAttackSpeedBonus() => Equipment.GetEquipmentAttackSpeedBonus();
        public int GetEquipmentHealthRegenBonus() => Equipment.GetEquipmentHealthRegenBonus();
        public int GetTotalArmor() => Equipment.GetTotalArmor();
        public int GetTotalRerollCharges() => Equipment.GetTotalRerollCharges();
        public int GetModificationMagicFind() => Equipment.GetModificationMagicFind();
        public int GetModificationRollBonus() => Equipment.GetModificationRollBonus();
        public int GetModificationDamageBonus() => Equipment.GetModificationDamageBonus();
        public double GetModificationSpeedMultiplier() => Equipment.GetModificationSpeedMultiplier();
        public double GetModificationDamageMultiplier() => Equipment.GetModificationDamageMultiplier();
        public double GetModificationLifesteal() => Equipment.GetModificationLifesteal();
        public int GetModificationGodlikeBonus() => Equipment.GetModificationGodlikeBonus();
        public double GetModificationBleedChance() => Equipment.GetModificationBleedChance();
        public double GetModificationUniqueActionChance() => Equipment.GetModificationUniqueActionChance();
        public double GetArmorSpikeDamage() => Equipment.GetArmorSpikeDamage();
        public List<ArmorStatus> GetEquippedArmorStatuses() => Equipment.GetEquippedArmorStatuses();
        public bool HasAutoSuccess() => Equipment.HasAutoSuccess();

        // Progression
        public string GetCurrentClass() => Progression.GetCurrentClass();
        public string GetFullNameWithQualifier() => Progression.GetFullNameWithQualifier(Name);
        public int GetNextClassThreshold(string className) => Progression.GetNextClassThreshold(className);
        public string GetClassUpgradeInfo() => Progression.GetClassUpgradeInfo();
        public void AwardClassPoint(WeaponType weaponType) => Progression.AwardClassPoint(weaponType);

        // Combat calculations
        public double GetComboAmplifier()
        {
            var tuning = TuningConfig.Instance;
            
            // Clamp Technique to valid range
            int clampedTech = Math.Max(1, Math.Min(tuning.ComboSystem.ComboAmplifierMaxTech, Technique));
            
            // Linear interpolation between ComboAmplifierAtTech5 and ComboAmplifierMax
            double techRange = tuning.ComboSystem.ComboAmplifierMaxTech - 5;
            double ampRange = tuning.ComboSystem.ComboAmplifierMax - tuning.ComboSystem.ComboAmplifierAtTech5;
            
            if (clampedTech <= 5)
            {
                return tuning.ComboSystem.ComboAmplifierAtTech5;
            }
            
            double techProgress = (clampedTech - 5) / techRange;
            return tuning.ComboSystem.ComboAmplifierAtTech5 + (ampRange * techProgress);
        }

        public double GetCurrentComboAmplification()
        {
            var comboActions = GetComboActions();
            if (comboActions.Count == 0) return 1.0;
            
            int currentStep = ComboStep % comboActions.Count;
            double baseAmp = GetComboAmplifier();
            return Math.Pow(baseAmp, currentStep);
        }

        public string GetComboInfo()
        {
            var comboActions = GetComboActions();
            if (comboActions.Count == 0) return "No combo actions available";
            
            int currentStep = ComboStep % comboActions.Count;
            double baseAmp = GetComboAmplifier();
            double currentAmp = Math.Pow(baseAmp, currentStep + 1);
            
            return $"Amplification: {currentAmp:F2}x";
        }

        public int GetAttacksPerTurn()
        {
            double attackSpeed = GetTotalAttackSpeed();
            int attacks = (int)Math.Floor(attackSpeed);
            return Math.Max(1, attacks); // Always at least 1 attack
        }

        public double GetTotalAttackSpeed()
        {
            var tuning = TuningConfig.Instance;
            
            // Base attack time: 10 seconds
            double baseAttackTime = tuning.Combat.BaseAttackTime;
            
            // Agility reduces attack time (makes you faster)
            double agilityReduction = Agility * tuning.Combat.AgilitySpeedReduction;
            double agilityAdjustedTime = baseAttackTime - agilityReduction;
            
            // Calculate weapon speed using the speed formula
            double weaponSpeed = 1.0;
            if (Weapon is WeaponItem w)
            {
                // Use the scaling manager to calculate the proper weapon speed
                weaponSpeed = ScalingManager.CalculateWeaponSpeed(w.BaseAttackSpeed, w.Tier, Level, w.Type.ToString());
            }
            double weaponAdjustedTime = agilityAdjustedTime * weaponSpeed;
            
            // Equipment speed bonus reduces time further
            double equipmentSpeedBonus = GetEquipmentAttackSpeedBonus();
            double finalAttackTime = weaponAdjustedTime - equipmentSpeedBonus;
            
            // Apply slow debuff if active
            if (SlowTurns > 0)
            {
                finalAttackTime *= SlowMultiplier;
            }
            
            // Apply speed multiplier modifications (like Ethereal)
            double speedMultiplier = GetModificationSpeedMultiplier();
            finalAttackTime /= speedMultiplier; // Divide by multiplier to make attacks faster
            
            // Apply minimum cap
            return Math.Max(tuning.Combat.MinimumAttackTime, finalAttackTime);
        }

        public int GetIntelligenceRollBonus()
        {
            var tuning = TuningConfig.Instance;
            return Intelligence / tuning.Attributes.IntelligenceRollBonusPer; // Every X points of INT gives +1 to rolls
        }

        public void SetTechniqueForTesting(int value) => Stats.SetTechniqueForTesting(value);

        public bool MeetsStatThreshold(string statType, double threshold) => 
            Stats.MeetsStatThreshold(statType, threshold, Equipment.GetEquipmentStatBonus(statType), Equipment.GetModificationGodlikeBonus());

        // Environment actions
        public void AddEnvironmentActions(Environment environment) => Actions.AddEnvironmentActions(this, environment);
        public void ClearEnvironmentActions() => Actions.ClearEnvironmentActions(this);

        // Unique actions
        public List<Action> GetAvailableUniqueActions() => Actions.GetAvailableUniqueActions(Weapon as WeaponItem);

        // Display methods
        public override string GetDescription()
        {
            return $"Level {Level} (Health: {CurrentHealth}/{MaxHealth}) (STR: {Strength}, AGI: {Agility}, TEC: {Technique}, INT: {Intelligence})";
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public void DisplayCharacterInfo()
        {
            Console.WriteLine($"=== CHARACTER INFORMATION ===");
            Console.WriteLine($"Name: {Name}");
            Console.WriteLine($"Class: {GetCurrentClass()}");
            Console.WriteLine($"Level: {Level}");
            Console.WriteLine($"Health: {CurrentHealth}/{MaxHealth}");
            Console.WriteLine($"XP: {XP}");
            Console.WriteLine();
            Console.WriteLine("=== STATS ===");
            Console.WriteLine($"Strength: {Strength}");
            Console.WriteLine($"Agility: {Agility}");
            Console.WriteLine($"Technique: {Technique}");
            Console.WriteLine($"Intelligence: {Intelligence}");
            Console.WriteLine();
            Console.WriteLine("=== CLASS POINTS ===");
            Console.WriteLine($"Barbarian (Mace): {BarbarianPoints}");
            Console.WriteLine($"Warrior (Sword): {WarriorPoints}");
            Console.WriteLine($"Rogue (Dagger): {RoguePoints}");
            Console.WriteLine($"Wizard (Wand): {WizardPoints}");
            Console.WriteLine();
            Console.WriteLine("=== EQUIPMENT ===");
            Console.WriteLine($"Weapon: {(Weapon?.Name ?? "None")}");
            Console.WriteLine($"Head: {(Head?.Name ?? "None")}");
            Console.WriteLine($"Body: {(Body?.Name ?? "None")}");
            Console.WriteLine($"Feet: {(Feet?.Name ?? "None")}");
            Console.WriteLine();
        }

        // Save/Load methods
        public void SaveCharacter(string filename = "GameData/character_save.json")
        {
            try
            {
                var saveData = new CharacterSaveData
                {
                    Name = Name,
                    Level = Level,
                    XP = XP,
                    CurrentHealth = CurrentHealth,
                    MaxHealth = MaxHealth,
                    Strength = Stats.Strength,
                    Agility = Stats.Agility,
                    Technique = Stats.Technique,
                    Intelligence = Stats.Intelligence,
                    BarbarianPoints = BarbarianPoints,
                    WarriorPoints = WarriorPoints,
                    RoguePoints = RoguePoints,
                    WizardPoints = WizardPoints,
                    ComboStep = ComboStep,
                    ComboBonus = ComboBonus,
                    TempComboBonus = TempComboBonus,
                    TempComboBonusTurns = TempComboBonusTurns,
                    DamageReduction = DamageReduction,
                    Inventory = Inventory,
                    Head = Head,
                    Body = Body,
                    Weapon = Weapon,
                    Feet = Feet
                };

                string json = System.Text.Json.JsonSerializer.Serialize(saveData, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filename, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving character: {ex.Message}");
            }
        }

        public static Character? LoadCharacter(string filename = "GameData/character_save.json")
        {
            try
            {
                if (!File.Exists(filename))
                {
                    Console.WriteLine($"Save file {filename} not found.");
                    return null;
                }

                string json = File.ReadAllText(filename);
                var saveData = System.Text.Json.JsonSerializer.Deserialize<CharacterSaveData>(json);
                
                if (saveData == null)
                {
                    Console.WriteLine("Error: Could not deserialize save data.");
                    return null;
                }

                // Create character with saved data
                var character = new Character(saveData.Name, saveData.Level);
                
                // Use the saved name, but fix "Unknown Unknown" names
                if (saveData.Name == "Unknown Unknown")
                {
                    character.Name = FlavorText.GenerateCharacterName();
                }
                else
                {
                    character.Name = saveData.Name; // Preserve the saved name
                }
                
                character.Progression.XP = saveData.XP;
                character.CurrentHealth = saveData.CurrentHealth;
                character.MaxHealth = saveData.MaxHealth;
                character.Stats.Strength = saveData.Strength;
                character.Stats.Agility = saveData.Agility;
                character.Stats.Technique = saveData.Technique;
                character.Stats.Intelligence = saveData.Intelligence;
                character.Progression.BarbarianPoints = saveData.BarbarianPoints;
                character.Progression.WarriorPoints = saveData.WarriorPoints;
                character.Progression.RoguePoints = saveData.RoguePoints;
                character.Progression.WizardPoints = saveData.WizardPoints;
                character.Effects.ComboStep = saveData.ComboStep;
                character.Effects.ComboBonus = saveData.ComboBonus;
                character.Effects.TempComboBonus = saveData.TempComboBonus;
                character.Effects.TempComboBonusTurns = saveData.TempComboBonusTurns;
                character.DamageReduction = saveData.DamageReduction;
                character.Equipment.Inventory = saveData.Inventory;
                character.Equipment.Head = saveData.Head;
                character.Equipment.Body = saveData.Body;
                character.Equipment.Weapon = saveData.Weapon;
                character.Equipment.Feet = saveData.Feet;

                // Rebuild action pool with proper structure
                character.ActionPool.Clear();
                character.Actions.AddDefaultActions(character); // Add basic attack
                character.Actions.AddClassActions(character, character.Progression, (character.Equipment.Weapon as WeaponItem)?.WeaponType); // Add class actions based on weapon
                
                // Re-add gear actions for equipped items (with probability for non-starter items)
                if (character.Head != null)
                    character.Actions.AddArmorActions(character, character.Head);
                if (character.Body != null)
                    character.Actions.AddArmorActions(character, character.Body);
                if (character.Weapon is WeaponItem weapon)
                    character.Actions.AddWeaponActions(character, weapon);
                if (character.Feet != null)
                    character.Actions.AddArmorActions(character, character.Feet);

                // Initialize combo sequence after all actions are loaded
                character.InitializeDefaultCombo();

                Console.WriteLine($"Character loaded from {filename}");
                return character;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading character: {ex.Message}");
                return null;
            }
        }

        public static void DeleteSaveFile(string filename = "GameData/character_save.json")
        {
            try
            {
                if (File.Exists(filename))
                {
                    File.Delete(filename);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting save file: {ex.Message}");
            }
        }
    }

    // Data class for character save/load
    public class CharacterSaveData
    {
        public string Name { get; set; } = "";
        public int Level { get; set; }
        public int XP { get; set; }
        public int CurrentHealth { get; set; }
        public int MaxHealth { get; set; }
        public int Strength { get; set; }
        public int Agility { get; set; }
        public int Technique { get; set; }
        public int Intelligence { get; set; }
        public int BarbarianPoints { get; set; }
        public int WarriorPoints { get; set; }
        public int RoguePoints { get; set; }
        public int WizardPoints { get; set; }
        public int ComboStep { get; set; }
        public int ComboBonus { get; set; }
        public int TempComboBonus { get; set; }
        public int TempComboBonusTurns { get; set; }
        public double DamageReduction { get; set; }
        public List<Item> Inventory { get; set; } = new List<Item>();
        public Item? Head { get; set; }
        public Item? Body { get; set; }
        public Item? Weapon { get; set; }
        public Item? Feet { get; set; }
    }
}
