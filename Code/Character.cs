using System.Text.Json;
using System.Linq;

namespace RPGGame
{
    public interface IComboMemory
    {
        int LastComboActionIdx { get; set; }
    }

    public class Character : Entity, IComboMemory
    {
        // Attributes
        public int Strength { get; protected set; }
        public int Agility { get; protected set; }
        public int Technique { get; protected set; }
        public int Intelligence { get; protected set; }

        // Health
        public int CurrentHealth { get; protected set; }
        public int MaxHealth { get; protected set; }

        // Level and XP
        public int Level { get; protected set; }
        public int XP { get; protected set; }

        // Inventory
        public List<Item> Inventory { get; private set; }

        // Item slots
        public Item? Head { get; private set; }
        public Item? Body { get; private set; }
        public Item? Weapon { get; private set; }
        public Item? Feet { get; private set; }

        // Combo system state
        public int ComboStep { get; set; } = 0;
        public double ComboAmplifier { get; set; } = 1.0;
        public int ComboBonus { get; set; } = 0; // For loot integration
        // Temporary bonus for Taunt
        public int TempComboBonus { get; set; } = 0;
        public int TempComboBonusTurns { get; set; } = 0;
        
        // Enemy roll penalty from actions like Arcane Shield
        public int EnemyRollPenalty { get; set; } = 0;
        public int EnemyRollPenaltyTurns { get; set; } = 0;
        public int LastComboActionIdx { get; set; } = -1;
        // New combo mode tracking
        public bool ComboModeActive { get; set; } = false;
        
        // Combo sequence - the actions currently selected for the combo
        public List<Action> ComboSequence { get; private set; } = new List<Action>();

        // New mechanics for advanced actions
        public int TempStrengthBonus { get; set; } = 0;
        public int TempAgilityBonus { get; set; } = 0;
        public int TempTechniqueBonus { get; set; } = 0;
        public int TempIntelligenceBonus { get; set; } = 0;
        public int TempStatBonusTurns { get; set; } = 0;
        public Action? LastAction { get; set; } = null; // For Deja Vu
        public bool SkipNextTurn { get; set; } = false; // For True Strike
        public bool GuaranteeNextSuccess { get; set; } = false; // For True Strike
        public int ExtraAttacks { get; set; } = 0; // For Flurry/Precision Strike
        public int ExtraDamage { get; set; } = 0; // For Opening Volley
        public double DamageReduction { get; set; } = 0.0; // For Sharp Edge
        public bool IsStunned { get; set; } = false; // For Stun effects
        public int StunTurnsRemaining { get; set; } = 0; // Stun duration
        public double LengthReduction { get; set; } = 0.0; // For Taunt
        public int LengthReductionTurns { get; set; } = 0; // Length reduction duration
        public double ComboAmplifierMultiplier { get; set; } = 1.0; // For Pretty Boy Swag
        public int ComboAmplifierTurns { get; set; } = 0; // Amplifier duration

        // Class points system
        public int BarbarianPoints { get; set; } = 0; // Mace weapon
        public int WarriorPoints { get; set; } = 0;   // Sword weapon
        public int RoguePoints { get; set; } = 0;     // Dagger weapon
        public int WizardPoints { get; set; } = 0;    // Wand weapon

        public Character(string? name = null, int level = 1)
            : base(name ?? FlavorText.GenerateCharacterName())
        {
            Level = level;
            
            var tuning = TuningConfig.Instance;
            
            // Initialize attributes based on tuning config
            Strength = tuning.Attributes.PlayerBaseAttributes.Strength + (level - 1) * tuning.Attributes.PlayerAttributesPerLevel;
            Agility = tuning.Attributes.PlayerBaseAttributes.Agility + (level - 1) * tuning.Attributes.PlayerAttributesPerLevel;
            Technique = tuning.Attributes.PlayerBaseAttributes.Technique + (level - 1) * tuning.Attributes.PlayerAttributesPerLevel;
            Intelligence = tuning.Attributes.PlayerBaseAttributes.Intelligence + (level - 1) * tuning.Attributes.PlayerAttributesPerLevel;

            // Initialize health based on tuning config
            MaxHealth = tuning.Character.PlayerBaseHealth + (level - 1) * tuning.Character.HealthPerLevel;
            CurrentHealth = MaxHealth;

            // Initialize level and XP
            XP = 0;

            // Initialize inventory
            Inventory = new List<Item>();

            // Initialize item slots
            Head = null;
            Body = null;
            Weapon = null;
            Feet = null;

            // Add default actions
            AddDefaultActions();
            AddComboActions();
            
            // Initialize combo sequence with core class actions
            InitializeDefaultCombo();
        }

        private void AddDefaultActions()
        {
            // Add basic attack that's always available
            var basicAttack = new Action(
                name: "BASIC ATTACK",
                type: ActionType.Attack,
                targetType: TargetType.SingleTarget,
                baseValue: 0,
                range: 1,
                cooldown: 0,
                description: "A simple attack",
                comboOrder: -1, // Basic attack doesn't participate in combos
                damageMultiplier: 1.0,
                length: 1.0,
                causesBleed: false,
                causesWeaken: false,
                isComboAction: false
            );
            AddAction(basicAttack, 1.0); // High probability for basic attack
        }

        private void AddComboActions()
        {
            // Load class actions based on current weapon
            AddClassActions();
        }

        private void AddClassActions()
        {
            // Remove existing class actions first
            RemoveClassActions();
            
            // Add class-specific actions based on class points
            AddBarbarianActions();
            AddWarriorActions();
            AddRogueActions();
            AddWizardActions();
        }

        private void LoadClassActionsFromJson(WeaponType weaponType)
        {
            try
            {
                string[] possiblePaths = {
                    Path.Combine("GameData", "Actions.json"),
                    Path.Combine("..", "GameData", "Actions.json"),
                    Path.Combine("..", "..", "GameData", "Actions.json"),
                    Path.Combine("DF4 - CONSOLE", "GameData", "Actions.json"),
                    Path.Combine("..", "DF4 - CONSOLE", "GameData", "Actions.json")
                };

                string? foundPath = null;
                foreach (string path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        foundPath = path;
                        break;
                    }
                }

                if (foundPath != null)
                {
                    string jsonContent = File.ReadAllText(foundPath);
                    var allActions = System.Text.Json.JsonSerializer.Deserialize<List<ActionData>>(jsonContent);
                    
                    if (allActions != null)
                    {
                        // Define which actions belong to which class
                        var classActions = GetClassActions(weaponType);
                        
                        foreach (var actionData in allActions)
                        {
                            if (classActions.Contains(actionData.Name))
                            {
                                var action = CreateActionFromData(actionData);
                                if (action.IsComboAction)
                                {
                                    AddAction(action, 0.0);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading class actions from JSON: {ex.Message}");
            }
        }

        private List<string> GetClassActions(WeaponType weaponType)
        {
            // No core class actions - all actions come from equipped gear
            return new List<string>();
        }

        private void LoadActionsFromJson()
        {
            try
            {
                string[] possiblePaths = {
                    Path.Combine("GameData", "Actions.json"),
                    Path.Combine("..", "GameData", "Actions.json"),
                    Path.Combine("..", "..", "GameData", "Actions.json"),
                    Path.Combine("DF4 - CONSOLE", "GameData", "Actions.json"),
                    Path.Combine("..", "DF4 - CONSOLE", "GameData", "Actions.json")
                };

                string? foundPath = null;
                foreach (string path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        foundPath = path;
                        break;
                    }
                }

                if (foundPath != null)
                {
                    string jsonContent = File.ReadAllText(foundPath);
                    var actions = System.Text.Json.JsonSerializer.Deserialize<List<ActionData>>(jsonContent);
                    
                    if (actions != null)
                    {
                        // Clear existing actions first
                        ActionPool.Clear();
                        
                        foreach (var actionData in actions)
                        {
                            var action = CreateActionFromData(actionData);
                            if (action.IsComboAction)
                            {
                                AddAction(action, 0.0);
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Actions.json file not found. Tried paths: {string.Join(", ", possiblePaths)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading actions from JSON: {ex.Message}");
            }
        }


        private Action CreateActionFromData(ActionData data)
        {
            // Enhance description with modifiers
            string enhancedDescription = EnhanceActionDescription(data);
            
            var action = new Action(
                name: data.Name,
                type: ParseActionType(data.Type),
                targetType: TargetType.SingleTarget,
                baseValue: data.BaseValue,
                range: data.Range,
                cooldown: data.Cooldown,
                description: enhancedDescription,
                comboOrder: data.ComboOrder,
                damageMultiplier: data.DamageMultiplier,
                length: data.Length,
                causesBleed: data.CausesBleed,
                causesWeaken: data.CausesWeaken,
                isComboAction: data.IsComboAction,
                comboBonusAmount: data.ComboBonusAmount,
                comboBonusDuration: data.ComboBonusDuration
            );
            
            // Set additional properties
            action.RollBonus = data.RollBonus;
            action.StatBonus = data.StatBonus;
            action.StatBonusType = data.StatBonusType;
            action.StatBonusDuration = data.StatBonusDuration;
            action.MultiHitCount = data.MultiHitCount;
            action.MultiHitDamagePercent = data.MultiHitDamagePercent;
            action.SelfDamagePercent = data.SelfDamagePercent;
            action.SkipNextTurn = data.SkipNextTurn;
            action.RepeatLastAction = data.RepeatLastAction;
                
            return action;
        }

        private string EnhanceActionDescription(ActionData data)
        {
            var modifiers = new List<string>();
            
            // Add roll bonus information
            if (data.RollBonus != 0)
            {
                string rollText = data.RollBonus > 0 ? $"+{data.RollBonus}" : data.RollBonus.ToString();
                modifiers.Add($"Roll: {rollText}");
            }
            
            // Add damage multiplier information
            if (data.DamageMultiplier != 1.0)
            {
                modifiers.Add($"Damage: {data.DamageMultiplier:F1}x");
            }
            
            // Add combo bonus information
            if (data.ComboBonusAmount > 0 && data.ComboBonusDuration > 0)
            {
                modifiers.Add($"Combo: +{data.ComboBonusAmount} for {data.ComboBonusDuration} turns");
            }
            
            // Add status effect information
            if (data.CausesBleed)
            {
                modifiers.Add("Causes Bleed");
            }
            
            if (data.CausesWeaken)
            {
                modifiers.Add("Causes Weaken");
            }
            
            // Add multi-hit information
            if (data.MultiHitCount > 1)
            {
                modifiers.Add($"Multi-hit: {data.MultiHitCount} attacks");
            }
            
            // Add self-damage information
            if (data.SelfDamagePercent > 0)
            {
                modifiers.Add($"Self-damage: {data.SelfDamagePercent}%");
            }
            
            // Add special effects
            if (data.SkipNextTurn)
            {
                modifiers.Add("Skips next turn");
            }
            
            if (data.RepeatLastAction)
            {
                modifiers.Add("Repeats last action");
            }
            
            // Combine base description with modifiers
            string result = data.Description;
            if (modifiers.Count > 0)
            {
                result += $" | {string.Join(", ", modifiers)}";
            }
            
            return result;
        }

        private ActionType ParseActionType(string type)
        {
            return type.ToLower() switch
            {
                "attack" => ActionType.Attack,
                "heal" => ActionType.Heal,
                "buff" => ActionType.Buff,
                "debuff" => ActionType.Debuff,
                "spell" => ActionType.Spell,
                _ => ActionType.Attack
            };
        }

        // Methods to add/remove items from inventory
        public void AddToInventory(Item item) => Inventory.Add(item);
        public bool RemoveFromInventory(Item item) => Inventory.Remove(item);

        // Methods to equip/unequip items
        public Item? EquipItem(Item item, string slot)
        {
            Item? previousItem = null;
            switch (slot.ToLower())
            {
                case "head": 
                    previousItem = Head;
                    Head = item;
                    // Add armor actions if applicable
                    AddArmorActions(item);
                    break;
                case "body": 
                    previousItem = Body;
                    Body = item;
                    // Add armor actions if applicable
                    AddArmorActions(item);
                    break;
                case "weapon": 
                    previousItem = Weapon;
                    Weapon = item;
                    // Add weapon-specific actions
                    if (item is WeaponItem weapon)
                    {
                        AddWeaponActions(weapon);
                    }
                    break;
                case "feet": 
                    previousItem = Feet;
                    Feet = item;
                    // Add armor actions if applicable
                    AddArmorActions(item);
                    break;
            }
            
            // Reinitialize combo sequence after equipment change
            InitializeDefaultCombo();
            
            // Apply roll bonuses from the new item
            ApplyRollBonusesFromGear(item);
            
            return previousItem;
        }

        public Item? UnequipItem(string slot)
        {
            Item? unequippedItem = null;
            switch (slot.ToLower())
            {
                case "head": 
                    unequippedItem = Head;
                    Head = null;
                    // Remove armor actions if applicable
                    RemoveArmorActions(unequippedItem);
                    break;
                case "body": 
                    unequippedItem = Body;
                    Body = null;
                    // Remove armor actions if applicable
                    RemoveArmorActions(unequippedItem);
                    break;
                case "weapon": 
                    unequippedItem = Weapon;
                    Weapon = null;
                    // Remove weapon actions
                    RemoveWeaponActions();
                    break;
                case "feet": 
                    unequippedItem = Feet;
                    Feet = null;
                    // Remove armor actions if applicable
                    RemoveArmorActions(unequippedItem);
                    break;
            }
            
            // Remove roll bonuses from the unequipped item
            if (unequippedItem != null)
            {
                RemoveRollBonusesFromGear(unequippedItem);
            }
            
            return unequippedItem;
        }

        // Methods to add XP and handle leveling up
        public void AddXP(int amount)
        {
            XP += amount;
            while (XP >= XPToNextLevel())
            {
                XP -= XPToNextLevel();
                LevelUp();
            }
        }

        private void LevelUp()
        {
            Level++;
            var tuning = TuningConfig.Instance;
            MaxHealth += tuning.Character.HealthPerLevel;
            CurrentHealth = MaxHealth;
            
            // Award class point and stat increases based on equipped weapon
            if (Weapon is WeaponItem weapon)
            {
                string className = weapon.WeaponType switch
                {
                    WeaponType.Mace => "Barbarian",
                    WeaponType.Sword => "Warrior",
                    WeaponType.Dagger => "Rogue",
                    WeaponType.Wand => "Wizard",
                    _ => "Unknown"
                };
                
                // Increase stats based on weapon class: +3 for weapon class, +1 for others
                switch (weapon.WeaponType)
                {
                    case WeaponType.Mace: // Barbarian - Strength
                        Strength += 3;
                        Agility += 1;
                        Technique += 1;
                        Intelligence += 1;
                        break;
                    case WeaponType.Sword: // Warrior - Agility
                        Strength += 1;
                        Agility += 3;
                        Technique += 1;
                        Intelligence += 1;
                        break;
                    case WeaponType.Dagger: // Rogue - Technique
                        Strength += 1;
                        Agility += 1;
                        Technique += 3;
                        Intelligence += 1;
                        break;
                    case WeaponType.Wand: // Wizard - Intelligence
                        Strength += 1;
                        Agility += 1;
                        Technique += 1;
                        Intelligence += 3;
                        break;
                    default:
                        // Fallback: equal increases if unknown weapon type
                        Strength += 2;
                        Agility += 2;
                        Technique += 2;
                        Intelligence += 2;
                        break;
                }
                
                AwardClassPoint(weapon.WeaponType);
                Console.WriteLine($"\n*** LEVEL UP! ***");
                Console.WriteLine($"You reached level {Level}!");
                Console.WriteLine($"Gained +1 {className} class point!");
                Console.WriteLine($"Stats increased: {GetStatIncreaseMessage(weapon.WeaponType)}");
                Console.WriteLine($"Current class: {GetCurrentClass()}");
                // Show only classes with points > 0
                var classPointsInfo = new List<string>();
                if (BarbarianPoints > 0) classPointsInfo.Add($"Barbarian({BarbarianPoints})");
                if (WarriorPoints > 0) classPointsInfo.Add($"Warrior({WarriorPoints})");
                if (RoguePoints > 0) classPointsInfo.Add($"Rogue({RoguePoints})");
                if (WizardPoints > 0) classPointsInfo.Add($"Wizard({WizardPoints})");
                
                if (classPointsInfo.Count > 0)
                {
                    Console.WriteLine($"Class Points: {string.Join(" ", classPointsInfo)}");
                    Console.WriteLine($"Next Upgrades: {GetClassUpgradeInfo()}");
                }
                Console.WriteLine();
            }
            else
            {
                // No weapon equipped - equal stat increases
                Strength += 2;
                Agility += 2;
                Technique += 2;
                Intelligence += 2;
                
                Console.WriteLine($"\n*** LEVEL UP! ***");
                Console.WriteLine($"You reached level {Level}!");
                Console.WriteLine("No weapon equipped - equal stat increases (+2 all stats)");
                Console.WriteLine();
            }
        }

        private int XPToNextLevel()
        {
            var tuning = TuningConfig.Instance;
            return (int)(tuning.Progression.BaseXPToLevel2 * Math.Pow(tuning.Progression.XPScalingFactor, Level - 1));
        }

        private string GetStatIncreaseMessage(WeaponType weaponType)
        {
            return weaponType switch
            {
                WeaponType.Mace => "+3 STR, +1 AGI, +1 TEC, +1 INT",    // Barbarian - Strength
                WeaponType.Sword => "+1 STR, +3 AGI, +1 TEC, +1 INT",   // Warrior - Agility
                WeaponType.Dagger => "+1 STR, +1 AGI, +3 TEC, +1 INT",  // Rogue - Technique
                WeaponType.Wand => "+1 STR, +1 AGI, +1 TEC, +3 INT",    // Wizard - Intelligence
                _ => "+2 all stats"
            };
        }

        public void TakeDamage(int amount)
        {
            // Apply damage reduction if active
            if (DamageReduction > 0)
            {
                amount = (int)(amount * (1.0 - DamageReduction));
            }
            CurrentHealth = Math.Max(0, CurrentHealth - amount);
        }

        public void Heal(int amount)
        {
            CurrentHealth = Math.Min(MaxHealth, CurrentHealth + amount);
        }

        public bool IsAlive => CurrentHealth > 0;

        public override string GetDescription()
        {
            return $"Level {Level} (Health: {CurrentHealth}/{MaxHealth}) (STR: {Strength}, AGI: {Agility}, TEC: {Technique}, INT: {Intelligence})";
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public void UpdateComboBonus()
        {
            int bonus = 0;
            // Equipped items
            if (Head != null) bonus += Head.ComboBonus;
            if (Body != null) bonus += Body.ComboBonus;
            if (Weapon != null) bonus += Weapon.ComboBonus;
            if (Feet != null) bonus += Feet.ComboBonus;
            // Inventory items (optional: only if you want inventory to count)
            // foreach (var item in Inventory) bonus += item.ComboBonus;
            ComboBonus = bonus;
        }

        public List<Action> GetComboActions()
        {
            // Return the current combo sequence, sorted by combo order
            var sortedCombo = ComboSequence.ToList();
            sortedCombo.Sort((a, b) => a.ComboOrder.CompareTo(b.ComboOrder));
            return sortedCombo;
        }
        
        public List<Action> GetActionPool()
        {
            // Return all available actions from the action pool (no ordering)
            var allActions = new List<Action>();
            foreach (var entry in ActionPool)
            {
                if (entry.action.IsComboAction)
                    allActions.Add(entry.action);
            }
            // Don't sort by ComboOrder - Action Pool is just a list of available actions
            return allActions;
        }
        
        public void AddToCombo(Action action)
        {
            if (!ComboSequence.Contains(action) && action.IsComboAction)
            {
                ComboSequence.Add(action);
                // Reassign combo orders to be sequential starting from 1
                ReorderComboSequence();
            }
        }
        
        public void RemoveFromCombo(Action action)
        {
            if (ComboSequence.Contains(action))
            {
                ComboSequence.Remove(action);
                // Reset the action's combo order since it's no longer in the combo
                action.ComboOrder = 0;
                // Reassign combo orders to be sequential starting from 1
                ReorderComboSequence();
            }
        }

        public override void RemoveAction(Action action)
        {
            // Remove from action pool (base implementation)
            base.RemoveAction(action);
            
            // Also remove from combo sequence if it's there
            RemoveFromCombo(action);
        }
        
        private void ReorderComboSequence()
        {
            // Sort by current combo order, then reassign sequential orders starting from 1
            // Only assign combo orders to actions that are in the combo sequence
            ComboSequence.Sort((a, b) => a.ComboOrder.CompareTo(b.ComboOrder));
            for (int i = 0; i < ComboSequence.Count; i++)
            {
                ComboSequence[i].ComboOrder = i + 1;
            }
        }
        
        private void InitializeDefaultCombo()
        {
            // Clear existing combo sequence
            ComboSequence.Clear();
            
            // Add the two weapon actions to the combo by default
            if (Weapon is WeaponItem weapon)
            {
                var weaponActions = GetGearActions(weapon);
                foreach (var actionName in weaponActions)
                {
                    // Find the action in the action pool and add it to combo
                    var action = ActionPool.FirstOrDefault(a => a.action.Name == actionName);
                    if (action.action != null && action.action.IsComboAction)
                    {
                        AddToCombo(action.action);
                    }
                }
            }
        }

        public void SetTempComboBonus(int bonus, int turns)
        {
            TempComboBonus = bonus;
            TempComboBonusTurns = turns;
        }

        public int ConsumeTempComboBonus()
        {
            int bonus = TempComboBonus;
            if (TempComboBonusTurns > 0)
            {
                TempComboBonusTurns--;
                if (TempComboBonusTurns == 0)
                    TempComboBonus = 0;
            }
            return bonus;
        }

        public void ApplyHealthMultiplier(double multiplier)
        {
            MaxHealth = (int)(MaxHealth * multiplier);
            CurrentHealth = MaxHealth;
        }

        /// <summary>
        /// Activates combo mode when a combo attack is triggered
        /// </summary>
        public void ActivateComboMode()
        {
            ComboModeActive = true;
        }

        /// <summary>
        /// Deactivates combo mode when combo fails or completes
        /// </summary>
        public void DeactivateComboMode()
        {
            ComboModeActive = false;
        }

        /// <summary>
        /// Resets combo state including combo mode
        /// </summary>
        public void ResetCombo()
        {
            ComboStep = 0;
            ComboAmplifier = 1.0;
            LastComboActionIdx = -1;
            ComboModeActive = false;
        }

        // New methods for advanced mechanics
        public void ApplyStatBonus(int bonus, string statType, int duration)
        {
            switch (statType.ToUpper())
            {
                case "STR":
                    TempStrengthBonus = bonus;
                    break;
                case "AGI":
                    TempAgilityBonus = bonus;
                    break;
                case "TEC":
                    TempTechniqueBonus = bonus;
                    break;
                case "INT":
                    TempIntelligenceBonus = bonus;
                    break;
            }
            TempStatBonusTurns = duration;
        }

        public void UpdateTempEffects()
        {
            // Update temporary stat bonuses
            if (TempStatBonusTurns > 0)
            {
                TempStatBonusTurns--;
                if (TempStatBonusTurns == 0)
                {
                    TempStrengthBonus = 0;
                    TempAgilityBonus = 0;
                    TempTechniqueBonus = 0;
                    TempIntelligenceBonus = 0;
                }
            }

            // Update stun
            if (StunTurnsRemaining > 0)
            {
                StunTurnsRemaining--;
                if (StunTurnsRemaining == 0)
                    IsStunned = false;
            }

            // Update length reduction
            if (LengthReductionTurns > 0)
            {
                LengthReductionTurns--;
                if (LengthReductionTurns == 0)
                    LengthReduction = 0.0;
            }

            // Update enemy roll penalty
            if (EnemyRollPenaltyTurns > 0)
            {
                EnemyRollPenaltyTurns--;
                if (EnemyRollPenaltyTurns == 0)
                    EnemyRollPenalty = 0;
            }

            // Update combo amplifier multiplier
            if (ComboAmplifierTurns > 0)
            {
                ComboAmplifierTurns--;
                if (ComboAmplifierTurns == 0)
                    ComboAmplifierMultiplier = 1.0;
            }

            // Update extra damage decay
            if (ExtraDamage > 0)
            {
                ExtraDamage = Math.Max(0, ExtraDamage - 1); // Default decay of 1 per turn
            }

            // Update damage reduction decay
            if (DamageReduction > 0)
            {
                DamageReduction = Math.Max(0.0, DamageReduction - 0.1); // Default decay of 10% per turn
            }
        }

        public int GetEffectiveStrength()
        {
            return Strength + TempStrengthBonus + GetEquipmentStatBonus("STR");
        }

        public int GetEffectiveAgility()
        {
            return Agility + TempAgilityBonus + GetEquipmentStatBonus("AGI");
        }

        public int GetEffectiveTechnique()
        {
            return Technique + TempTechniqueBonus + GetEquipmentStatBonus("TEC");
        }

        public int GetEffectiveIntelligence()
        {
            return Intelligence + TempIntelligenceBonus + GetEquipmentStatBonus("INT");
        }

        /// <summary>
        /// Gets the total stat bonus from all equipped items for a specific stat type
        /// </summary>
        private int GetEquipmentStatBonus(string statType)
        {
            int totalBonus = 0;
            
            // Check all equipped items
            var equippedItems = new[] { Head, Body, Weapon, Feet };
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var statBonus in item.StatBonuses)
                    {
                        // Check for exact stat type match
                        if (statBonus.StatType == statType)
                        {
                            totalBonus += (int)statBonus.Value;
                        }
                        // Check for "ALL" stat type which applies to all stats
                        else if (statBonus.StatType == "ALL")
                        {
                            totalBonus += (int)statBonus.Value;
                        }
                    }
                }
            }
            
            return totalBonus;
        }

        /// <summary>
        /// Gets the total damage bonus from all equipped items
        /// </summary>
        public int GetEquipmentDamageBonus()
        {
            return GetEquipmentStatBonus("Damage");
        }

        /// <summary>
        /// Gets the total health bonus from all equipped items
        /// </summary>
        public int GetEquipmentHealthBonus()
        {
            return GetEquipmentStatBonus("Health");
        }

        /// <summary>
        /// Gets the total armor from all equipped items
        /// </summary>
        public int GetTotalArmor()
        {
            return GetEquipmentStatBonus("Armor");
        }

        /// <summary>
        /// Gets the effective max health including equipment bonuses
        /// </summary>
        public int GetEffectiveMaxHealth()
        {
            return MaxHealth + GetEquipmentHealthBonus();
        }

        public double GetHealthPercentage()
        {
            return (double)CurrentHealth / GetEffectiveMaxHealth();
        }

        public bool MeetsHealthThreshold(double threshold)
        {
            return GetHealthPercentage() <= threshold;
        }

        public bool MeetsStatThreshold(string statType, double threshold)
        {
            return statType.ToUpper() switch
            {
                "STR" => GetEffectiveStrength() >= threshold,
                "AGI" => GetEffectiveAgility() >= threshold,
                "TEC" => GetEffectiveTechnique() >= threshold,
                "INT" => GetEffectiveIntelligence() >= threshold,
                _ => false
            };
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
            Console.WriteLine($"Strength: {GetEffectiveStrength()}");
            Console.WriteLine($"Agility: {GetEffectiveAgility()}");
            Console.WriteLine($"Technique: {GetEffectiveTechnique()}");
            Console.WriteLine($"Intelligence: {GetEffectiveIntelligence()}");
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

        // Class-specific action methods
        private void AddBarbarianActions()
        {
            // Add special Barbarian class action when they have at least 5 points
            if (BarbarianPoints >= 5)
            {
                var berserkerRage = ActionLoader.GetAction("BERSERKER RAGE");
                if (berserkerRage != null)
                {
                    AddAction(berserkerRage, 0.0);
                }
            }
            // Future thresholds: 10, 20, 40, 80, 100 points for additional abilities
        }

        private void AddWarriorActions()
        {
            // Add special Warrior class action when they have at least 5 points
            if (WarriorPoints >= 5)
            {
                var heroicStrike = ActionLoader.GetAction("HEROIC STRIKE");
                if (heroicStrike != null)
                {
                    AddAction(heroicStrike, 0.0);
                }
            }
        }

        private void AddRogueActions()
        {
            // Add special Rogue class action when they have at least 5 points
            if (RoguePoints >= 5)
            {
                var shadowStrike = ActionLoader.GetAction("SHADOW STRIKE");
                if (shadowStrike != null)
                {
                    AddAction(shadowStrike, 0.0);
                }
            }
        }

        private void AddWizardActions()
        {
            // Add special Wizard class action when they have at least 5 points
            if (WizardPoints >= 5)
            {
                var meteor = ActionLoader.GetAction("METEOR");
                if (meteor != null)
                {
                    AddAction(meteor, 0.0);
                }
            }
        }

        /// <summary>
        /// Gets the next class upgrade threshold for a given class
        /// </summary>
        public int GetNextClassThreshold(string className)
        {
            int currentPoints = className switch
            {
                "Barbarian" => BarbarianPoints,
                "Warrior" => WarriorPoints,
                "Rogue" => RoguePoints,
                "Wizard" => WizardPoints,
                _ => 0
            };

            // Define upgrade thresholds
            int[] thresholds = { 5, 10, 20, 40, 80, 100 };
            
            foreach (int threshold in thresholds)
            {
                if (currentPoints < threshold)
                {
                    return threshold;
                }
            }
            
            return -1; // Max level reached
        }

        /// <summary>
        /// Gets a string showing next class upgrade thresholds
        /// </summary>
        public string GetClassUpgradeInfo()
        {
            var upgradeInfo = new List<string>();
            
            var classes = new[] { ("Barbarian", BarbarianPoints), ("Warrior", WarriorPoints), ("Rogue", RoguePoints), ("Wizard", WizardPoints) };
            
            // Filter to only show classes with at least 1 point, then sort by points (highest first) and take top 2
            var classesWithPoints = classes.Where(c => c.Item2 > 0);
            var sortedClasses = classesWithPoints.OrderByDescending(c => c.Item2).Take(2);
            
            foreach (var (className, points) in sortedClasses)
            {
                int nextThreshold = GetNextClassThreshold(className);
                if (nextThreshold > 0)
                {
                    int pointsNeeded = nextThreshold - points;
                    upgradeInfo.Add($"{className}: {pointsNeeded} to go");
                }
                else
                {
                    upgradeInfo.Add($"{className}: MAX");
                }
            }
            
            return string.Join(" | ", upgradeInfo);
        }

        // Class progression methods
        public void AwardClassPoint(WeaponType weaponType)
        {
            switch (weaponType)
            {
                case WeaponType.Mace:
                    BarbarianPoints++;
                    break;
                case WeaponType.Sword:
                    WarriorPoints++;
                    break;
                case WeaponType.Dagger:
                    RoguePoints++;
                    break;
                case WeaponType.Wand:
                    WizardPoints++;
                    break;
            }
            
            // Re-add class actions when points change
            AddClassActions();
        }

        public string GetCurrentClass()
        {
            var classes = new List<(string name, int points)>
            {
                ("Barbarian", BarbarianPoints),
                ("Warrior", WarriorPoints),
                ("Rogue", RoguePoints),
                ("Wizard", WizardPoints)
            };

            // Sort by points descending
            classes.Sort((a, b) => b.points.CompareTo(a.points));

            if (classes[0].points == 0)
                return "Fighter";

            string primaryClass = GetClassTier(classes[0].name, classes[0].points);
            
            // Check for hybrid classes
            if (classes[1].points >= 5)
            {
                string secondaryClass = GetClassTier(classes[1].name, classes[1].points);
                return $"{primaryClass}-{secondaryClass}";
            }

            return primaryClass;
        }

        private string GetClassTier(string baseClass, int points)
        {
            if (points >= 40)
                return $"Grand {baseClass}";
            if (points >= 20)
                return $"Master {baseClass}";
            if (points >= 5)
                return baseClass;
            return "Novice";
        }

        private void AddWeaponActions(WeaponItem weapon)
        {
            // Add gear-specific actions based on weapon type
            AddGearActions(weapon);
        }

        private void AddGearActions(Item gear)
        {
            // Define gear-specific actions that are lost when unequipped
            var gearActions = GetGearActions(gear);
            
            // Determine if this gear should have actions
            bool shouldHaveActions = false;
            
            if (gear.Type == ItemType.Weapon)
            {
                // All weapons (starter and loot) always get at least 1 action
                shouldHaveActions = true;
            }
            else if (!gear.IsStarterItem)
            {
                // For non-starter armor, use probability-based action assignment
                // Higher tier items have better chance of having actions
                double actionChance = Math.Min(0.3 + (gear.Tier * 0.1), 0.8); // 30% base + 10% per tier, max 80%
                shouldHaveActions = new Random().NextDouble() < actionChance;
            }
            // Starter armor (IsStarterItem = true but Type != Weapon) gets no actions
            
            if (shouldHaveActions)
            {
                foreach (var actionName in gearActions)
                {
                    // Load gear actions from JSON to ensure they're properly configured
                    LoadGearActionFromJson(actionName);
                }
                
                // Apply roll bonuses from stat bonuses to all actions
                ApplyRollBonusesFromGear(gear);
            }
        }

        private List<string> GetGearActions(Item gear)
        {
            if (gear is WeaponItem weapon)
            {
                return weapon.WeaponType switch
                {
                    WeaponType.Sword => new List<string> { "PARRY", "SWORD SLASH" },
                    WeaponType.Mace => new List<string> { "CRUSHING BLOW", "SHIELD BREAK" },
                    WeaponType.Dagger => new List<string> { "QUICK STAB", "POISON BLADE" },
                    WeaponType.Wand => new List<string> { "MAGIC MISSILE", "ARCANE SHIELD" },
                    _ => new List<string>()
                };
            }
            else if (gear is HeadItem)
            {
                return new List<string> { "HEADBUTT" };
            }
            else if (gear is ChestItem)
            {
                return new List<string> { "CHEST BASH" };
            }
            else if (gear is FeetItem)
            {
                return new List<string> { "KICK" };
            }
            
            return new List<string>();
        }

        private void LoadGearActionFromJson(string actionName)
        {
            try
            {
                string[] possiblePaths = {
                    Path.Combine("GameData", "Actions.json"),
                    Path.Combine("..", "GameData", "Actions.json"),
                    Path.Combine("..", "..", "GameData", "Actions.json"),
                    Path.Combine("DF4 - CONSOLE", "GameData", "Actions.json"),
                    Path.Combine("..", "DF4 - CONSOLE", "GameData", "Actions.json")
                };

                string? foundPath = null;
                foreach (string path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        foundPath = path;
                        break;
                    }
                }
                
                if (foundPath != null)
                {
                    string jsonContent = File.ReadAllText(foundPath);
                    var allActions = System.Text.Json.JsonSerializer.Deserialize<List<ActionData>>(jsonContent);
                    
                    if (allActions != null)
                    {
                        var actionData = allActions.FirstOrDefault(a => a.Name == actionName);
                        if (actionData != null)
                        {
                            var action = CreateActionFromData(actionData);
                            if (action.IsComboAction)
                            {
                                // Find the highest combo order among combo actions only and add 1
                                var comboActions = ActionPool.Where(a => a.action.IsComboAction).ToList();
                                int maxOrder = comboActions.Count > 0 ? comboActions.Max(a => a.action.ComboOrder) : 0;
                                action.ComboOrder = maxOrder + 1;
                                
                                AddAction(action, 0.0);
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Actions.json not found when loading gear action {actionName}. Tried paths: {string.Join(", ", possiblePaths)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading gear action {actionName} from JSON: {ex.Message}");
            }
        }

        private void RemoveWeaponActions()
        {
            // Remove weapon-specific gear actions
            var weaponActions = new[] { "SWORD SLASH", "PARRY", "QUICK STAB", "POISON BLADE", "CRUSHING BLOW", "SHIELD BREAK", "MAGIC MISSILE", "ARCANE SHIELD" };
            foreach (var actionName in weaponActions)
            {
                var actionToRemove = ActionPool.FirstOrDefault(a => a.action.Name == actionName);
                if (actionToRemove.action != null)
                {
                    RemoveAction(actionToRemove.action);
                }
            }
        }

        private void RemoveClassActions()
        {
            // Remove class-specific actions (core combo actions)
            var classActions = new[] { 
                "TAUNT", "JAB", "STUN", "CRIT", "SHIELD BASH", "DEFENSIVE STANCE", 
                "BERSERK", "BLOOD FRENZY", "PRECISION STRIKE", "QUICK REFLEXES",
                "FOCUS", "READ BOOK"
            };
            foreach (var actionName in classActions)
            {
                var actionToRemove = ActionPool.FirstOrDefault(a => a.action.Name == actionName);
                if (actionToRemove.action != null)
                {
                    RemoveAction(actionToRemove.action);
                }
            }
        }

        private void AddArmorActions(Item armor)
        {
            // Add gear-specific actions for armor
            AddGearActions(armor);
        }

        private void ApplyRollBonusesFromGear(Item gear)
        {
            // Find all roll bonus stat bonuses from the gear
            int totalRollBonus = 0;
            foreach (var statBonus in gear.StatBonuses)
            {
                if (statBonus.StatType == "RollBonus")
                {
                    totalRollBonus += (int)statBonus.Value;
                }
            }
            
            // Apply the roll bonus to all actions in the action pool
            if (totalRollBonus > 0)
            {
                foreach (var actionEntry in ActionPool)
                {
                    actionEntry.action.RollBonus += totalRollBonus;
                }
            }
        }

        private void RemoveRollBonusesFromGear(Item gear)
        {
            // Find all roll bonus stat bonuses from the gear
            int totalRollBonus = 0;
            foreach (var statBonus in gear.StatBonuses)
            {
                if (statBonus.StatType == "RollBonus")
                {
                    totalRollBonus += (int)statBonus.Value;
                }
            }
            
            // Remove the roll bonus from all actions in the action pool
            if (totalRollBonus > 0)
            {
                foreach (var actionEntry in ActionPool)
                {
                    actionEntry.action.RollBonus -= totalRollBonus;
                }
            }
        }

        /// <summary>
        /// Calculates the combo amplification multiplier based on Technique
        /// Formula: Linear scaling from ComboAmplifierAtTech5 (at TEC=5) to ComboAmplifierMax (at TEC=100)
        /// </summary>
        public double GetComboAmplifier()
        {
            var tuning = TuningConfig.Instance;
            
            // Clamp Technique to valid range
            int clampedTech = Math.Max(1, Math.Min(tuning.ComboSystem.ComboAmplifierMaxTech, GetEffectiveTechnique()));
            
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

        /// <summary>
        /// Sets Technique for testing purposes
        /// </summary>
        public void SetTechniqueForTesting(int value)
        {
            Technique = value;
        }

        /// <summary>
        /// Gets the current combo amplification for the current combo step
        /// </summary>
        public double GetCurrentComboAmplification()
        {
            var comboActions = GetComboActions();
            if (comboActions.Count == 0) return 1.0;
            
            int currentStep = ComboStep % comboActions.Count;
            double baseAmp = GetComboAmplifier();
            return Math.Pow(baseAmp, currentStep + 1);
        }

        /// <summary>
        /// Gets combo information for display
        /// </summary>
        public string GetComboInfo()
        {
            var comboActions = GetComboActions();
            if (comboActions.Count == 0) return "No combo actions available";
            
            int currentStep = ComboStep % comboActions.Count;
            double baseAmp = GetComboAmplifier();
            double currentAmp = Math.Pow(baseAmp, currentStep + 1);
            
            return $"Combo Step: {currentStep + 1}/{comboActions.Count} | Amplification: {currentAmp:F2}x";
        }

        /// <summary>
        /// Calculates the number of attacks this character can make per turn based on their attack speed
        /// </summary>
        /// <returns>Number of attacks (minimum 1)</returns>
        public int GetAttacksPerTurn()
        {
            double attackSpeed = GetTotalAttackSpeed();
            int attacks = (int)Math.Floor(attackSpeed);
            return Math.Max(1, attacks); // Always at least 1 attack
        }

        /// <summary>
        /// Calculates the total attack speed including weapon and agility bonuses
        /// </summary>
        /// <returns>Total attack speed value</returns>
        public double GetTotalAttackSpeed()
        {
            var tuning = TuningConfig.Instance;
            double weaponAttackSpeed = (Weapon is WeaponItem w) ? w.GetTotalAttackSpeed() : 1.0;
            double totalSpeed = weaponAttackSpeed + (GetEffectiveAgility() * tuning.Combat.AgilitySpeedBonus);
            return Math.Max(tuning.Combat.MinimumAttackSpeed, totalSpeed);
        }

        /// <summary>
        /// Calculates the Intelligence roll bonus (every 10 INT = +1 to rolls by default)
        /// </summary>
        /// <returns>Roll bonus from Intelligence</returns>
        public int GetIntelligenceRollBonus()
        {
            var tuning = TuningConfig.Instance;
            return GetEffectiveIntelligence() / tuning.Attributes.IntelligenceRollBonusPer; // Every X points of INT gives +1 to rolls
        }

        private void RemoveArmorActions(Item? armor)
        {
            if (armor == null) return;
            
            // Determine which action to remove based on armor type
            string actionName = "";
            
            if (armor is HeadItem)
            {
                actionName = "HEADBUTT";
            }
            else if (armor is ChestItem)
            {
                actionName = "CHEST BASH";
            }
            else if (armor is FeetItem)
            {
                actionName = "KICK";
            }
            
            if (!string.IsNullOrEmpty(actionName))
            {
                var actionToRemove = ActionPool.FirstOrDefault(a => a.action.Name == actionName);
                if (actionToRemove.action != null)
                {
                    RemoveAction(actionToRemove.action);
                }
            }
        }

        // Save/Load methods
        public void SaveCharacter(string filename = "character_save.json")
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
                    Strength = Strength,
                    Agility = Agility,
                    Technique = Technique,
                    Intelligence = Intelligence,
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
                Console.WriteLine($"Character saved to {filename}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving character: {ex.Message}");
            }
        }

        public static Character? LoadCharacter(string filename = "character_save.json")
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
                character.XP = saveData.XP;
                character.CurrentHealth = saveData.CurrentHealth;
                character.MaxHealth = saveData.MaxHealth;
                character.Strength = saveData.Strength;
                character.Agility = saveData.Agility;
                character.Technique = saveData.Technique;
                character.Intelligence = saveData.Intelligence;
                character.BarbarianPoints = saveData.BarbarianPoints;
                character.WarriorPoints = saveData.WarriorPoints;
                character.RoguePoints = saveData.RoguePoints;
                character.WizardPoints = saveData.WizardPoints;
                character.ComboStep = saveData.ComboStep;
                character.ComboBonus = saveData.ComboBonus;
                character.TempComboBonus = saveData.TempComboBonus;
                character.TempComboBonusTurns = saveData.TempComboBonusTurns;
                character.DamageReduction = saveData.DamageReduction;
                character.Inventory = saveData.Inventory;
                character.Head = saveData.Head;
                character.Body = saveData.Body;
                character.Weapon = saveData.Weapon;
                character.Feet = saveData.Feet;

                // Rebuild action pool with proper structure
                character.ActionPool.Clear();
                character.AddDefaultActions(); // Add basic attack
                character.AddClassActions(); // Add class actions based on weapon
                
                // Re-add gear actions for equipped items (with probability for non-starter items)
                if (character.Head != null)
                    character.AddArmorActions(character.Head);
                if (character.Body != null)
                    character.AddArmorActions(character.Body);
                if (character.Weapon is WeaponItem weapon)
                    character.AddWeaponActions(weapon);
                if (character.Feet != null)
                    character.AddArmorActions(character.Feet);

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

        public static void DeleteSaveFile(string filename = "character_save.json")
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