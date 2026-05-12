namespace RPGGame
{
    using System;
    using System.Threading.Tasks;
    using RPGGame.Entity.Services;
    using RPGGame.UI.Avalonia;

    /// <summary>
    /// Pre-weapon offer: enter Training Ground or skip to the path intro before weapon selection.
    /// </summary>
    public class TrainingGroundOfferHandler
    {
        private readonly GameStateManager stateManager;
        private readonly IUIManager? customUIManager;
        private readonly DungeonRunnerManager dungeonRunnerManager;

        public TrainingGroundOfferHandler(
            GameStateManager stateManager,
            IUIManager? customUIManager,
            DungeonRunnerManager dungeonRunnerManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.customUIManager = customUIManager;
            this.dungeonRunnerManager = dungeonRunnerManager ?? throw new ArgumentNullException(nameof(dungeonRunnerManager));
        }

        public delegate void OnShowMessage(string message);

        public event OnShowMessage? ShowMessageEvent;

        /// <summary>
        /// Renders the offer screen and sets <see cref="GameState.TrainingGroundOffer"/>.
        /// </summary>
        public void ShowTrainingGroundOffer()
        {
            if (stateManager.CurrentPlayer == null)
            {
                ShowMessageEvent?.Invoke("No character selected.");
                return;
            }

            var screenCoordinator = new GameScreenCoordinator(stateManager);
            screenCoordinator.ShowTrainingGroundOffer(stateManager.CurrentPlayer);
        }

        /// <summary>
        /// 1 = start Training Ground dungeon, 2 = skip to the path intro.
        /// </summary>
        public async Task HandleMenuInput(string input)
        {
            if (stateManager.CurrentPlayer == null)
            {
                ShowMessageEvent?.Invoke("No character selected.");
                return;
            }

            var trimmed = input?.Trim() ?? "";
            if (trimmed == "1")
            {
                try
                {
                    // No starter weapon: STR-only base damage + actions from defaults/class/fallback rebuild.
                    ActionLoader.ReloadActions();
                    CharacterSerializer.RebuildCharacterActions(stateManager.CurrentPlayer);

                    var dungeon = PreWeaponTrainingFlow.CreateTrainingGroundDungeon();
                    dungeon.Generate();
                    stateManager.SetCurrentDungeon(dungeon);

                    if (customUIManager is CanvasUICoordinator canvas)
                    {
                        canvas.SetCharacter(stateManager.CurrentPlayer);
                        canvas.RestoreDisplayBufferRendering();
                    }

                    await dungeonRunnerManager.RunDungeon();
                }
                catch (Exception ex)
                {
                    ShowMessageEvent?.Invoke($"Could not start Training Ground: {ex.Message}");
                }
            }
            else if (trimmed == "2")
            {
                stateManager.CurrentPlayer.PendingPreWeaponTrainingGround = false;
                var screenCoordinator = new GameScreenCoordinator(stateManager);
                screenCoordinator.ShowPreWeaponPathIntro(stateManager.CurrentPlayer);
            }
            else
            {
                ShowMessageEvent?.Invoke("Invalid choice. Press 1 to enter Training Ground, or 2 to skip.");
            }
        }
    }
}
