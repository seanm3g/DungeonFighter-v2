using System;
using RPGGame;
using RPGGame.GameCore.Editors.Processors;

namespace RPGGame.GameCore.Editors
{
    /// <summary>
    /// Handles action form processing and validation
    /// Extracted from ActionEditorHandler.cs ProcessFormStep() method
    /// </summary>
    public class ActionFormProcessor
    {
        private readonly Action<string> showMessage;

        public ActionFormProcessor(Action<string> showMessage)
        {
            this.showMessage = showMessage ?? throw new ArgumentNullException(nameof(showMessage));
        }

        /// <summary>
        /// Process input for the current form step
        /// </summary>
        public bool ProcessFormStep(ActionData actionData, int currentFormStep, string input)
        {
            if (actionData == null) return false;

            return currentFormStep switch
            {
                0 => ActionFieldProcessors.ProcessName(actionData, input, showMessage),
                1 => ActionFieldProcessors.ProcessDescription(actionData, input, showMessage),
                2 => ActionFieldProcessors.ProcessDoubleField(actionData, input, showMessage, "DamageMultiplier", (a, v) => a.DamageMultiplier = v),
                3 => ActionFieldProcessors.ProcessDoubleField(actionData, input, showMessage, "Length", (a, v) => a.Length = v),
                4 => ActionFieldProcessors.ProcessIntField(actionData, input, showMessage, "Cooldown", (a, v) => a.Cooldown = v),
                5 => ActionFieldProcessors.ProcessBooleanField(actionData, input, showMessage, (a, v) => a.CausesBleed = v),
                6 => ActionFieldProcessors.ProcessBooleanField(actionData, input, showMessage, (a, v) => a.CausesWeaken = v),
                7 => ActionFieldProcessors.ProcessBooleanField(actionData, input, showMessage, (a, v) => a.CausesSlow = v),
                8 => ActionFieldProcessors.ProcessBooleanField(actionData, input, showMessage, (a, v) => a.CausesPoison = v),
                9 => ActionFieldProcessors.ProcessBooleanField(actionData, input, showMessage, (a, v) => a.CausesBurn = v),
                10 => ActionFieldProcessors.ProcessBooleanField(actionData, input, showMessage, (a, v) => { }), // CausesStun not in ActionData
                11 => ActionFieldProcessors.ProcessBooleanField(actionData, input, showMessage, (a, v) => a.IsComboAction = v),
                12 => ActionFieldProcessors.ProcessIntField(actionData, input, showMessage, "ComboOrder", (a, v) => a.ComboOrder = v, true, -1),
                13 => ActionFieldProcessors.ProcessIntField(actionData, input, showMessage, "ComboBonusDuration", (a, v) => a.ComboBonusDuration = v, true),
                14 => ActionFieldProcessors.ProcessBooleanField(actionData, input, showMessage, (a, v) => a.SkipNextTurn = v),
                15 => ActionFieldProcessors.ProcessIntField(actionData, input, showMessage, "CriticalMissThresholdAdjustment", (a, v) => a.CriticalMissThresholdAdjustment = v, true),
                16 => ActionFieldProcessors.ProcessIntField(actionData, input, showMessage, "HitThresholdAdjustment", (a, v) => a.HitThresholdAdjustment = v, true),
                17 => ActionFieldProcessors.ProcessIntField(actionData, input, showMessage, "ComboThresholdAdjustment", (a, v) => a.ComboThresholdAdjustment = v, true),
                18 => ActionFieldProcessors.ProcessIntField(actionData, input, showMessage, "CriticalHitThresholdAdjustment", (a, v) => a.CriticalHitThresholdAdjustment = v, true),
                19 => ActionFieldProcessors.ProcessIntField(actionData, input, showMessage, "StatBonus", (a, v) => a.StatBonus = v, true),
                20 => ActionFieldProcessors.ProcessStatBonusType(actionData, input, showMessage),
                21 => ActionFieldProcessors.ProcessIntField(actionData, input, showMessage, "MultiHitCount", (a, v) => a.MultiHitCount = v, true, 1),
                22 => ActionFieldProcessors.ProcessBooleanField(actionData, input, showMessage, (a, v) => a.RepeatLastAction = v),
                23 => ActionFieldProcessors.ProcessHealthThreshold(actionData, input, showMessage),
                24 => ActionFieldProcessors.ProcessTags(actionData, input, showMessage),
                _ => false
            };
        }
    }
}

