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

    public class Character : Actor, IComboMemory
    {
        // Flag to disable debug output during balance analysis
        public static bool DisableCharacterCreationDebug = false;
        
        // Core components using composition
        public CharacterStats Stats { get; private set; }
        public CharacterEffects Effects { get; private set; }
        public CharacterEquipment Equipment { get; private set; }
        public CharacterProgression Progression { get; private set; }
        public CharacterActions Actions { get; private set; }

        // Extracted managers
        public CharacterHealthManager Health { get; private set; }
        public CharacterCombatCalculator Combat { get; private set; }
        public GameDisplayManager Display { get; set; }

        // NEW: Simplified facade and managers
        public CharacterFacade Facade { get; private set; }
        private readonly EquipmentManager _equipmentManager;
        private readonly LevelUpManager _levelUpManager;
        
        // Session statistics tracking
        public SessionStatistics SessionStats { get; private set; }

        // Health properties (delegated to Health manager)
        public int CurrentHealth 
        { 
            get => Health.CurrentHealth; 
            set => Health.CurrentHealth = value; 
        }
        public int MaxHealth 
        { 
            get => Health.MaxHealth; 
            set => Health.MaxHealth = value; 
        }

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

            // Initialize managers
            Health = new CharacterHealthManager(this);
            Combat = new CharacterCombatCalculator(this);
            Display = new GameDisplayManager(this, new List<Item>()); // Will be updated with actual inventory later

            // Initialize new managers
            _equipmentManager = new EquipmentManager(this);
            _levelUpManager = new LevelUpManager(this);
            Facade = new CharacterFacade(this);
            
            // Initialize session statistics
            SessionStats = new SessionStatistics();
            SessionStats.StartingLevel = level;

            // Initialize health based on tuning config
            var tuning = GameConfiguration.Instance;
            Health.MaxHealth = tuning.Character.PlayerBaseHealth + (level - 1) * tuning.Character.HealthPerLevel;
            Health.CurrentHealth = Health.MaxHealth;

            // Add default actions
            Actions.AddDefaultActions(this);
            Actions.AddClassActions(this, Progression, null);
            
            // Initialize default combo sequence
            Actions.InitializeDefaultCombo(this, Equipment.Weapon as WeaponItem);
        }

        // IComboMemory implementation
        public int LastComboActionIdx 
        { 
            get => Effects.LastComboActionIdx; 
            set => Effects.LastComboActionIdx = value; 
        }

        // Health management (delegated to Health manager)
        public void TakeDamage(int amount) => Health.TakeDamage(amount);
        public List<string> TakeDamageWithNotifications(int amount) => Health.TakeDamageWithNotifications(amount);
        public (int finalDamage, int shieldReduction, bool shieldUsed) CalculateDamageWithShield(int amount) => Health.CalculateDamageWithShield(amount);
        public void Heal(int amount) => Health.Heal(amount);
        public bool IsAlive => Health.IsAlive;
        public int GetEffectiveMaxHealth() => Health.GetEffectiveMaxHealth();
        public double GetHealthPercentage() => Health.GetHealthPercentage();
        public bool MeetsHealthThreshold(double threshold) => Health.MeetsHealthThreshold(threshold);
        public void ApplyHealthMultiplier(double multiplier) => Health.ApplyHealthMultiplier(multiplier);

        // Equipment management (delegated to EquipmentManager)
        public Item? EquipItem(Item item, string slot) => _equipmentManager.EquipItem(item, slot);
        public Item? UnequipItem(string slot) => _equipmentManager.UnequipItem(slot);

        // UpdateActionsAfterGearChange is now handled by EquipmentManager

        // XP and leveling (delegated to LevelUpManager)
        public void AddXP(int amount) => Facade.AddXP(amount);
        public List<LevelUpInfo> AddXPWithLevelUpInfo(int amount) => Facade.AddXPWithLevelUpInfo(amount);
        public void LevelUp() => _levelUpManager.LevelUp();

        // Stat accessors (delegated to Facade)
        public int Strength { get => Facade.Strength; set => Facade.Strength = value; }
        public int Agility { get => Facade.Agility; set => Facade.Agility = value; }
        public int Technique { get => Facade.Technique; set => Facade.Technique = value; }
        public int Intelligence { get => Facade.Intelligence; set => Facade.Intelligence = value; }

        public int Level { get => Facade.Properties.Level; set => Facade.Properties.Level = value; }
        public int XP { get => Facade.Properties.XP; set => Facade.Properties.XP = value; }

        // Equipment accessors (delegated to Facade)
        public List<Item> Inventory => Facade.Properties.Inventory;
        public Item? Head => Facade.Properties.Head;
        public Item? Body => Facade.Properties.Body;
        public Item? Weapon { get => Facade.Properties.Weapon; set => Facade.Properties.Weapon = value; }
        public Item? Feet => Facade.Properties.Feet;

        public void AddToInventory(Item item) => Facade.AddToInventory(item);
        public bool RemoveFromInventory(Item item) => Facade.RemoveFromInventory(item);

        // Effects accessors (delegated to Facade)
        public int ComboStep { get => Facade.Properties.ComboStep; set => Facade.Properties.ComboStep = value; }
        
        /// <summary>
        /// Increments ComboStep and wraps it within the combo sequence bounds
        /// Applies combo routing if the last action had routing properties
        /// </summary>
        /// <param name="lastAction">The action that was just executed (for routing)</param>
        public void IncrementComboStep(Action? lastAction = null)
        {
            var comboActions = GetComboActions();
            if (comboActions.Count == 0)
            {
                // If no combo actions, just increment (shouldn't happen in normal gameplay)
                ComboStep++;
                return;
            }
            
            // Apply combo routing if action has routing properties
            if (lastAction != null && this is Character character)
            {
                // Find the index of the lastAction in the combo sequence
                // This is more accurate than using ComboStep % comboActions.Count
                int currentSlotIndex = comboActions.FindIndex(a => a.Name == lastAction.Name);
                if (currentSlotIndex < 0)
                {
                    // If action not found in combo, use ComboStep as fallback
                    currentSlotIndex = ComboStep % comboActions.Count;
                }
                
                var routingResult = Entity.Actions.ComboRouting.ComboRouter.RouteCombo(character, lastAction, currentSlotIndex, comboActions);
                
                if (!routingResult.ContinueCombo)
                {
                    // Stop combo early - reset to 0
                    ComboStep = 0;
                    return;
                }
                
                // Apply routing to determine next slot
                int nextSlotIndex = routingResult.NextSlotIndex;
                if (nextSlotIndex < 0) nextSlotIndex = 0;
                if (nextSlotIndex >= comboActions.Count) nextSlotIndex = nextSlotIndex % comboActions.Count;
                
                // Update ComboStep to point to the next slot
                // ComboStep tracks absolute position, so we need to calculate the new absolute step
                // Find the action at nextSlotIndex and set ComboStep to match
                if (nextSlotIndex < comboActions.Count)
                {
                    // Calculate new ComboStep: find how many full cycles we've done, then add the slot index
                    int cycles = ComboStep / comboActions.Count;
                    int newComboStep = (cycles * comboActions.Count) + nextSlotIndex;
                    
                    // If the new step would be the same as current, increment to next cycle
                    // This ensures we always advance when IncrementComboStep is called
                    if (newComboStep == ComboStep)
                    {
                        // We're at the same absolute step, so advance to next cycle
                        ComboStep = ((cycles + 1) * comboActions.Count) + nextSlotIndex;
                    }
                    else
                    {
                        ComboStep = newComboStep;
                    }
                }
                else
                {
                    // Default: just increment and wrap
                    ComboStep = (ComboStep + 1) % comboActions.Count;
                }
            }
            else
            {
                // Default: just increment and wrap
                ComboStep = (ComboStep + 1) % comboActions.Count;
            }
        }
        public double ComboAmplifier => Facade.Properties.ComboAmplifier;
        public int ComboBonus => Facade.Properties.ComboBonus;
        public int TempComboBonus => Facade.Properties.TempComboBonus;
        public int TempComboBonusTurns => Facade.Properties.TempComboBonusTurns;
        public int EnemyRollPenalty => Facade.Properties.EnemyRollPenalty;
        public int EnemyRollPenaltyTurns => Facade.Properties.EnemyRollPenaltyTurns;
        public double SlowMultiplier => Facade.Properties.SlowMultiplier;
        public int SlowTurns => Facade.Properties.SlowTurns;
        public bool HasShield => Facade.Properties.HasShield;
        public int LastShieldReduction => Facade.Properties.LastShieldReduction;
        public bool ComboModeActive => Facade.Properties.ComboModeActive;
        public int TempStrengthBonus => Facade.Properties.TempStrengthBonus;
        public int TempAgilityBonus => Facade.Properties.TempAgilityBonus;
        public int TempTechniqueBonus => Facade.Properties.TempTechniqueBonus;
        public int TempIntelligenceBonus => Facade.Properties.TempIntelligenceBonus;
        public int TempStatBonusTurns => Facade.Properties.TempStatBonusTurns;
        public Action? LastAction => Facade.Properties.LastAction;
        public bool SkipNextTurn => Facade.Properties.SkipNextTurn;
        public bool GuaranteeNextSuccess => Facade.Properties.GuaranteeNextSuccess;
        public int ExtraAttacks => Facade.Properties.ExtraAttacks;
        public int ExtraDamage => Facade.Properties.ExtraDamage;
        public double LengthReduction => Facade.Properties.LengthReduction;
        public int LengthReductionTurns => Facade.Properties.LengthReductionTurns;
        public double ComboAmplifierMultiplier => Facade.Properties.ComboAmplifierMultiplier;
        public int ComboAmplifierTurns => Facade.Properties.ComboAmplifierTurns;
        public int RerollCharges => Facade.Properties.RerollCharges;
        public bool UsedRerollThisTurn => Facade.Properties.UsedRerollThisTurn;

        // Class points accessors (delegated to Facade)
        public int BarbarianPoints => Facade.Properties.BarbarianPoints;
        public int WarriorPoints => Facade.Properties.WarriorPoints;
        public int RoguePoints => Facade.Properties.RoguePoints;
        public int WizardPoints => Facade.Properties.WizardPoints;

        // Combo sequence accessor (delegated to Facade)
        public List<Action> ComboSequence => Facade.Properties.ComboSequence;

        // All other methods delegated to Facade for clean separation
        public List<Action> GetComboActions() => Facade.GetComboActions();
        public List<Action> GetActionPool() => Facade.GetActionPool();
        public void AddToCombo(Action action) => Facade.AddToCombo(action);
        public void RemoveFromCombo(Action action) => Facade.RemoveFromCombo(action);
        public void InitializeDefaultCombo() => Facade.InitializeDefaultCombo();
        public int GetEffectiveStrength() => Facade.GetEffectiveStrength();
        public int GetEffectiveAgility() => Facade.GetEffectiveAgility();
        public int GetEffectiveTechnique() => Facade.GetEffectiveTechnique();
        public int GetEffectiveIntelligence() => Facade.GetEffectiveIntelligence();
        public double CalculateTurnsFromActionLength(double actionLength) => Facade.CalculateTurnsFromActionLength(actionLength);
        public void RemoveItemActions() => Facade.RemoveItemActions();
        public void ApplyRollBonusesFromGear(Item gear) => Facade.ApplyRollBonusesFromGear(gear);
        public void SetTempComboBonus(int bonus, int turns) => Facade.SetTempComboBonus(bonus, turns);
        public int ConsumeTempComboBonus() => Facade.ConsumeTempComboBonus();
        public void ActivateComboMode() => Facade.ActivateComboMode();
        public void DeactivateComboMode() => Facade.DeactivateComboMode();
        public void ResetCombo() => Facade.ResetCombo();
        public void ApplyStatBonus(int bonus, string statType, int duration) => Facade.ApplyStatBonus(bonus, statType, duration);
        public override void UpdateTempEffects(double actionLength = DEFAULT_ACTION_LENGTH) 
        {
            // Update base class effects (stun, weaken, roll penalty, poison, burn, damage reduction)
            base.UpdateTempEffects(actionLength);
            
            // Update character-specific effects
            Stats.UpdateTempEffects(actionLength);
            Effects.UpdateTempEffects(actionLength);
        }
        public void ApplySlow(double slowMultiplier, int duration) => Facade.ApplySlow(slowMultiplier, duration);
        public override void ApplyPoison(int damage, int stacks = 1, bool isBleeding = false) => base.ApplyPoison(damage, stacks, isBleeding);
        public override void ApplyBurn(int damage, int stacks = 1) => base.ApplyBurn(damage, stacks);
        public void ApplyShield() => Facade.ApplyShield();
        public bool ConsumeShield() => Facade.ConsumeShield();
        public override void ApplyWeaken(int turns) => base.ApplyWeaken(turns);
        public override int ProcessPoison(double currentTime) => base.ProcessPoison(currentTime);
        public override int ProcessBurn(double currentTime) => base.ProcessBurn(currentTime);
        public override string GetDamageTypeText() => base.GetDamageTypeText();
        public override void ClearAllTempEffects() 
        {
            // Clear base class effects (poison, burn, stun, weaken, etc.)
            base.ClearAllTempEffects();
            // Clear character-specific effects
            Effects.ClearAllTempEffects();
            // Clear temporary stat bonuses
            Stats.TempStrengthBonus = 0;
            Stats.TempAgilityBonus = 0;
            Stats.TempTechniqueBonus = 0;
            Stats.TempIntelligenceBonus = 0;
            Stats.TempStatBonusTurns = 0;
        }
        public bool UseReroll() => Facade.UseReroll();
        public void ResetRerollUsage() => Facade.ResetRerollUsage();
        public void ResetRerollCharges() => Facade.ResetRerollCharges();
        public int GetRemainingRerollCharges() => Facade.GetRemainingRerollCharges();
        public bool UseRerollCharge() => Facade.UseRerollCharge();
        public int GetEquipmentDamageBonus() => Facade.GetEquipmentDamageBonus();
        public int GetEquipmentHealthBonus() => Facade.GetEquipmentHealthBonus();
        public int GetEquipmentRollBonus() => Facade.GetEquipmentRollBonus();
        public int GetMagicFind() => Facade.GetMagicFind();
        public double GetEquipmentAttackSpeedBonus() => Facade.GetEquipmentAttackSpeedBonus();
        public int GetEquipmentHealthRegenBonus() => Facade.GetEquipmentHealthRegenBonus();
        public int GetTotalArmor() => Facade.GetTotalArmor();
        public int GetTotalRerollCharges() => Facade.GetTotalRerollCharges();
        public int GetModificationMagicFind() => Facade.GetModificationMagicFind();
        public int GetModificationRollBonus() => Facade.GetModificationRollBonus();
        public int GetModificationDamageBonus() => Facade.GetModificationDamageBonus();
        public double GetModificationSpeedMultiplier() => Facade.GetModificationSpeedMultiplier();
        public double GetModificationDamageMultiplier() => Facade.GetModificationDamageMultiplier();
        public double GetModificationLifesteal() => Facade.GetModificationLifesteal();
        public int GetModificationGodlikeBonus() => Facade.GetModificationGodlikeBonus();
        public double GetModificationBleedChance() => Facade.GetModificationBleedChance();
        public double GetModificationUniqueActionChance() => Facade.GetModificationUniqueActionChance();
        public double GetArmorSpikeDamage() => Facade.GetArmorSpikeDamage();
        public List<ArmorStatus> GetEquippedArmorStatuses() => Facade.GetEquippedArmorStatuses();
        public bool HasAutoSuccess() => Facade.HasAutoSuccess();
        public string GetCurrentClass() => Facade.GetCurrentClass();
        public string GetFullNameWithQualifier() => Facade.GetFullNameWithQualifier();
        public int GetNextClassThreshold(string className) => Facade.GetNextClassThreshold(className);
        public string GetClassUpgradeInfo() => Facade.GetClassUpgradeInfo();
        public void AwardClassPoint(WeaponType weaponType) => Facade.AwardClassPoint(weaponType);
        public double GetComboAmplifier() => Facade.GetComboAmplifier();
        public double GetCurrentComboAmplification() => Facade.GetCurrentComboAmplification();
        public double GetNextComboAmplification() => Facade.GetNextComboAmplification();
        public string GetComboInfo() => Facade.GetComboInfo();
        public int GetAttacksPerTurn() => Facade.GetAttacksPerTurn();
        public double GetTotalAttackSpeed() => Facade.GetTotalAttackSpeed();
        public int GetIntelligenceRollBonus() => Facade.GetIntelligenceRollBonus();
        public void SetTechniqueForTesting(int value) => Facade.SetTechniqueForTesting(value);
        public bool MeetsStatThreshold(string statType, double threshold) => Facade.MeetsStatThreshold(statType, threshold);
        public void AddEnvironmentActions(Environment environment) => Facade.AddEnvironmentActions(environment);
        public void ClearEnvironmentActions() => Facade.ClearEnvironmentActions();
        public List<Action> GetAvailableUniqueActions() => Facade.GetAvailableUniqueActions();
        public override string GetDescription() => Facade.GetDescription();
        public override string ToString() => base.ToString();
        public void DisplayCharacterInfo() => Facade.DisplayCharacterInfo();
        public void SaveCharacter(string? filename = null) => Facade.SaveCharacter(filename);
        public static async Task<Character?> LoadCharacterAsync(string? filename = null) => await CharacterFacade.LoadCharacterAsync(filename).ConfigureAwait(false);
        public static Character? LoadCharacter(string? filename = null) => CharacterFacade.LoadCharacter(filename);
        public static void DeleteSaveFile(string? filename = null) => CharacterFacade.DeleteSaveFile(filename);
        
        // Session statistics methods
        public void RecordEnemyDefeat() => SessionStats.RecordEnemyDefeat();
        public void RecordDamageDealt(int damage, bool isCritical = false) => SessionStats.RecordDamageDealt(damage, isCritical);
        public void RecordDamageReceived(int damage) => SessionStats.RecordDamageReceived(damage);
        public void RecordHealingReceived(int healing) => SessionStats.RecordHealingReceived(healing);
        public void RecordAction(bool hit, bool isCritical = false, bool isCriticalMiss = false) => SessionStats.RecordAction(hit, isCritical, isCriticalMiss);
        public void RecordCombo(int comboStep, int comboDamage) => SessionStats.RecordCombo(comboStep, comboDamage);
        public void RecordItemCollected(Item item) => SessionStats.RecordItemCollected(item);
        public void RecordItemEquipped() => SessionStats.RecordItemEquipped();
        public void RecordDungeonCompleted() => SessionStats.RecordDungeonCompleted();
        public void RecordRoomExplored() => SessionStats.RecordRoomExplored();
        public void RecordEncounterSurvived() => SessionStats.RecordEncounterSurvived();
        public void RecordLevelUp(int newLevel) => SessionStats.RecordLevelUp(newLevel);
        public void RecordXPGain(int xp) => SessionStats.RecordXPGain(xp);
        public void RecordHealthStatus(double healthPercentage) => SessionStats.RecordHealthStatus(healthPercentage);
        public void RecordPerfectCombat() => SessionStats.RecordPerfectCombat();
        public void RecordOneShotKill() => SessionStats.RecordOneShotKill();
        public void EndTurn() => SessionStats.EndTurn();
        public string GetDefeatSummary() => SessionStats.GetDefeatSummary();
    }
}

