using RPGGame;
using RPGGame.Data;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Managers.Settings;
using RPGGame.UI.ColorSystem;
using RPGGame.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Generates and displays action test blocks for the settings panel
    /// Extracted from SettingsPanel.axaml.cs to reduce file size and improve separation of concerns
    /// </summary>
    public class SettingsActionTestGenerator
    {
        private readonly CanvasUICoordinator? canvasUI;
        private readonly Action<string, bool>? showStatusMessage;

        public SettingsActionTestGenerator(CanvasUICoordinator? canvasUI, Action<string, bool>? showStatusMessage)
        {
            this.canvasUI = canvasUI;
            this.showStatusMessage = showStatusMessage;
        }

        /// <summary>
        /// Generates a random action block and displays it in the center panel
        /// </summary>
        public void GenerateAndDisplayRandomActionBlock()
        {
            try
            {
                // Get a random action from all available actions
                var allActions = ActionLoader.GetAllActions();
                if (allActions == null || allActions.Count == 0)
                {
                    showStatusMessage?.Invoke("No actions available", false);
                    return;
                }
                
                var random = new Random();
                var randomAction = allActions[random.Next(allActions.Count)];
                
                // Create mock characters for the action
                var attacker = Tests.TestDataBuilders.Character()
                    .WithName("Test Hero")
                    .Build();
                var target = Tests.TestDataBuilders.Enemy()
                    .WithName("Test Enemy")
                    .Build();
                
                // Generate random roll values
                int roll = random.Next(1, 21); // 1-20
                int rollBonus = random.Next(-3, 4); // -3 to +3
                int rawDamage = random.Next(10, 51); // 10-50
                int targetDefense = random.Next(0, 21); // 0-20
                double actualSpeed = random.NextDouble() * 2.0 + 0.5; // 0.5-2.5
                double? comboAmplifier = randomAction.IsComboAction ? (double?)random.NextDouble() * 0.5 + 1.0 : null; // 1.0-1.5 if combo
                
                // Generate action text
                var actionText = GenerateActionText(attacker, target, randomAction, rawDamage);
                
                // Generate roll info
                var rollInfo = Combat.Formatting.RollInfoFormatter.FormatRollInfoColored(
                    roll, rollBonus, rawDamage, targetDefense, actualSpeed, comboAmplifier, randomAction);
                
                // Display the action block directly in the center panel
                // Bypass menu state filter by directly adding to buffer and forcing render
                if (canvasUI != null)
                {
                    var textManager = canvasUI.GetTextManager();
                    if (textManager is CanvasTextManager canvasTextManager)
                    {
                        var displayManager = canvasTextManager.DisplayManager;
                        
                        // Add action text and roll info directly to buffer (bypassing menu state filter)
                        displayManager.Buffer.Add(actionText);
                        displayManager.Buffer.Add(rollInfo);
                        displayManager.Buffer.Add(new List<ColoredText>()); // Blank line after action block
                        
                        // Force immediate render to show the action block
                        displayManager.ForceRender();
                        
                        showStatusMessage?.Invoke("Generated random action block", true);
                    }
                    else
                    {
                        showStatusMessage?.Invoke("Canvas text manager not available", false);
                    }
                }
                else
                {
                    showStatusMessage?.Invoke("Canvas UI not available", false);
                }
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"SettingsActionTestGenerator: Error generating random action block: {ex.Message}\n{ex.StackTrace}");
                showStatusMessage?.Invoke($"Error: {ex.Message}", false);
            }
        }
        
        /// <summary>
        /// Generates and displays random actions (character, environment, or status effect) X number of times
        /// </summary>
        public void GenerateAndDisplayActionTests(int count)
        {
            try
            {
                if (canvasUI == null)
                {
                    showStatusMessage?.Invoke("Canvas UI not available", false);
                    return;
                }
                
                var textManager = canvasUI.GetTextManager();
                if (textManager is CanvasTextManager canvasTextManager)
                {
                    var displayManager = canvasTextManager.DisplayManager;
                    var random = new Random();
                    
                    // Create mock characters
                    var attacker = Tests.TestDataBuilders.Character()
                        .WithName("Test Hero")
                        .Build();
                    var target = Tests.TestDataBuilders.Enemy()
                        .WithName("Test Enemy")
                        .Build();
                    
                    // Add header
                    var headerBuilder = new ColoredTextBuilder();
                    headerBuilder.Add("=== ACTION TEST GENERATION ===", ColorPalette.Warning);
                    displayManager.Buffer.Add(headerBuilder.Build());
                    displayManager.Buffer.Add(new List<ColoredText>()); // Blank line
                    
                    // Generate actions - randomly select one of three types per iteration
                    for (int i = 0; i < count; i++)
                    {
                        // Randomly select action type: 0 = Character, 1 = Environment, 2 = Status Effect
                        int actionType = random.Next(3);
                        Action? selectedAction = null;
                        string actionTypeLabel = "";
                        var labelColor = ColorPalette.Info;
                        
                        switch (actionType)
                        {
                            case 0: // Character Action
                                selectedAction = Settings.ActionSelector.GetRandomCharacterAction();
                                actionTypeLabel = "Character Action";
                                labelColor = ColorPalette.Success;
                                break;
                            case 1: // Environment Action
                                selectedAction = Settings.ActionSelector.GetRandomEnvironmentAction();
                                actionTypeLabel = "Environment Action";
                                labelColor = ColorPalette.Info;
                                break;
                            case 2: // Status Effect Action
                                selectedAction = Settings.ActionSelector.GetRandomStatusEffectAction();
                                actionTypeLabel = "Status Effect Action";
                                labelColor = ColorPalette.Warning;
                                break;
                        }
                        
                        if (selectedAction != null)
                        {
                            int damage = random.Next(10, 51);
                            var actionText = GenerateActionText(attacker, target, selectedAction, damage);
                            
                            // Add label
                            var labelBuilder = new ColoredTextBuilder();
                            labelBuilder.Add($"[{i + 1}] {actionTypeLabel}: ", labelColor);
                            displayManager.Buffer.Add(labelBuilder.Build());
                            
                            displayManager.Buffer.Add(actionText);
                            
                            // Add description if available (especially for environment actions)
                            if (!string.IsNullOrEmpty(selectedAction.Description))
                            {
                                var descBuilder = new ColoredTextBuilder();
                                descBuilder.Add("     Description: ", ColorPalette.White);
                                descBuilder.Add(selectedAction.Description, ColorPalette.Success);
                                displayManager.Buffer.Add(descBuilder.Build());
                            }
                            
                            // List status effects if this is a status effect action
                            if (actionType == 2)
                            {
                                var effectsList = new List<string>();
                                if (selectedAction.CausesBleed) effectsList.Add("Bleed");
                                if (selectedAction.CausesWeaken) effectsList.Add("Weaken");
                                if (selectedAction.CausesSlow) effectsList.Add("Slow");
                                if (selectedAction.CausesPoison) effectsList.Add("Poison");
                                if (selectedAction.CausesBurn) effectsList.Add("Burn");
                                if (selectedAction.CausesStun) effectsList.Add("Stun");
                                if (selectedAction.CausesVulnerability) effectsList.Add("Vulnerability");
                                if (selectedAction.CausesHarden) effectsList.Add("Harden");
                                if (selectedAction.CausesFortify) effectsList.Add("Fortify");
                                if (selectedAction.CausesFocus) effectsList.Add("Focus");
                                if (selectedAction.CausesExpose) effectsList.Add("Expose");
                                if (selectedAction.CausesHPRegen) effectsList.Add("HP Regen");
                                if (selectedAction.CausesArmorBreak) effectsList.Add("Armor Break");
                                if (selectedAction.CausesPierce) effectsList.Add("Pierce");
                                if (selectedAction.CausesReflect) effectsList.Add("Reflect");
                                if (selectedAction.CausesSilence) effectsList.Add("Silence");
                                if (selectedAction.CausesStatDrain) effectsList.Add("Stat Drain");
                                if (selectedAction.CausesAbsorb) effectsList.Add("Absorb");
                                if (selectedAction.CausesTemporaryHP) effectsList.Add("Temporary HP");
                                if (selectedAction.CausesConfusion) effectsList.Add("Confusion");
                                if (selectedAction.CausesCleanse) effectsList.Add("Cleanse");
                                if (selectedAction.CausesMark) effectsList.Add("Mark");
                                if (selectedAction.CausesDisrupt) effectsList.Add("Disrupt");
                                
                                if (effectsList.Count > 0)
                                {
                                    var effectsBuilder = new ColoredTextBuilder();
                                    effectsBuilder.Add("     Status Effects: ", ColorPalette.White);
                                    effectsBuilder.Add(string.Join(", ", effectsList), ColorPalette.Warning);
                                    displayManager.Buffer.Add(effectsBuilder.Build());
                                }
                            }
                            
                            displayManager.Buffer.Add(new List<ColoredText>()); // Blank line
                        }
                        else
                        {
                            // If action couldn't be generated, show a message
                            var errorBuilder = new ColoredTextBuilder();
                            errorBuilder.Add($"[{i + 1}] Failed to generate {actionTypeLabel}", ColorPalette.Error);
                            displayManager.Buffer.Add(errorBuilder.Build());
                            displayManager.Buffer.Add(new List<ColoredText>()); // Blank line
                        }
                        
                        // Add separator between iterations
                        if (i < count - 1)
                        {
                            displayManager.Buffer.Add(new List<ColoredText>()); // Blank line
                        }
                    }
                    
                    // Add footer
                    displayManager.Buffer.Add(new List<ColoredText>()); // Blank line
                    var footerBuilder = new ColoredTextBuilder();
                    footerBuilder.Add($"=== Generated {count} test(s) ===", ColorPalette.Success);
                    displayManager.Buffer.Add(footerBuilder.Build());
                    displayManager.Buffer.Add(new List<ColoredText>()); // Blank line
                    
                    // Force immediate render
                    displayManager.ForceRender();
                    
                    showStatusMessage?.Invoke($"Generated {count} action test(s)", true);
                }
                else
                {
                    showStatusMessage?.Invoke("Canvas text manager not available", false);
                }
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"SettingsActionTestGenerator: Error generating action tests: {ex.Message}\n{ex.StackTrace}");
                showStatusMessage?.Invoke($"Error: {ex.Message}", false);
            }
        }
        
        /// <summary>
        /// Generates action text for a combat action
        /// </summary>
        private List<ColoredText> GenerateActionText(Actor attacker, Actor target, Action action, int damage)
        {
            var builder = new ColoredTextBuilder();
            
            // Attacker name with appropriate color
            builder.Add(attacker.Name, EntityColorHelper.GetActorColor(attacker));
            
            // Action verb and name
            string actionName = action.Name;
            bool isComboAction = action.IsComboAction;
            
            // Determine if it's a critical hit (random for demo)
            var random = new Random();
            bool isCritical = random.Next(100) < 10; // 10% chance
            
            if (isCritical)
            {
                actionName = $"CRITICAL {actionName}";
            }
            
            // Use appropriate color for action
            var actionColor = isComboAction ? ColorPalette.Green : ColorPalette.White;
            if (isCritical)
            {
                actionColor = ColorPalette.Warning;
            }
            
            builder.AddSpace();
            builder.Add("hits", ColorPalette.Success);
            builder.AddSpace();
            builder.Add(target.Name, EntityColorHelper.GetActorColor(target));
            builder.AddSpace();
            builder.Add("with", ColorPalette.White);
            builder.AddSpace();
            builder.Add(actionName, actionColor);
            builder.AddSpace();
            builder.Add("for", ColorPalette.White);
            builder.AddSpace();
            builder.Add(damage.ToString(), ColorPalette.Error);
            builder.AddSpace();
            builder.Add("damage", ColorPalette.White);
            
            return builder.Build();
        }
        
    }
}

