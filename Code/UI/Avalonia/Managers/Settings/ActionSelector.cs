using RPGGame;
using RPGGame.Data;
using RPGGame.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Avalonia.Managers.Settings
{
    /// <summary>
    /// Selects random actions for testing purposes.
    /// Extracted from SettingsActionTestGenerator to reduce size and improve Single Responsibility Principle compliance.
    /// </summary>
    public static class ActionSelector
    {
        /// <summary>
        /// Gets a random environment action
        /// </summary>
        public static Action? GetRandomEnvironmentAction()
        {
            try
            {
                var loader = new EnvironmentalActionLoader();
                var allActions = loader.LoadAllActions();
                
                if (allActions == null || allActions.Count == 0)
                    return null;
                
                var random = new Random();
                var randomData = allActions[random.Next(allActions.Count)];
                
                // Convert to Action
                var actionType = Enum.TryParse<ActionType>(randomData.Type, true, out var parsedType)
                    ? parsedType
                    : ActionType.Debuff;
                
                var action = new Action(
                    name: randomData.Name ?? "Unknown Environment Action",
                    type: actionType,
                    targetType: TargetType.AreaOfEffect,
                    cooldown: 0,
                    description: randomData.Description ?? "",
                    comboOrder: -1,
                    damageMultiplier: randomData.DamageMultiplier,
                    length: randomData.Length,
                    causesBleed: randomData.CausesBleed,
                    causesWeaken: randomData.CausesWeaken,
                    isComboAction: false,
                    comboBonusAmount: 0,
                    comboBonusDuration: 0
                );
                
                action.CausesStun = randomData.CausesStun;
                action.CausesSlow = randomData.CausesSlow;
                action.CausesPoison = randomData.CausesPoison;
                
                return action;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"ActionSelector: Error getting random environment action: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Gets a random character action (any regular action from ActionLoader)
        /// </summary>
        public static Action? GetRandomCharacterAction()
        {
            try
            {
                var allActions = ActionLoader.GetAllActions();
                
                if (allActions == null || allActions.Count == 0)
                    return null;
                
                var random = new Random();
                return allActions[random.Next(allActions.Count)];
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"ActionSelector: Error getting random character action: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Gets a random status effect action (action that causes at least one status effect)
        /// </summary>
        public static Action? GetRandomStatusEffectAction()
        {
            try
            {
                var allActions = ActionLoader.GetAllActions();
                
                if (allActions == null || allActions.Count == 0)
                    return null;
                
                // Filter actions that have at least one status effect
                var statusEffectActions = allActions.Where(action =>
                    action.CausesBleed || action.CausesWeaken || action.CausesSlow ||
                    action.CausesPoison || action.CausesBurn || action.CausesStun ||
                    action.CausesVulnerability || action.CausesHarden || action.CausesFortify ||
                    action.CausesFocus || action.CausesExpose || action.CausesHPRegen ||
                    action.CausesArmorBreak || action.CausesPierce || action.CausesReflect ||
                    action.CausesSilence || action.CausesStatDrain || action.CausesAbsorb ||
                    action.CausesTemporaryHP || action.CausesConfusion || action.CausesCleanse ||
                    action.CausesMark || action.CausesDisrupt
                ).ToList();
                
                if (statusEffectActions.Count == 0)
                    return null;
                
                var random = new Random();
                return statusEffectActions[random.Next(statusEffectActions.Count)];
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"ActionSelector: Error getting random status effect action: {ex.Message}");
                return null;
            }
        }
    }
}
