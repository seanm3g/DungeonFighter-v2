using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using RPGGame;
using RPGGame.ActionInteractionLab;
using RPGGame.UI.Avalonia.Helpers;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.UI.Avalonia.ActionInteractionLab
{
    /// <summary>
    /// Pop-out window for Action Lab session tools (snapshots, dungeon, d20, step/sim).
    /// Foe types and the action catalog open in <see cref="ActionLabCatalogWindow"/>.
    /// </summary>
    public sealed class ActionLabControlsWindow : Window
    {
        private static ActionLabControlsWindow? _instance;

        private readonly GameCanvasControl _canvas;
        private readonly CanvasInteractionManager _interaction = new();
        private CanvasUICoordinator? _canvasUi;
        private GameCoordinator? _game;

        private ActionLabControlsWindow()
        {
            Title = "Action Lab — tools";
            // Single-column tools: snapshots / dungeon / triggers / turn / d20 / footer.
            // Height must fit Triggers + full Step/Sim/Exit footer (was 44; footer clipped after Triggers).
            _canvas = new GameCanvasControl(isAuxiliaryLayoutCanvas: true, auxiliaryGridWidth: 38, auxiliaryGridHeight: 58);
            _canvas.Focusable = true;
            _canvas.PointerPressed += OnCanvasPointerPressed;
            _canvas.PointerMoved += OnCanvasPointerMoved;
            _canvas.PointerReleased += OnCanvasPointerReleased;
            _canvas.PointerWheelChanged += OnCanvasPointerWheelChanged;

            Background = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            Content = new Border
            {
                Background = Brushes.Transparent,
                Child = _canvas,
            };

            Width = 480;
            Height = 1040;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            CanResize = true;
            MinWidth = 360;
            MinHeight = 640;

            Closed += OnClosed;
            KeyDown += OnKeyDown;
        }

        public static void Open(Window? owner, CanvasUICoordinator canvasUi, GameCoordinator game)
        {
            CloseIfOpen();
            var w = new ActionLabControlsWindow
            {
                _canvasUi = canvasUi,
                _game = game,
            };
            _instance = w;
            var effectiveOwner = WindowOwnerResolver.ResolveUsableOwnerWindow(owner);

            ActionLabCatalogWindow.Open(effectiveOwner ?? owner, canvasUi, game);

            if (effectiveOwner != null)
            {
                w.WindowStartupLocation = WindowStartupLocation.Manual;
                EventHandler? layoutOnce = null;
                layoutOnce = (_, _) =>
                {
                    w.Opened -= layoutOnce!;
                    Dispatcher.UIThread.Post(
                        () =>
                        {
                            try
                            {
                                ActionLabWindowPlacement.ApplyActionLabOpenMultiWindowLayout(
                                    effectiveOwner,
                                    w,
                                    ActionLabCatalogWindow.CurrentInstance);
                            }
                            catch
                            {
                                /* best-effort layout */
                            }
                        },
                        DispatcherPriority.Loaded);
                };
                w.Opened += layoutOnce;
                w.Show(effectiveOwner);
            }
            else
            {
                w.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                w.Show();
            }

            w.RefreshFromSession();
            ActionLabCatalogWindow.RefreshIfOpen();
        }

        /// <summary>Open tools pop-out, if any (dialog owner for lab prompts).</summary>
        public static ActionLabControlsWindow? CurrentInstance => _instance;

        public static void CloseIfOpen()
        {
            ActionLabCatalogWindow.CloseIfOpen();
            if (_instance == null)
                return;
            var w = _instance;
            _instance = null;
            try
            {
                w.Close();
            }
            catch
            {
                /* ignore */
            }
        }

        public static void RefreshIfOpen()
        {
            _instance?.RefreshFromSession();
            ActionLabCatalogWindow.RefreshIfOpen();
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            if (ReferenceEquals(_instance, this))
                _instance = null;
            // Closing tools while ExitActionInteractionLab is already tearing down would double-close.
            // Still exit the lab when the user closes the tools window directly.
            ActionLabCatalogWindow.CloseIfOpen();
            if (_game?.StateManager?.CurrentState == GameState.ActionInteractionLab)
                _game.ExitActionInteractionLab();
        }

        private async void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key != Key.PageUp && e.Key != Key.PageDown)
                return;

            string? input = e.Key == Key.PageUp ? "pageup" : "pagedown";
            string? labToken = ActionLabInputCoordinator.MapPageStepInput(input);
            if (labToken == null)
                return;

            e.Handled = true;
            await ActionLabInputCoordinator.HandleLabControlAsync(labToken, _canvasUi, _game).ConfigureAwait(true);
        }

        private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            try
            {
                e.Pointer.Capture(null);
            }
            catch
            {
                /* ignore */
            }
        }

        private void RefreshFromSession()
        {
            var session = ActionInteractionLabSession.Current;
            if (session == null)
                return;

            _canvas.Clear();
            _interaction.ClearClickableElements();
            ActionLabControlsRenderer.Render(_canvas, _interaction, session);
            _canvas.Refresh();
        }

        private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
        {
            var pt = e.GetCurrentPoint(_canvas);
            var g = ScreenToGrid(pt.Position);
            if (_interaction.SetHoverPosition(g.X, g.Y))
                RefreshFromSession();
        }

        private void OnCanvasPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            var session = ActionInteractionLabSession.Current;
            if (session == null || _canvasUi == null) return;

            var pt = e.GetCurrentPoint(_canvas);
            var g = ScreenToGrid(pt.Position);

            if (!session.IsEncounterSimulationRunning
                && session.LastSimBatchWheelGridY >= 0
                && g.Y == session.LastSimBatchWheelGridY
                && g.X >= session.LastSimBatchWheelMinGridX
                && g.X <= session.LastSimBatchWheelMaxGridX)
            {
                ActionLabInputCoordinator.ApplyEncounterSimulationBatchWheel(session, e.Delta.Y, _canvasUi);
                e.Handled = true;
            }
        }

        private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var pt = e.GetCurrentPoint(_canvas);
            if (!pt.Properties.IsLeftButtonPressed)
                return;

            var g = ScreenToGrid(pt.Position);
            _interaction.SetHoverPosition(g.X, g.Y);
            var el = _interaction.GetElementAt(g.X, g.Y);
            if (el == null || string.IsNullOrEmpty(el.Value))
                return;

            e.Pointer.Capture(_canvas);
            e.Handled = true;

            _ = ActionLabInputCoordinator.HandleLabControlAsync(el.Value, _canvasUi, _game);
        }

        private (int X, int Y) ScreenToGrid(Point screenPosition)
        {
            double charWidth = _canvas.GetCharWidth();
            double charHeight = _canvas.GetCharHeight();
            if (charWidth <= 0 || charHeight <= 0)
                return (0, 0);
            int gridX = (int)(screenPosition.X / charWidth);
            int gridY = (int)(screenPosition.Y / charHeight);
            return (gridX, gridY);
        }
    }
}
