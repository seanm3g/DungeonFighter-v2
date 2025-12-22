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
                1 => ActionFieldProcessors.ProcessType(actionData, input, showMessage),
                2 => ActionFieldProcessors.ProcessTargetType(actionData, input, showMessage),
                3 => ActionFieldProcessors.ProcessDescription(actionData, input, showMessage),
                4 => ActionFieldProcessors.ProcessDoubleField(actionData, input, showMessage, "DamageMultiplier", (a, v) => a.DamageMultiplier = v),
                5 => ActionFieldProcessors.ProcessDoubleField(actionData, input, showMessage, "Length", (a, v) => a.Length = v),
                6 => ActionFieldProcessors.ProcessIntField(actionData, input, showMessage, "Cooldown", (a, v) => a.Cooldown = v),
                7 => ActionFieldProcessors.ProcessBooleanField(actionData, input, showMessage, (a, v) => a.CausesBleed = v),
                8 => ActionFieldProcessors.ProcessBooleanField(actionData, input, showMessage, (a, v) => a.CausesWeaken = v),
                9 => ActionFieldProcessors.ProcessBooleanField(actionData, input, showMessage, (a, v) => a.CausesSlow = v),
                10 => ActionFieldProcessors.ProcessBooleanField(actionData, input, showMessage, (a, v) => a.CausesPoison = v),
                11 => ActionFieldProcessors.ProcessBooleanField(actionData, input, showMessage, (a, v) => a.CausesBurn = v),
                12 => ActionFieldProcessors.ProcessBooleanField(actionData, input, showMessage, (a, v) => { }), // CausesStun not in ActionData
                13 => ActionFieldProcessors.ProcessBooleanField(actionData, input, showMessage, (a, v) => a.IsComboAction = v),
                14 => ActionFieldProcessors.ProcessIntField(actionData, input, showMessage, "ComboOrder", (a, v) => a.ComboOrder = v, true, -1),
                15 => ActionFieldProcessors.ProcessIntField(actionData, input, showMessage, "ComboBonusAmount", (a, v) => a.ComboBonusAmount = v, true),
                16 => ActionFieldProcessors.ProcessIntField(actionData, input, showMessage, "ComboBonusDuration", (a, v) => a.ComboBonusDuration = v, true),
                17 => ActionFieldProcessors.ProcessIntField(actionData, input, showMessage, "RollBonus", (a, v) => a.RollBonus = v, true),
                18 => ActionFieldProcessors.ProcessIntField(actionData, input, showMessage, "StatBonus", (a, v) => a.StatBonus = v, true),
                19 => ActionFieldProcessors.ProcessStatBonusType(actionData, input, showMessage),
                20 => ActionFieldProcessors.ProcessIntField(actionData, input, showMessage, "StatBonusDuration", (a, v) => a.StatBonusDuration = v, true),
                21 => ActionFieldProcessors.ProcessIntField(actionData, input, showMessage, "MultiHitCount", (a, v) => a.MultiHitCount = v, true, 1),
                22 => ActionFieldProcessors.ProcessIntField(actionData, input, showMessage, "SelfDamagePercent", (a, v) => a.SelfDamagePercent = v, true),
                23 => ActionFieldProcessors.ProcessBooleanField(actionData, input, showMessage, (a, v) => a.SkipNextTurn = v),
                24 => ActionFieldProcessors.ProcessBooleanField(actionData, input, showMessage, (a, v) => a.RepeatLastAction = v),
                25 => ActionFieldProcessors.ProcessIntField(actionData, input, showMessage, "EnemyRollPenalty", (a, v) => a.EnemyRollPenalty = v, true),
                26 => ActionFieldProcessors.ProcessHealthThreshold(actionData, input, showMessage),
                27 => ActionFieldProcessors.ProcessDoubleField(actionData, input, showMessage, "ConditionalDamageMultiplier", (a, v) => a.ConditionalDamageMultiplier = v, true, 1.0),
                28 => ActionFieldProcessors.ProcessTags(actionData, input, showMessage),
                _ => false
            };
        }
    }
}

