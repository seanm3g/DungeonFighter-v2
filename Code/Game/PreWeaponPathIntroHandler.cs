using System;
using System.Threading.Tasks;

namespace RPGGame
{
    /// <summary>
    /// Shows the post-Training-Ground path intro before starter weapon selection.
    /// </summary>
    public class PreWeaponPathIntroHandler
    {
        private readonly GameStateManager stateManager;
        private readonly WeaponSelectionHandler weaponSelectionHandler;

        public PreWeaponPathIntroHandler(GameStateManager stateManager, WeaponSelectionHandler weaponSelectionHandler)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.weaponSelectionHandler = weaponSelectionHandler ?? throw new ArgumentNullException(nameof(weaponSelectionHandler));
        }

        public delegate void OnShowMessage(string message);

        public event OnShowMessage? ShowMessageEvent;

        public void ShowPathIntro()
        {
            if (stateManager.CurrentPlayer == null)
            {
                ShowMessageEvent?.Invoke("No character selected.");
                return;
            }

            var screenCoordinator = new GameScreenCoordinator(stateManager);
            screenCoordinator.ShowPreWeaponPathIntro(stateManager.CurrentPlayer);
        }

        public Task HandleMenuInput(string input)
        {
            if (stateManager.CurrentPlayer == null)
            {
                ShowMessageEvent?.Invoke("No character selected.");
                return Task.CompletedTask;
            }

            stateManager.TransitionToState(GameState.WeaponSelection);
            weaponSelectionHandler.ShowWeaponSelection();
            return Task.CompletedTask;
        }
    }
}
