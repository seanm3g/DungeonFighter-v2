using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Renderers;

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
        /// <summary>Redraw interval while the hero walks the map route.</summary>
        private const int WalkFrameMs = 50;
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

            lastRouteResult = null;
            stateManager.TransitionToState(GameState.RegionTravel);
            RenderTravelScreen();
        }

        /// <summary>Redraws the travel map with the route in progress (hover changes, chrome refresh).</summary>
        public void Refresh() => RenderTravelScreen();

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

            // Numbers match the map pins: every region in level order, including the one the hero is in.
            var regions = RegionTravelRenderer.OrderRegions(regionCatalog.GetAllRegions());
            if (choice < 1 || choice > regions.Count)
            {
                ShowMessageEvent?.Invoke("Invalid destination. Select one of the numbered regions.");
                return;
            }

            var destination = regions[choice - 1];
            if (string.Equals(destination.Id, regionCatalog.GetRegionForCharacter(stateManager.CurrentPlayer).Id, StringComparison.OrdinalIgnoreCase))
            {
                ShowMessageEvent?.Invoke($"You are already in {destination.DisplayName}.");
                return;
            }

            await RunRouteTravelAsync(destination);
        }

        private async Task RunRouteTravelAsync(TravelRegion destination)
        {
            var player = stateManager.CurrentPlayer;
            if (player == null)
                return;

            isTravelInProgress = true;
            // The pointer may leave the window mid-walk; don't keep the clicked pin lit for the whole journey.
            RPGGame.UI.TravelMapHoverState.Clear();
            try
            {
                lastRouteResult = routeGenerator.CreateRouteResult(player, destination.Id);
                RenderTravelScreen();

                int eventCount = Math.Max(1, lastRouteResult.EventCount);
                for (int i = 0; i < lastRouteResult.EventCount; i++)
                {
                    var step = routeGenerator.GenerateRouteStep(player, lastRouteResult.ToRegion, i + 1, null, lastRouteResult.JourneyDungeonThemes);
                    lastRouteResult.Steps.Add(step);

                    if (step.LootReceived != null)
                        lastRouteResult.LootFound.Add(step.LootReceived);

                    // Each event happens at its stretch of the road; the walk to the next one plays out during the pause.
                    lastRouteResult.WalkProgress = (i + 0.5) / eventCount;
                    RenderTravelScreen();

                    if (i < lastRouteResult.EventCount - 1)
                    {
                        int scaledDelayMs = DeveloperModeState.ScaleDelayMs(step.DelayMs);
                        await AnimateWalkAsync(ApplyTravelDelayAsync(step.DelayMs), scaledDelayMs,
                            lastRouteResult.WalkProgress, (i + 1.5) / eventCount);
                    }
                }

                routeGenerator.CompleteRoute(player, lastRouteResult);
                RenderTravelScreen();
            }
            finally
            {
                isTravelInProgress = false;
            }
        }

        /// <summary>
        /// Advances the hero along the map route while <paramref name="pause"/> runs, redrawing every frame.
        /// Without the canvas UI (tests, headless) it just awaits the pause.
        /// </summary>
        private async Task AnimateWalkAsync(Task pause, int durationMs, double fromProgress, double toProgress)
        {
            var route = lastRouteResult;
            if (route == null || durationMs <= 0 || customUIManager is not CanvasUICoordinator)
            {
                await pause;
                if (route != null)
                    route.WalkProgress = toProgress;
                return;
            }

            var clock = Stopwatch.StartNew();
            while (!pause.IsCompleted)
            {
                double t = Math.Min(1.0, clock.ElapsedMilliseconds / (double)durationMs);
                route.WalkProgress = fromProgress + (toProgress - fromProgress) * t;
                RenderTravelScreen();
                await Task.WhenAny(pause, Task.Delay(WalkFrameMs));
            }

            await pause;
            route.WalkProgress = toProgress;
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
