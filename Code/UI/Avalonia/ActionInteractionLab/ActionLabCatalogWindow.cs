using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using RPGGame.ActionInteractionLab;
using RPGGame.UI.Avalonia.Helpers;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.UI.Avalonia.ActionInteractionLab
{
    /// <summary>
    /// Pop-out for Action Lab foe type picker and action catalog.
    /// Opened alongside <see cref="ActionLabControlsWindow"/>; closing this alone does not exit the lab.
    /// </summary>
    public sealed class ActionLabCatalogWindow : Window
    {
        private static ActionLabCatalogWindow? _instance;

        private readonly GameCanvasControl _canvas;
        private readonly CanvasInteractionManager _interaction = new();
        private CanvasUICoordinator? _canvasUi;
        private GameCoordinator? _game; // kept for same HandleLabControlAsync signature as tools window

        private ActionLabCatalogWindow()
        {
            Title = "Action Lab — foes & actions";
            _canvas = new GameCanvasControl(isAuxiliaryLayoutCanvas: true, auxiliaryGridWidth: 72, auxiliaryGridHeight: 48);
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

            Width = 720;
            Height = 880;
            WindowStartupLocation = WindowStartupLocation.Manual;
            CanResize = true;
            MinWidth = 480;
            MinHeight = 480;

            Closed += OnClosed;
        }

        public static void Open(Window? owner, CanvasUICoordinator canvasUi, GameCoordinator game)
        {
            CloseIfOpen();
            var w = new ActionLabCatalogWindow
            {
                _canvasUi = canvasUi,
                _game = game,
            };
            _instance = w;
            var effectiveOwner = WindowOwnerResolver.ResolveUsableOwnerWindow(owner);
            if (effectiveOwner != null)
                w.Show(effectiveOwner);
            else
                w.Show();
            w.RefreshFromSession();
        }

        public static void CloseIfOpen()
        {
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
        }

        public static Window? CurrentInstance => _instance;

        private void OnClosed(object? sender, EventArgs e)
        {
            if (ReferenceEquals(_instance, this))
                _instance = null;
            // Clearing catalog wheel hit boxes so the tools window cannot mis-route wheel events.
            var session = ActionInteractionLabSession.Current;
            if (session != null)
            {
                session.LastCatalogWheelMinGridX = -1;
                session.LastCatalogWheelMaxGridX = -1;
                session.LastCatalogWheelMinGridY = -1;
                session.LastCatalogWheelMaxGridY = -1;
                session.LastEnemyCatalogWheelMinGridX = -1;
                session.LastEnemyCatalogWheelMaxGridX = -1;
                session.LastEnemyCatalogWheelMinGridY = -1;
                session.LastEnemyCatalogWheelMaxGridY = -1;
            }
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
            ActionLabCatalogRenderer.Render(_canvas, _interaction, session);
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

            var delta = e.Delta.Y;
            if (Math.Abs(delta) < 0.1) return;

            int steps = Math.Max(1, (int)(Math.Abs(delta) / 50.0));
            if (steps > 30) steps = 30;
            int signed = delta > 0 ? -steps : steps;

            if (session.LastEnemyCatalogWheelMinGridY >= 0 && session.LastEnemyCatalogWheelMinGridX >= 0
                && g.X >= session.LastEnemyCatalogWheelMinGridX && g.X <= session.LastEnemyCatalogWheelMaxGridX
                && g.Y >= session.LastEnemyCatalogWheelMinGridY && g.Y <= session.LastEnemyCatalogWheelMaxGridY)
            {
                ActionLabInputCoordinator.ApplyEnemyCatalogScrollOffsetDelta(session, signed, _canvasUi);
                e.Handled = true;
                return;
            }

            if (session.LastCatalogWheelMinGridY < 0 || session.LastCatalogWheelMinGridX < 0) return;
            if (g.X < session.LastCatalogWheelMinGridX || g.X > session.LastCatalogWheelMaxGridX) return;
            if (g.Y < session.LastCatalogWheelMinGridY || g.Y > session.LastCatalogWheelMaxGridY) return;

            ActionLabInputCoordinator.ApplyCatalogScrollOffsetDelta(session, signed, _canvasUi);
            e.Handled = true;
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
