using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Editors;
using RPGGame.UI.Avalonia;

namespace RPGGame.GameCore.Editors
{
    /// <summary>
    /// Manages action creation and editing state
    /// Extracted from ActionEditorHandler.cs to reduce file size
    /// </summary>
    public class ActionStateManager
    {
        private readonly GameStateManager stateManager;
        private readonly IUIManager? customUIManager;
        private readonly Action<string> showMessage;
        
        public ActionData? NewAction { get; private set; }
        public int CurrentFormStep { get; private set; }
        public string CurrentFormInput { get; set; } = "";
        public bool IsEditMode { get; private set; }
        public string? OriginalActionName { get; private set; }
        
        private readonly string[] formSteps = {
            "Name",
            "Type (Attack/Heal/Buff/Debuff/Spell/Interact/Move/UseItem)",
            "TargetType (Self/SingleTarget/AreaOfEffect/Environment)",
            "Description",
            "DamageMultiplier (number, default 1.0)",
            "Length (number, default 1.0)",
            "Cooldown (number, default 0)",
            "CausesBleed (true/false, default false)",
            "CausesWeaken (true/false, default false)",
            "CausesSlow (true/false, default false)",
            "CausesPoison (true/false, default false)",
            "CausesBurn (true/false, default false)",
            "CausesStun (true/false, default false)",
            "IsComboAction (true/false, default false)",
            "ComboOrder (number, -1 = not in combo)",
            "ComboBonusAmount (number, default 0)",
            "ComboBonusDuration (number, default 0)",
            "RollBonus (number, default 0)",
            "StatBonus (number, default 0)",
            "StatBonusType (Strength/Agility/Technique/Intelligence, or empty)",
            "StatBonusDuration (number, -1 = permanent, default 0)",
            "MultiHitCount (number, default 1)",
            "SelfDamagePercent (number, default 0)",
            "SkipNextTurn (true/false, default false)",
            "RepeatLastAction (true/false, default false)",
            "EnemyRollPenalty (number, default 0)",
            "HealthThreshold (number 0.0-1.0, default 0.0)",
            "ConditionalDamageMultiplier (number, default 1.0)",
            "Tags (comma-separated list, e.g. 'sword,melee,physical')"
        };
        
        public string[] FormSteps => formSteps;

        public ActionStateManager(GameStateManager stateManager, IUIManager? customUIManager, Action<string> showMessage)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.customUIManager = customUIManager;
            this.showMessage = showMessage ?? throw new ArgumentNullException(nameof(showMessage));
        }

        public void StartCreateAction()
        {
            IsEditMode = false;
            OriginalActionName = null;
            NewAction = CreateDefaultAction();
            CurrentFormStep = 0;
            CurrentFormInput = "";
            stateManager.TransitionToState(GameState.CreateAction);
            
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderCreateActionForm(NewAction, CurrentFormStep, formSteps, CurrentFormInput, IsEditMode);
                canvasUI.FocusCanvas();
            }
        }

        public void StartEditAction(ActionData action)
        {
            IsEditMode = true;
            OriginalActionName = action.Name;
            NewAction = CreateActionCopy(action);
            CurrentFormStep = 0;
            CurrentFormInput = "";
            stateManager.TransitionToState(GameState.EditAction);
            
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderCreateActionForm(NewAction, CurrentFormStep, formSteps, CurrentFormInput, IsEditMode);
                canvasUI.FocusCanvas();
            }
        }

        public void Reset()
        {
            NewAction = null;
            CurrentFormStep = 0;
            CurrentFormInput = "";
            IsEditMode = false;
            OriginalActionName = null;
        }

        public void MoveToNextStep()
        {
            if (CurrentFormStep < formSteps.Length - 1)
            {
                CurrentFormStep++;
                CurrentFormInput = "";
            }
        }

        public void MoveToPreviousStep()
        {
            if (CurrentFormStep > 0)
            {
                CurrentFormStep--;
                CurrentFormInput = "";
            }
        }

        public void RefreshForm()
        {
            if (customUIManager is CanvasUICoordinator canvasUI && NewAction != null)
            {
                canvasUI.RenderCreateActionForm(NewAction, CurrentFormStep, formSteps, CurrentFormInput, IsEditMode);
            }
        }

        private ActionData CreateDefaultAction()
        {
            return new ActionData
            {
                Name = "",
                Type = "Attack",
                TargetType = "SingleTarget",
                Cooldown = 0,
                Description = "",
                DamageMultiplier = 1.0,
                Length = 1.0,
                Tags = new List<string>(),
                CausesBleed = false,
                CausesWeaken = false,
                CausesSlow = false,
                CausesPoison = false,
                CausesBurn = false,
                IsComboAction = false,
                ComboOrder = -1,
                ComboBonusAmount = 0,
                ComboBonusDuration = 0,
                RollBonus = 0,
                StatBonus = 0,
                StatBonusType = "",
                StatBonusDuration = 0,
                MultiHitCount = 1,
                SelfDamagePercent = 0,
                SkipNextTurn = false,
                RepeatLastAction = false,
                EnemyRollPenalty = 0,
                HealthThreshold = 0.0,
                ConditionalDamageMultiplier = 1.0
            };
        }

        private ActionData CreateActionCopy(ActionData action)
        {
            return new ActionData
            {
                Name = action.Name,
                Type = action.Type,
                TargetType = action.TargetType,
                Cooldown = action.Cooldown,
                Description = action.Description ?? "",
                DamageMultiplier = action.DamageMultiplier,
                Length = action.Length,
                Tags = action.Tags != null ? new List<string>(action.Tags) : new List<string>(),
                CausesBleed = action.CausesBleed,
                CausesWeaken = action.CausesWeaken,
                CausesSlow = action.CausesSlow,
                CausesPoison = action.CausesPoison,
                CausesBurn = action.CausesBurn,
                IsComboAction = action.IsComboAction,
                ComboOrder = action.ComboOrder,
                ComboBonusAmount = action.ComboBonusAmount,
                ComboBonusDuration = action.ComboBonusDuration,
                StatBonus = action.StatBonus,
                StatBonusType = action.StatBonusType,
                StatBonusDuration = action.StatBonusDuration,
                RollBonus = action.RollBonus,
                MultiHitCount = action.MultiHitCount,
                SelfDamagePercent = action.SelfDamagePercent,
                SkipNextTurn = action.SkipNextTurn,
                RepeatLastAction = action.RepeatLastAction,
                EnemyRollPenalty = action.EnemyRollPenalty,
                HealthThreshold = action.HealthThreshold,
                ConditionalDamageMultiplier = action.ConditionalDamageMultiplier
            };
        }
    }
}

