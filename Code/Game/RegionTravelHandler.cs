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
        private TravelRouteResult? lastRouteResult;

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
            TravelRouteGenerator routeGenerator)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.customUIManager = customUIManager;
            this.regionCatalog = regionCatalog ?? throw new ArgumentNullException(nameof(regionCatalog));
            this.routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
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
            lastRouteResult = routeGenerator.GenerateRoute(stateManager.CurrentPlayer, destination.Id);
            RenderTravelScreen();
            await Task.CompletedTask;
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
