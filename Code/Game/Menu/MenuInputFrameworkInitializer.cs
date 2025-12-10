using System;
using RPGGame;
using DungeonFighter.Game.Menu.Routing;
using DungeonFighter.Game.Menu.Core;

namespace RPGGame.Menu
{
    /// <summary>
    /// Initializes the Menu Input Framework
    /// </summary>
    public static class MenuInputFrameworkInitializer
    {
        /// <summary>
        /// Result containing initialized menu input framework components
        /// </summary>
        public class MenuInputFrameworkResult
        {
            public MenuInputRouter? MenuInputRouter { get; set; }
            public MenuInputValidator? MenuInputValidator { get; set; }
        }

        /// <summary>
        /// Initializes the Menu Input Framework
        /// </summary>
        public static MenuInputFrameworkResult Initialize()
        {
            try
            {
                // Create validator and router
                var validator = new MenuInputValidator();
                var router = new MenuInputRouter(validator);
                
                // Register validation rules for each menu
                validator.RegisterRules(GameState.MainMenu, new MainMenuValidationRules());
                validator.RegisterRules(GameState.CharacterCreation, new CharacterCreationValidationRules());
                validator.RegisterRules(GameState.WeaponSelection, new WeaponSelectionValidationRules(4));
                validator.RegisterRules(GameState.Inventory, new InventoryValidationRules());
                validator.RegisterRules(GameState.Settings, new SettingsValidationRules());
                validator.RegisterRules(GameState.DungeonSelection, new DungeonSelectionValidationRules(10));
                return new MenuInputFrameworkResult
                {
                    MenuInputRouter = router,
                    MenuInputValidator = validator
                };
            }
            catch (Exception ex)
            {
                return new MenuInputFrameworkResult();
            }
        }
    }
}

