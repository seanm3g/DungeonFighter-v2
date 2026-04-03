using System;
using System.Collections.Generic;
using Avalonia.Controls;

namespace RPGGame.UI.Avalonia.Builders
{
    /// <summary>
    /// Shared context for building action form sections (control registry and factory).
    /// </summary>
    public class ActionFormBuildContext
    {
        public Dictionary<string, Control> ActionFormControls { get; }
        public ActionFormControlFactory Factory { get; }

        public ActionFormBuildContext(Dictionary<string, Control> actionFormControls, ActionFormControlFactory factory)
        {
            ActionFormControls = actionFormControls;
            Factory = factory;
        }
    }
}
