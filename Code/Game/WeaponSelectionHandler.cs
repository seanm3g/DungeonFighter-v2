namespace RPGGame
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using RPGGame.UI.Avalonia;

    /// <summary>
    /// Handles weapon selection menu for new characters.
    /// Extracted from Game.cs to manage weapon selection flow.
    /// 
    /// Responsibilities:
    /// - Display available weapon options
    /// - Handle weapon selection input
    /// - Initialize character with selected weapon
    /// - Transition to character creation (welcome / confirm), then game loop
    /// </summary>
    public class WeaponSelectionHandler
    {
        private GameStateManager stateManager;
        private GameInitializationManager initializationManager;
        private IUIManager? customUIManager;
        private GameInitializer gameInitializer;
        private List<StartingWeapon>? availableWeapons;
        
        // Delegates
        public delegate void OnShowMessage(string message);
        
        public event OnShowMessage? ShowMessageEvent;

        public WeaponSelectionHandler(
            GameStateManager stateManager,
            GameInitializationManager initializationManager,
            IUIManager? customUIManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.initializationManager = initializationManager ?? throw new ArgumentNullException(nameof(initializationManager));
            this.customUIManager = customUIManager;
            this.gameInitializer = new GameInitializer();
        }

        /// <summary>
        /// Display weapon selection screen.
        /// Uses GameScreenCoordinator for standardized screen transition.
        /// </summary>
        public void ShowWeaponSelection()
        {
            try
            {
                if (stateManager.CurrentPlayer == null)
                {
                    ShowMessageEvent?.Invoke("No character selected. Cannot show weapon selection.");
                    DebugLogger.Log("WeaponSelectionHandler", "CurrentPlayer is null");
                    return;
                }
                
                // Load weapons from starting gear
                availableWeapons = LoadStartingWeapons();
                
                if (availableWeapons == null || availableWeapons.Count == 0)
                {
                    ShowMessageEvent?.Invoke("No weapons available. Cannot show weapon selection.");
                    return;
                }
                
                // Use GameScreenCoordinator for standardized screen transition
                var screenCoordinator = new GameScreenCoordinator(stateManager);
                screenCoordinator.ShowWeaponSelection(availableWeapons);
            }
            catch (Exception ex)
            {
                ShowMessageEvent?.Invoke($"Error showing weapon selection: {ex.Message}");
                DebugLogger.Log("WeaponSelectionHandler", $"Error in ShowWeaponSelection: {ex}");
            }
        }

        /// <summary>
        /// Load starting weapons from game data
        /// </summary>
        private List<StartingWeapon> LoadStartingWeapons()
        {
            try
            {
                _ = GameConfiguration.Instance;
                return GameInitializer.BuildStarterWeaponsForMenu();
            }
            catch (Exception)
            {
                return GameInitializer.BuildStarterWeaponsForMenu();
            }
        }

        /// <summary>
        /// Handle weapon selection input (1-4 for weapons)
        /// </summary>
        public void HandleMenuInput(string input)
        {
            if (stateManager.CurrentPlayer == null)
            {
                ShowMessageEvent?.Invoke("No character selected.");
                return;
            }
            int maxChoice = availableWeapons?.Count ?? 0;
            if (maxChoice == 0)
            {
                try
                {
                    _ = GameConfiguration.Instance;
                    maxChoice = GameInitializer.BuildStarterWeaponsForMenu().Count;
                }
                catch
                {
                    maxChoice = 0;
                }
            }

            if (int.TryParse(input?.Trim() ?? "", out int weaponChoice) && weaponChoice >= 1 && weaponChoice <= maxChoice && maxChoice > 0)
            {
                // Initialize character with weapon choice
                initializationManager.InitializeNewCharacter(stateManager.CurrentPlayer, weaponChoice);
                
                // Ensure character is set in UI coordinator for persistent display
                if (customUIManager is CanvasUICoordinator canvasUI)
                {
                    canvasUI.SetCharacter(stateManager.CurrentPlayer);
                }
                
                ShowMessageEvent?.Invoke($"You selected weapon {weaponChoice}.");
                
                var screenCoordinator = new GameScreenCoordinator(stateManager);
                screenCoordinator.ShowCharacterCreation(stateManager.CurrentPlayer);
            }
            else
            {
                ShowMessageEvent?.Invoke(maxChoice > 0
                    ? $"Invalid choice. Please select 1-{maxChoice}."
                    : "Invalid choice. No starting weapons are configured.");
            }
        }
    }
}

