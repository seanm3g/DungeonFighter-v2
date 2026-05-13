using System;
using System.Linq;
using System.Threading.Tasks;
using RPGGame.UI.Avalonia;

namespace RPGGame
{
    /// <summary>
    /// Handles region travel selection and route generation.
    /// </summary>
    public class RegionTravelHandler
    {
        private readonly GameStateManager stateManager;
        private readonly IUIManager? customUIManager;
        private readonly TravelRegionCatalog regionCatalog;
        private readonly TravelRouteGenerator routeGenerator;
        private readonly Func<int, Task> delayAsync;
        private TravelRouteResult? lastRouteResult;
        private bool isTravelInProgress;

        public delegate void OnShowGameLoop();
        public delegate void OnShowMessage(string message);

        public event OnShowGameLoop? ShowGameLoopEvent;
        public event OnShowMessage? ShowMessageEvent;

        public RegionTravelHandler(GameStateManager stateManager, IUIManager? customUIManager)
            : this(stateManager, customUIManager, new TravelRegionCatalog(), new TravelRouteGenerator())
        {
        }

        public RegionTravelHandler(
            GameStateManager stateManager,
            IUIManager? customUIManager,
            TravelRegionCatalog regionCatalog,
            TravelRouteGenerator routeGenerator,
            Func<int, Task>? delayAsync = null)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.customUIManager = customUIManager;
            this.regionCatalog = regionCatalog ?? throw new ArgumentNullException(nameof(regionCatalog));
            this.routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
            this.delayAsync = delayAsync ?? Task.Delay;
        }

        public void ShowRegionTravel()
        {
            if (stateManager.CurrentPlayer == null)
            {
                ShowMessageEvent?.Invoke("No character loaded.");
                return;
            }

            stateManager.TransitionToState(GameState.RegionTravel);
            RenderTravelScreen();
        }

        public async Task HandleMenuInput(string input)
        {
            if (stateManager.CurrentPlayer == null)
                return;

            if (isTravelInProgress)
                return;

            string trimmedInput = (input ?? "").Trim();
            if (trimmedInput == "0")
            {
                lastRouteResult = null;
                stateManager.TransitionToState(GameState.GameLoop);
                ShowGameLoopEvent?.Invoke();
                return;
            }

            if (!int.TryParse(trimmedInput, out int choice))
            {
                ShowMessageEvent?.Invoke("Invalid input. Select a destination or 0 to return.");
                return;
            }

            var destinations = regionCatalog.GetDestinationRegions(stateManager.CurrentPlayer).ToList();
            if (choice < 1 || choice > destinations.Count)
            {
                ShowMessageEvent?.Invoke("Invalid destination. Select one of the listed regions.");
                return;
            }

            var destination = destinations[choice - 1];
            await RunRouteTravelAsync(destination);
        }

        private async Task RunRouteTravelAsync(TravelRegion destination)
        {
            var player = stateManager.CurrentPlayer;
            if (player == null)
                return;

            isTravelInProgress = true;
            try
            {
                lastRouteResult = routeGenerator.CreateRouteResult(player, destination.Id);
                RenderTravelScreen();

                for (int i = 0; i < lastRouteResult.EventCount; i++)
                {
                    var step = routeGenerator.GenerateRouteStep(player, lastRouteResult.ToRegion, i + 1, null, lastRouteResult.JourneyDungeonThemes);
                    lastRouteResult.Steps.Add(step);

                    if (step.LootReceived != null)
                        lastRouteResult.LootFound.Add(step.LootReceived);

                    RenderTravelScreen();

                    if (i < lastRouteResult.EventCount - 1)
                        await ApplyTravelDelayAsync(step.DelayMs);
                }

                routeGenerator.CompleteRoute(player, lastRouteResult);
                RenderTravelScreen();
            }
            finally
            {
                isTravelInProgress = false;
            }
        }

        private Task ApplyTravelDelayAsync(int delayMs)
        {
            int scaledDelayMs = DeveloperModeState.ScaleDelayMs(delayMs);
            if (scaledDelayMs <= 0)
                return Task.CompletedTask;

            return delayAsync(scaledDelayMs);
        }

        private void RenderTravelScreen()
        {
            if (customUIManager is CanvasUICoordinator canvasUI && stateManager.CurrentPlayer != null)
            {
                var destinations = regionCatalog.GetDestinationRegions(stateManager.CurrentPlayer).ToList();
                canvasUI.Clear();
                canvasUI.RenderRegionTravel(stateManager.CurrentPlayer, destinations, lastRouteResult);
            }
        }
    }
}
