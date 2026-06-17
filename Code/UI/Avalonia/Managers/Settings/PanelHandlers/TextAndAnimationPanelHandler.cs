using Avalonia.Controls;
using RPGGame.UI.Avalonia.Settings;

namespace RPGGame.UI.Avalonia.Managers.Settings.PanelHandlers
{
    /// <summary>Delegates wire/load/save to Text Delays and Text Animation child panels.</summary>
    public sealed class TextAndAnimationPanelHandler : ISettingsPanelHandler
    {
        private readonly TextDelaysPanelHandler delaysHandler;
        private readonly TextAnimationPresetsPanelHandler animationHandler;

        public TextAndAnimationPanelHandler(
            TextDelaysPanelHandler delaysHandler,
            TextAnimationPresetsPanelHandler animationHandler)
        {
            this.delaysHandler = delaysHandler;
            this.animationHandler = animationHandler;
        }

        public string PanelType => "TextAndAnimation";

        public void WireUp(UserControl panel)
        {
            if (panel is not TextAndAnimationSettingsPanel container)
                return;
            delaysHandler.WireUp(container.DelaysPanel);
            animationHandler.WireUp(container.AnimationPanel);
        }

        public void LoadSettings(UserControl panel)
        {
            if (panel is not TextAndAnimationSettingsPanel container)
                return;
            delaysHandler.LoadSettings(container.DelaysPanel);
            animationHandler.LoadSettings(container.AnimationPanel);
        }

        public void SaveSettings(UserControl panel)
        {
            if (panel is not TextAndAnimationSettingsPanel container)
                return;
            delaysHandler.SaveSettings(container.DelaysPanel);
            animationHandler.SaveSettings(container.AnimationPanel);
        }
    }
}
