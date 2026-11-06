using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using RPGGame.UI;
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
        /// <summary>
        /// When wired, the world map is opened from dungeon selection: Back returns there, and arriving somewhere
        /// returns there automatically. Unwired (tests, headless) the old game-loop behavior stays.
        /// </summary>
        public event Func<Task>? ShowDungeonSelectionEvent;

        /// <summary>Escape on the world map: closes the travel popup if open, else leaves the map.</summary>
        public const string CancelKey = "escape";

        /// <summary>Pause on the arrival summary before returning to dungeon selection.</summary>
        private const int ArrivalPauseMs = 1600;

        private TravelRegion? pendingDestination;
        private TravelRouteResult? pendingRoute;

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
            ClearPendingTravel();
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

            // Travel popup open: only confirm / cancel.
            if (pendingDestination != null)
            {
                string key = trimmedInput.ToLowerInvariant();
                if (key == TravelConfirmPopupState.ConfirmKey || key == "y" || key == "enter")
                {
                    var confirmed = pendingDestination;
                    var route = pendingRoute;
                    ClearPendingTravel();
                    await RunRouteTravelAsync(confirmed, route);
                    await ReturnToDungeonSelectionAfterArrivalAsync();
                }
                else if (key == TravelConfirmPopupState.CancelKey || key == "n" || key == "0" || key == CancelKey)
                {
                    ClearPendingTravel();
                    RenderTravelScreen();
                }
                return;
            }

            if (trimmedInput == "0" || string.Equals(trimmedInput, CancelKey, StringComparison.OrdinalIgnoreCase))
            {
                lastRouteResult = null;
                await LeaveMapAsync();
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

            // With the canvas UI, ask first (travel time and danger); headless travels straight away.
            if (customUIManager is CanvasUICoordinator)
            {
                OpenTravelPopup(destination);
                return;
            }

            await RunRouteTravelAsync(destination);
        }

        /// <summary>True while the "Travel to X?" popup waits for confirm / cancel.</summary>
        public bool IsTravelPopupOpen => pendingDestination != null;

        private void OpenTravelPopup(TravelRegion destination)
        {
            var player = stateManager.CurrentPlayer;
            if (player == null)
                return;

            // The road is rolled now so the popup's estimate is the journey that will actually play out.
            pendingRoute = routeGenerator.CreateRouteResult(player, destination.Id);
            pendingDestination = destination;
            TravelConfirmPopupState.Current = BuildConfirmView(destination, pendingRoute.EventCount);
            TravelMapHoverState.Clear();
            RenderTravelScreen();
        }

        /// <summary>
        /// Popup numbers: time is road events x the average minutes per stretch (over a d20); danger is the chance at
        /// least one stretch rolls a natural 1 (the only travel events that deal damage).
        /// </summary>
        public static TravelConfirmView BuildConfirmView(TravelRegion destination, int eventCount)
        {
            int n = Math.Max(1, eventCount);
            double avgMinutes = 0;
            for (int roll = 1; roll <= 20; roll++)
                avgMinutes += TravelPacing.GetSummaryTravelMinutesForRoll(roll) / 20.0;
            int danger = (int)Math.Round((1.0 - Math.Pow(19.0 / 20.0, n)) * 100.0);
            return new TravelConfirmView
            {
                DestinationName = destination.DisplayName,
                DestinationLevels = destination.LevelRangeLabel,
                TravelTime = TravelPacing.FormatTravelTime((int)Math.Round(avgMinutes * n)),
                DangerPercent = Math.Clamp(danger, 0, 100),
                RoadEvents = n
            };
        }

        private void ClearPendingTravel()
        {
            pendingDestination = null;
            pendingRoute = null;
            TravelConfirmPopupState.Current = null;
        }

        private async Task LeaveMapAsync()
        {
            ClearPendingTravel();
            if (ShowDungeonSelectionEvent != null)
            {
                await ShowDungeonSelectionEvent.Invoke();
                return;
            }
            stateManager.TransitionToState(GameState.GameLoop);
            ShowGameLoopEvent?.Invoke();
        }

        /// <summary>After arriving, hold the arrival summary briefly, then return to dungeon selection in the new region.</summary>
        private async Task ReturnToDungeonSelectionAfterArrivalAsync()
        {
            if (ShowDungeonSelectionEvent == null || lastRouteResult == null || !lastRouteResult.IsComplete)
                return;
            await delayAsync(DeveloperModeState.ScaleDelayMs(ArrivalPauseMs));
            if (stateManager.CurrentState != GameState.RegionTravel)
                return;
            lastRouteResult = null;
            await ShowDungeonSelectionEvent.Invoke();
        }

        private async Task RunRouteTravelAsync(TravelRegion destination, TravelRouteResult? preRolledRoute = null)
        {
            var player = stateManager.CurrentPlayer;
            if (player == null)
                return;

            isTravelInProgress = true;
            // The pointer may leave the window mid-walk; don't keep the clicked pin lit for the whole journey.
            TravelMapHoverState.Clear();
            try
            {
                lastRouteResult = preRolledRoute ?? routeGenerator.CreateRouteResult(player, destination.Id);
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
