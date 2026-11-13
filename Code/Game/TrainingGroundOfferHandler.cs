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

        /// <summary>Input that toggles the "don't show this again" checkbox (<see cref="GameSettings.ShowTrainingGroundOffer"/>).</summary>
        public const string ToggleDontShowKey = "3";

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

            // Turned off (checkbox on this screen or Settings > Gameplay): go straight to the path intro.
            // Only the windowed game skips; headless/MCP runs keep the offer so their scripted flow is unchanged.
            if (!GameSettings.Instance.ShowTrainingGroundOffer && customUIManager is CanvasUICoordinator)
            {
                SkipToPathIntro();
                return;
            }

            var screenCoordinator = new GameScreenCoordinator(stateManager);
            screenCoordinator.ShowTrainingGroundOffer(stateManager.CurrentPlayer);
        }

        private void SkipToPathIntro()
        {
            var player = stateManager.CurrentPlayer;
            if (player == null)
                return;
            player.PendingPreWeaponTrainingGround = false;
            var screenCoordinator = new GameScreenCoordinator(stateManager);
            screenCoordinator.ShowPreWeaponPathIntro(player);
        }

        /// <summary>
        /// 1 = start Training Ground dungeon, 2 = skip to the path intro, 3 = toggle "don't show this again".
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
                SkipToPathIntro();
            }
            else if (trimmed == ToggleDontShowKey)
            {
                var settings = GameSettings.Instance;
                settings.ShowTrainingGroundOffer = !settings.ShowTrainingGroundOffer;
                settings.SaveSettings();
                if (customUIManager is CanvasUICoordinator canvasUI)
                {
                    canvasUI.Clear();
                    canvasUI.RenderTrainingGroundOffer(stateManager.CurrentPlayer);
                }
            }
            else
            {
                ShowMessageEvent?.Invoke("Invalid choice. Press 1 to enter Training Ground, or 2 to skip.");
            }
        }
    }
}
