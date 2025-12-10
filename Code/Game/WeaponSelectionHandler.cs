namespace RPGGame
{
    using System;
    using System.Collections.Generic;
    using RPGGame.UI.Avalonia;

    /// <summary>
    /// Handles weapon selection menu for new characters.
    /// Extracted from Game.cs to manage weapon selection flow.
    /// 
    /// Responsibilities:
    /// - Display available weapon options
    /// - Handle weapon selection input
    /// - Initialize character with selected weapon
    /// - Transition to character creation or game loop
    /// </summary>
    public class WeaponSelectionHandler
    {
        private GameStateManager stateManager;
        private GameInitializationManager initializationManager;
        private IUIManager? customUIManager;
        private GameInitializer gameInitializer;
        private List<StartingWeapon>? availableWeapons;
        
        // Delegates
        public delegate void OnShowCharacterCreation();
        public delegate void OnShowMessage(string message);
        
        public event OnShowCharacterCreation? ShowCharacterCreationEvent;
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
        /// Display weapon selection screen
        /// </summary>
        public void ShowWeaponSelection()
        {
            DebugLogger.Log("WeaponSelectionHandler", $"customUIManager type: {customUIManager?.GetType().Name ?? "null"}");
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                if (stateManager.CurrentPlayer != null)
                {
                    // Load weapons from starting gear
                    availableWeapons = LoadStartingWeapons();
                    // Display the weapon selection screen
                    try
                    {
                        canvasUI.RenderWeaponSelection(availableWeapons);
                    }
                    catch (Exception ex)
                    {
                    }
                }
                else
                {
                }
            }
            else
            {
            }
            
            stateManager.TransitionToState(GameState.WeaponSelection);
        }

        /// <summary>
        /// Load starting weapons from game data
        /// </summary>
        private List<StartingWeapon> LoadStartingWeapons()
        {
            try
            {
                var startingGear = gameInitializer.LoadStartingGear();
                return startingGear.weapons ?? new List<StartingWeapon>();
            }
            catch (Exception ex)
            {
                // Return default weapons if loading fails
                return new List<StartingWeapon>
                {
                    new StartingWeapon { name = "Mace", damage = 7.5, attackSpeed = 0.8 },
                    new StartingWeapon { name = "Sword", damage = 6.0, attackSpeed = 1.0 },
                    new StartingWeapon { name = "Dagger", damage = 4.3, attackSpeed = 1.4 },
                    new StartingWeapon { name = "Wand", damage = 5.5, attackSpeed = 1.1 }
                };
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
            // Validate weapon choice (1-4 based on StartingGear.json)
            if (int.TryParse(input?.Trim() ?? "", out int weaponChoice) && weaponChoice >= 1 && weaponChoice <= 4)
            {
                // Initialize character with weapon choice
                initializationManager.InitializeNewCharacter(stateManager.CurrentPlayer, weaponChoice);
                
                // Ensure character is set in UI coordinator for persistent display
                if (customUIManager is CanvasUICoordinator canvasUI)
                {
                    canvasUI.SetCharacter(stateManager.CurrentPlayer);
                }
                
                ShowMessageEvent?.Invoke($"You selected weapon {weaponChoice}.");
                
                // Move to character creation (class selection, etc.)
                stateManager.TransitionToState(GameState.CharacterCreation);
                ShowCharacterCreationEvent?.Invoke();
            }
            else
            {
                ShowMessageEvent?.Invoke("Invalid choice. Please select 1-4.");
            }
        }
    }
}

