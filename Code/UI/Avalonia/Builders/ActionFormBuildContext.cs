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
        private readonly List<System.Action> _cadenceFlushActions = new List<System.Action>();

        public ActionFormBuildContext(Dictionary<string, Control> actionFormControls, ActionFormControlFactory factory)
        {
            ActionFormControls = actionFormControls;
            Factory = factory;
        }

        public void NotifyCadenceBlocksChanged() => OnCadenceBlocksChanged?.Invoke();

        public void ClearCadenceFlushActions() => _cadenceFlushActions.Clear();

        public void RegisterCadenceFlush(System.Action flush) => _cadenceFlushActions.Add(flush);

        /// <summary>Reads pending ComboBox/TextBox values into cadence block models (call before save).</summary>
        public void FlushCadenceEditorState()
        {
            foreach (var flush in _cadenceFlushActions)
                flush();
        }
    }
}
