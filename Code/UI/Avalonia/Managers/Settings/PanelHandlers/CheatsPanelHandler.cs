using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Settings;

namespace RPGGame.UI.Avalonia.Managers.Settings.PanelHandlers
{
    /// <summary>
    /// Developer cheats for live playtesting. Levels the active hub hero, guarantees a path Skill Point,
    /// auto-saves to disk (so Main Menu "Load Game" / next session see it), and refreshes the HUD.
    /// </summary>
    public sealed class CheatsPanelHandler : ISettingsPanelHandler
    {
        private readonly Action<string, bool>? showStatusMessage;
        private CanvasUICoordinator? canvasUI;
        private bool wired;
        private bool saveInFlight;
        private TextBlock? statusText;
        private Button? gainLevelButton;

        public CheatsPanelHandler(Action<string, bool>? showStatusMessage = null)
        {
            this.showStatusMessage = showStatusMessage;
        }

        public string PanelType => "Cheats";

        public void SetDependencies(CanvasUICoordinator? canvasUI, GameStateManager? _)
        {
            this.canvasUI = canvasUI ?? ResolveUI();
            RefreshStatus();
        }

        public void WireUp(UserControl panel)
        {
            if (panel is not CheatsSettingsPanel) return;

            canvasUI ??= ResolveUI();

            statusText = panel.FindControl<TextBlock>("CheatCharacterStatusText");
            gainLevelButton = panel.FindControl<Button>("GainLevelButton");

            if (!wired && gainLevelButton != null)
            {
                gainLevelButton.Click += (_, _) => GainLevel();
                wired = true;
            }

            RefreshStatus();
        }

        public void LoadSettings(UserControl panel)
        {
            canvasUI ??= ResolveUI();
            RefreshStatus();
        }

        public void SaveSettings(UserControl panel) { }

        private static CanvasUICoordinator? ResolveUI() =>
            UIManager.GetCustomUIManager() as CanvasUICoordinator;

        /// <summary>Strictly the game hub hero — same instance Inventory / Skill Tree / dungeon use.</summary>
        private Character? GetPlayer()
        {
            var ui = canvasUI ?? ResolveUI();
            canvasUI = ui;
            var game = ui?.GetGame();
            return game?.CurrentPlayer ?? game?.StateManager?.GetActiveCharacter();
        }

        private void RefreshStatus()
        {
            var player = GetPlayer();
            if (statusText == null) return;

            if (player == null)
            {
                statusText.Text = "No character loaded. Load a game / enter the hub first, then open Cheats.";
                if (gainLevelButton != null)
                    gainLevelButton.IsEnabled = false;
                return;
            }

            var path = ResolvePath(player);
            int avail = player.Progression.GetAvailableSkillPoints(path);
            int lifetime = player.Progression.GetClassPoints(path);
            string weapon = player.Equipment.Weapon is WeaponItem w ? w.WeaponType.ToString() : "none";
            statusText.Text =
                $"{player.Name} — Level {player.Level}  |  SP avail {avail} (path {path} pts {lifetime})  |  weapon:{weapon}" +
                (player.Level >= 99 ? "  (max level)" : "");
            if (gainLevelButton != null)
                gainLevelButton.IsEnabled = player.Level < 99 && !saveInFlight;
        }

        private static WeaponType ResolvePath(Character player) =>
            player.Progression.GetPrimaryClassWeaponType()
            ?? (player.Equipment.Weapon as WeaponItem)?.WeaponType
            ?? WeaponType.Mace;

        private void GainLevel()
        {
            canvasUI ??= ResolveUI();
            var player = GetPlayer();
            if (player == null)
            {
                showStatusMessage?.Invoke("No character loaded — Load Game / enter the hub first.", false);
                RefreshStatus();
                return;
            }

            if (player.Level >= 99)
            {
                showStatusMessage?.Invoke("Already at max level (99).", false);
                RefreshStatus();
                return;
            }

            var path = ResolvePath(player);
            int beforeLevel = player.Level;
            int beforePoints = player.Progression.GetClassPoints(path);

            // Full level step (stats/HP). Class points only auto-award when a weapon is equipped.
            player.ApplyActionLabLevelDelta(+1);

            if (player.Level <= beforeLevel)
            {
                showStatusMessage?.Invoke("Could not gain a level.", false);
                RefreshStatus();
                return;
            }

            // Guaranteed +1 SP on the playable path so Skill Tree testing works even without a weapon.
            if (player.Progression.GetClassPoints(path) <= beforePoints)
                player.Progression.AwardClassPoint(path);

            player.XP = 0;

            int avail = player.Progression.GetAvailableSkillPoints(path);
            showStatusMessage?.Invoke(
                $"{player.Name} → Level {player.Level}, {path} SP avail {avail}. Saving…",
                true);

            RefreshStatus();
            RefreshHeroChrome(player);
            _ = PersistPlayerAsync(player);
        }

        private async Task PersistPlayerAsync(Character player)
        {
            if (saveInFlight) return;
            saveInFlight = true;
            RefreshStatus();
            try
            {
                var ui = canvasUI ?? ResolveUI();
                var sm = ui?.GetGame()?.StateManager;
                string? id = sm?.GetCharacterId(player);
                await player.SaveCharacterAsync(id).ConfigureAwait(true);
                showStatusMessage?.Invoke(
                    $"Saved {player.Name} at Level {player.Level}. Main Menu → Load Game will show the new level.",
                    true);
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Level granted but save failed: {ex.Message}", false);
            }
            finally
            {
                saveInFlight = false;
                RefreshStatus();
            }
        }

        private void RefreshHeroChrome(Character player)
        {
            var ui = canvasUI ?? ResolveUI();
            if (ui == null) return;

            void Paint()
            {
                ui.SetCharacter(player);
                ui.RefreshCharacterPanel();
                var game = ui.GetGame();
                var sm = game?.StateManager;
                var inv = sm?.CurrentInventory ?? player.Inventory;

                if (sm?.CurrentState == GameState.GameLoop)
                {
                    ui.Clear();
                    ui.RenderGameMenu(player, inv);
                }
                else if (sm?.CurrentState == GameState.SkillTree)
                {
                    game?.RefreshPersistentChromeAfterStatsToggle();
                }
                else
                {
                    try { game?.RefreshPersistentChromeAfterStatsToggle(); }
                    catch { /* status confirms grant */ }
                    ui.ForceFullLayoutRender();
                }
            }

            if (Dispatcher.UIThread.CheckAccess())
                Paint();
            else
                Dispatcher.UIThread.Post(Paint);
        }
    }
}
