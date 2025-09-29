using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Manages character actions, combo system, and action-related mechanics
    /// </summary>
    public class CharacterActions
    {
        public List<Action> ComboSequence { get; private set; } = new List<Action>();

        public CharacterActions()
        {
        }

        public double CalculateTurnsFromActionLength(double actionLength)
        {
            // Basic action length to turns conversion
            // 1.0 action length = 1 turn
            return actionLength;
        }

        public void RemoveItemActions(Entity entity)
        {
            // Remove actions that were added from items
            // Implementation tracks which actions came from items and removes them
            RemoveClassActions(entity);
        }

        public void AddDefaultActions(Entity entity)
        {
            DebugLogger.LogMethodEntry("CharacterActions", "AddDefaultActions");
            
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
            entity.AddAction(basicAttack, 1.0); // High probability for basic attack
            DebugLogger.Log("CharacterActions", "Added BASIC ATTACK to ActionPool");
        }

        public void AddClassActions(Entity entity, CharacterProgression progression, WeaponType? weaponType)
        {
            DebugLogger.LogMethodEntry("CharacterActions", "AddClassActions");
            
            // Remove existing class actions first
            RemoveClassActions(entity);
            
            DebugLogger.LogClassPoints(progression.BarbarianPoints, progression.WarriorPoints, progression.RoguePoints, progression.WizardPoints);
            
            AddBarbarianActions(entity, progression);
            AddWarriorActions(entity, progression);
            AddRogueActions(entity, progression);
            AddWizardActions(entity, progression, weaponType);
            
            DebugLogger.LogActionPoolChange(entity.Name, entity.ActionPool.Count, "After AddClassActions");
        }

        private void AddBarbarianActions(Entity entity, CharacterProgression progression)
        {
            // Add special Barbarian class action when they have at least 5 points
            if (progression.BarbarianPoints >= 5)
            {
                var berserkerRage = ActionLoader.GetAction("BERSERKER RAGE");
                if (berserkerRage != null)
                {
                    entity.AddAction(berserkerRage, 1.0);
                }
            }
        }

        private void AddWarriorActions(Entity entity, CharacterProgression progression)
        {
            // Add special Warrior class actions when they have at least 5 points
            if (progression.WarriorPoints >= 5)
            {
                var heroicStrike = ActionLoader.GetAction("HEROIC STRIKE");
                if (heroicStrike != null)
                {
                    entity.AddAction(heroicStrike, 1.0);
                }
                
                var whirlwind = ActionLoader.GetAction("WHIRLWIND");
                if (whirlwind != null)
                {
                    entity.AddAction(whirlwind, 1.0);
                }
            }
        }

        private void AddRogueActions(Entity entity, CharacterProgression progression)
        {
            // Add special Rogue class action when they have at least 5 points
            if (progression.RoguePoints >= 5)
            {
                var shadowStrike = ActionLoader.GetAction("SHADOW STRIKE");
                if (shadowStrike != null)
                {
                    entity.AddAction(shadowStrike, 1.0);
                }
            }
        }

        private void AddWizardActions(Entity entity, CharacterProgression progression, WeaponType? weaponType)
        {
            // Only add wizard actions if the character is actually a wizard class
            bool isWizardClass = progression.IsWizardClass(weaponType);
            
            if (isWizardClass)
            {
                // Add FIREBALL as a basic wizard spell (available at 3+ wizard points)
                if (progression.WizardPoints >= 3)
                {
                    var fireball = ActionLoader.GetAction("FIREBALL");
                    if (fireball != null)
                    {
                        entity.AddAction(fireball, 1.0);
                    }
                }
                
                // Add special Wizard class action when they have at least 5 points
                if (progression.WizardPoints >= 5)
                {
                    var meteor = ActionLoader.GetAction("METEOR");
                    if (meteor != null)
                    {
                        entity.AddAction(meteor, 1.0);
                    }
                }
            }
        }

        private void RemoveClassActions(Entity entity)
        {
            // Remove class-specific actions (core combo actions)
            var classActions = new[] { 
                "TAUNT", "JAB", "STUN", "CRIT", "SHIELD BASH", "DEFENSIVE STANCE", 
                "BERSERK", "BLOOD FRENZY", "PRECISION STRIKE", "QUICK REFLEXES",
                "FOCUS", "READ BOOK", "HEROIC STRIKE", "WHIRLWIND", "BERSERKER RAGE", "SHADOW STRIKE"
            };
            foreach (var actionName in classActions)
            {
                var actionToRemove = entity.ActionPool.FirstOrDefault(a => a.action.Name == actionName);
                if (actionToRemove.action != null)
                {
                    entity.RemoveAction(actionToRemove.action);
                }
            }
        }

        public void AddWeaponActions(Entity entity, WeaponItem weapon)
        {
            DebugLogger.LogFormat("CharacterActions", "AddWeaponActions called for {0} (Type: {1})", weapon.Name, weapon.WeaponType);
            
            AddGearActions(entity, weapon);
            
            DebugLogger.LogActionPoolChange(entity.Name, entity.ActionPool.Count, "After AddWeaponActions");
        }

        public void AddArmorActions(Entity entity, Item armor)
        {
            AddGearActions(entity, armor);
        }

        private void AddGearActions(Entity entity, Item gear)
        {
            DebugLogger.LogFormat("CharacterActions", "AddGearActions called for {0}", gear.Name);
            
            var gearActions = GetGearActions(gear);
            DebugLogger.LogGearActions(gear.Name, gearActions.Count, string.Join(", ", gearActions));
            
            if (gearActions.Count > 0)
            {
                foreach (var actionName in gearActions)
                {
                    DebugLogger.LogFormat("CharacterActions", "Loading gear action: {0}", actionName);
                    
                    LoadGearActionFromJson(entity, actionName);
                }
                
                ApplyRollBonusesFromGear(entity, gear);
            }
            else
            {
                DebugLogger.LogFormat("CharacterActions", "No gear actions to add for {0}", gear.Name);
            }
        }

        private List<string> GetGearActions(Item gear)
        {
            var actions = new List<string>();
            
            if (gear is WeaponItem weapon)
            {
                actions.AddRange(GetWeaponActionsFromJson(weapon.WeaponType));
            }
            else if (gear is HeadItem || gear is ChestItem || gear is FeetItem)
            {
                if (HasSpecialArmorActions(gear))
                {
                    if (!string.IsNullOrEmpty(gear.GearAction))
                    {
                        actions.Add(gear.GearAction);
                    }
                    else
                    {
                        actions.AddRange(GetRandomArmorActionFromJson(gear));
                    }
                }
            }
            
            foreach (var actionBonus in gear.ActionBonuses)
            {
                if (!string.IsNullOrEmpty(actionBonus.Name))
                {
                    actions.Add(actionBonus.Name);
                }
            }
            
            return actions;
        }

        private List<string> GetWeaponActionsFromJson(WeaponType weaponType)
        {
            var weaponTag = weaponType.ToString().ToLower();
            if (TuningConfig.IsDebugEnabled)
                DebugLogger.LogFormat("CharacterActions", "GetWeaponActionsFromJson called for {0} (tag: {1})", weaponType, weaponTag);
            
            var allActions = ActionLoader.GetAllActions();
            if (TuningConfig.IsDebugEnabled)
                DebugLogger.LogFormat("CharacterActions", "Got {0} total actions from ActionLoader", allActions.Count);
            
            // For mace weapons, return the specific mace actions
            if (weaponType == WeaponType.Mace)
            {
                if (TuningConfig.IsDebugEnabled)
                    DebugLogger.Log("CharacterActions", "Using hardcoded mace actions");
                return new List<string> { "CRUSHING BLOW", "SHIELD BREAK", "THUNDER CLAP" };
            }
            
            // For other weapon types, use the original logic
            var weaponActions = allActions
                .Where(action => action.Tags.Contains("weapon") && 
                                action.Tags.Contains(weaponTag) &&
                                !action.Tags.Contains("unique"))
                .Select(action => action.Name)
                .ToList();
                
            if (TuningConfig.IsDebugEnabled)
                Console.WriteLine($"DEBUG: Found {weaponActions.Count} weapon actions for {weaponType}: {string.Join(", ", weaponActions)}");
            return weaponActions;
        }

        private List<string> GetRandomArmorActionFromJson(Item armor)
        {
            var randomAction = GetRandomArmorActionName();
            if (!string.IsNullOrEmpty(randomAction))
            {
                return new List<string> { randomAction };
            }
            
            var allActions = ActionLoader.GetAllActions();
            
            var armorActions = allActions
                .Where(action => action.Tags.Contains("armor") && 
                                !action.Tags.Contains("environment"))
                .Select(action => action.Name)
                .ToList();

            if (armorActions.Count == 0)
            {
                armorActions = allActions
                    .Where(action => action.IsComboAction && 
                                    !action.Tags.Contains("environment") &&
                                    !action.Tags.Contains("enemy") &&
                                    !action.Tags.Contains("weapon"))
                    .Select(action => action.Name)
                    .ToList();
            }

            if (armorActions.Count > 0)
            {
                var fallbackAction = armorActions[Random.Shared.Next(armorActions.Count)];
                return new List<string> { fallbackAction };
            }
            
            return new List<string>();
        }

        private string? GetRandomArmorActionName()
        {
            var allActions = ActionLoader.GetAllActions();
            var availableActions = allActions
                .Where(action => action.IsComboAction && 
                               !action.Tags.Contains("environment") &&
                               !action.Tags.Contains("enemy") &&
                               !action.Tags.Contains("unique"))
                .Select(action => action.Name)
                .ToList();

            if (availableActions.Count > 0)
            {
                return availableActions[Random.Shared.Next(availableActions.Count)];
            }
            
            return null;
        }

        private bool HasSpecialArmorActions(Item armor)
        {
            if (armor.Modifications.Count > 0)
            {
                return true;
            }
            
            if (armor.StatBonuses.Count > 0)
            {
                return true;
            }
            
            if (armor.ActionBonuses.Count > 0)
            {
                return true;
            }
            
            // Basic gear names moved to shared configuration
            string[] basicGearNames = BasicGearConfig.GetBasicGearNames();
            if (basicGearNames.Contains(armor.Name))
            {
                return false;
            }
            
            return false;
        }

        private void LoadGearActionFromJson(Entity entity, string actionName)
        {
            if (TuningConfig.IsDebugEnabled)
                Console.WriteLine($"DEBUG: LoadGearActionFromJson called for action: {actionName}");
            
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
                    if (TuningConfig.IsDebugEnabled)
                        Console.WriteLine($"DEBUG: Found Actions.json at: {foundPath}");
                    
                    string jsonContent = File.ReadAllText(foundPath);
                    var allActions = System.Text.Json.JsonSerializer.Deserialize<List<ActionData>>(jsonContent);
                    
                    if (allActions != null)
                    {
                        if (TuningConfig.IsDebugEnabled)
                            Console.WriteLine($"DEBUG: Deserialized {allActions.Count} actions from JSON");
                        
                        var actionData = allActions.FirstOrDefault(a => a.Name == actionName);
                        if (actionData != null)
                        {
                            if (TuningConfig.IsDebugEnabled)
                                Console.WriteLine($"DEBUG: Found action data for {actionName}");
                            
                            var action = CreateActionFromData(actionData);
                            if (action.IsComboAction)
                            {
                                // Find the highest combo order among combo actions only and add 1
                                var comboActions = entity.ActionPool.Where(a => a.action.IsComboAction).ToList();
                                int maxOrder = comboActions.Count > 0 ? comboActions.Max(a => a.action.ComboOrder) : 0;
                                action.ComboOrder = maxOrder + 1;
                                
                                entity.AddAction(action, 1.0);
                                if (TuningConfig.IsDebugEnabled)
                                    Console.WriteLine($"DEBUG: Successfully added action {actionName} to ActionPool");
                            }
                            else
                            {
                                if (TuningConfig.IsDebugEnabled)
                                    Console.WriteLine($"DEBUG: Action {actionName} is not a combo action, skipping");
                            }
                        }
                        else
                        {
                            if (TuningConfig.IsDebugEnabled)
                                Console.WriteLine($"DEBUG: Action {actionName} not found in Actions.json");
                        }
                    }
                    else
                    {
                        if (TuningConfig.IsDebugEnabled)
                            Console.WriteLine($"DEBUG: Failed to deserialize Actions.json");
                    }
                }
                else
                {
                    Console.WriteLine($"ERROR: Actions.json not found when loading gear action {actionName}. Tried paths: {string.Join(", ", possiblePaths)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading gear action {actionName} from JSON: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private Action CreateActionFromData(ActionData data)
        {
            var actionType = ParseActionType(data.Type);
            var targetType = ParseTargetType(data.TargetType);

            // Enhance description with modifiers
            string enhancedDescription = EnhanceActionDescription(data);
            
            var action = new Action(
                name: data.Name,
                type: actionType,
                targetType: targetType,
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
            
            // Set the new debuff properties
            action.CausesSlow = data.CausesSlow;
            action.CausesPoison = data.CausesPoison;
            action.CausesBurn = data.CausesBurn;
            
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
            action.Tags = data.Tags ?? new List<string>();
            action.EnemyRollPenalty = data.EnemyRollPenalty;
                
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
            
            // Add stat bonus information
            if (data.StatBonus > 0 && !string.IsNullOrEmpty(data.StatBonusType))
            {
                string durationText = data.StatBonusDuration == -1 ? "dungeon" : $"{data.StatBonusDuration} turns";
                modifiers.Add($"+{data.StatBonus} {data.StatBonusType} ({durationText})");
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

        private TargetType ParseTargetType(string targetType)
        {
            return targetType.ToLower() switch
            {
                "self" => TargetType.Self,
                "singletarget" => TargetType.SingleTarget,
                "areaofeffect" => TargetType.AreaOfEffect,
                "environment" => TargetType.Environment,
                _ => TargetType.SingleTarget
            };
        }

        public void ApplyRollBonusesFromGear(Entity entity, Item gear)
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
                foreach (var actionEntry in entity.ActionPool)
                {
                    actionEntry.action.RollBonus += totalRollBonus;
                }
            }
        }

        public void RemoveWeaponActions(Entity entity, WeaponItem? weapon = null)
        {
            if (weapon != null)
            {
                var weaponActions = GetWeaponActionsFromJson(weapon.WeaponType);
                
                foreach (var actionName in weaponActions)
                {
                    var actionToRemove = entity.ActionPool.FirstOrDefault(a => a.action.Name == actionName);
                    if (actionToRemove.action != null)
                    {
                        entity.RemoveAction(actionToRemove.action);
                    }
                }
            }
        }

        public void RemoveArmorActions(Entity entity, Item? armor)
        {
            if (armor == null) return;
            
            var armorActions = GetGearActions(armor);
            
            foreach (var actionName in armorActions)
            {
                var actionToRemove = entity.ActionPool.FirstOrDefault(a => a.action.Name == actionName);
                if (actionToRemove.action != null)
                {
                    entity.RemoveAction(actionToRemove.action);
                }
            }
        }

        public void RemoveRollBonusesFromGear(Entity entity, Item gear)
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
                foreach (var actionEntry in entity.ActionPool)
                {
                    actionEntry.action.RollBonus -= totalRollBonus;
                }
            }
        }

        public void AddEnvironmentActions(Entity entity, Environment environment)
        {
            // Add environment actions to the player's action pool
            if (environment != null && environment.ActionPool.Count > 0)
            {
                foreach (var (action, probability) in environment.ActionPool)
                {
                    // Add environment actions with lower probability (they're situational)
                    entity.AddAction(action, probability * 0.5); // 50% of environment's probability
                }
            }
        }

        public void ClearEnvironmentActions(Entity entity)
        {
            // Remove all actions that have the "environment" tag
            var actionsToRemove = new List<Action>();
            foreach (var (action, probability) in entity.ActionPool)
            {
                if (action.Tags.Contains("environment"))
                {
                    actionsToRemove.Add(action);
                }
            }
            
            foreach (var action in actionsToRemove)
            {
                entity.RemoveAction(action);
            }
        }

        // Combo system methods
        public List<Action> GetComboActions()
        {
            // Return the current combo sequence, sorted by combo order
            var sortedCombo = ComboSequence.ToList();
            sortedCombo.Sort((a, b) => a.ComboOrder.CompareTo(b.ComboOrder));
            return sortedCombo;
        }
        
        public List<Action> GetActionPool(Entity entity)
        {
            // Return all available actions from the action pool (no ordering)
            var allActions = new List<Action>();
            foreach (var entry in entity.ActionPool)
            {
                if (entry.action.IsComboAction)
                    allActions.Add(entry.action);
            }
            return allActions;
        }
        
        public void AddToCombo(Action action)
        {
            if (action.IsComboAction)
            {
                ComboSequence.Add(action);
                // Reassign combo orders to be sequential starting from 1
                ReorderComboSequence();
            }
        }
        
        public void RemoveFromCombo(Action action)
        {
            var actionToRemove = ComboSequence.FirstOrDefault(comboAction => comboAction.Name == action.Name);
            if (actionToRemove != null)
            {
                ComboSequence.Remove(actionToRemove);
                // Reset the action's combo order since it's no longer in the combo
                actionToRemove.ComboOrder = 0;
                // Reassign combo orders to be sequential starting from 1
                ReorderComboSequence();
            }
        }
        
        private void ReorderComboSequence()
        {
            // Sort by current combo order, then reassign sequential orders starting from 1
            ComboSequence.Sort((a, b) => a.ComboOrder.CompareTo(b.ComboOrder));
            for (int i = 0; i < ComboSequence.Count; i++)
            {
                ComboSequence[i].ComboOrder = i + 1;
            }
        }
        
        public void InitializeDefaultCombo(Entity entity, WeaponItem? weapon)
        {
            if (TuningConfig.IsDebugEnabled)
                Console.WriteLine("DEBUG: InitializeDefaultCombo called");
            
            // Clear existing combo sequence
            ComboSequence.Clear();
            
            // Add the two weapon actions to the combo by default
            if (weapon != null)
            {
                if (TuningConfig.IsDebugEnabled)
                    Console.WriteLine($"DEBUG: Found weapon: {weapon.Name} (Type: {weapon.WeaponType})");
                
                var weaponActions = GetGearActions(weapon);
                if (TuningConfig.IsDebugEnabled)
                    Console.WriteLine($"DEBUG: Found {weaponActions.Count} weapon actions: {string.Join(", ", weaponActions)}");
                
                foreach (var actionName in weaponActions)
                {
                    // Find the action in the action pool and add it to combo
                    var action = entity.ActionPool.FirstOrDefault(a => a.action.Name == actionName);
                    if (action.action != null && action.action.IsComboAction)
                    {
                        if (TuningConfig.IsDebugEnabled)
                            Console.WriteLine($"DEBUG: Adding {actionName} to combo sequence");
                        AddToCombo(action.action);
                    }
                    else
                    {
                        if (TuningConfig.IsDebugEnabled)
                            Console.WriteLine($"DEBUG: Could not add {actionName} to combo - action not found or not a combo action");
                    }
                }
            }
            else
            {
                if (TuningConfig.IsDebugEnabled)
                    Console.WriteLine("DEBUG: No weapon equipped, cannot initialize default combo");
            }
            
            if (TuningConfig.IsDebugEnabled)
                Console.WriteLine($"DEBUG: Combo sequence now has {ComboSequence.Count} actions");
        }

        public void UpdateComboSequenceAfterGearChange(Entity entity)
        {
            // Remove actions from combo sequence that are no longer in the action pool
            var actionsToRemove = new List<Action>();
            foreach (var comboAction in ComboSequence)
            {
                // Check if this action is still in the action pool
                var stillInPool = entity.ActionPool.Any(a => a.action.Name == comboAction.Name);
                if (!stillInPool)
                {
                    actionsToRemove.Add(comboAction);
                }
            }
            
            // Remove the actions that are no longer available
            foreach (var actionToRemove in actionsToRemove)
            {
                RemoveFromCombo(actionToRemove);
            }
            
            // Reorder the remaining combo sequence
            ReorderComboSequence();
        }

        public void UpdateComboBonus(CharacterEquipment equipment)
        {
            int bonus = 0;
            // Equipped items
            if (equipment.Head != null) bonus += equipment.Head.ComboBonus;
            if (equipment.Body != null) bonus += equipment.Body.ComboBonus;
            if (equipment.Weapon != null) bonus += equipment.Weapon.ComboBonus;
            if (equipment.Feet != null) bonus += equipment.Feet.ComboBonus;
            // Note: ComboBonus property would need to be moved to CharacterEffects
        }

        public List<Action> GetAvailableUniqueActions(WeaponItem? weapon)
        {
            var uniqueActions = new List<Action>();
            var allActions = ActionLoader.GetAllActions();
            
            // Get weapon-specific unique actions
            if (weapon != null)
            {
                string weaponType = weapon.WeaponType.ToString().ToLower();
                var weaponUniqueActions = allActions.Where(action => 
                    action.Tags.Contains("unique") && 
                    action.Tags.Contains("weapon") && 
                    action.Tags.Contains(weaponType)
                ).ToList();
                uniqueActions.AddRange(weaponUniqueActions);
            }
            
            // Get class-specific unique actions
            var classUniqueActions = allActions.Where(action => 
                action.Tags.Contains("unique") && 
                action.Tags.Contains("class")
            ).ToList();
            uniqueActions.AddRange(classUniqueActions);
            
            return uniqueActions;
        }
        
    }
}
