using System;
using System.Collections.Generic;
using Avalonia.Controls;
using RPGGame.Data;

namespace RPGGame.UI.Avalonia.Builders
{
    /// <summary>
    /// Shared context for building action form sections (control registry and factory).
    /// </summary>
    public class ActionFormBuildContext
    {
        public Dictionary<string, Control> ActionFormControls { get; }
        public ActionFormControlFactory Factory { get; }
        public List<CadenceEditorBlock> CadenceBlocks { get; set; } = new List<CadenceEditorBlock>();
        public System.Action? OnCadenceBlocksChanged { get; set; }

        public ActionFormBuildContext(Dictionary<string, Control> actionFormControls, ActionFormControlFactory factory)
        {
            ActionFormControls = actionFormControls;
            Factory = factory;
        }

        public void NotifyCadenceBlocksChanged() => OnCadenceBlocksChanged?.Invoke();
    }
}
