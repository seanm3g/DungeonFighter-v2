using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGGame;
using DungeonFighter.Game.Menu.Core;

namespace DungeonFighter.Game.Menu.Routing
{
    /// <summary>
    /// Central router for menu input.
    /// Routes input from the main Game.HandleInput method to the appropriate menu handler.
    /// Replaces the scattered switch statement in Game.cs with a centralized, extensible router.
    /// </summary>
    public class MenuInputRouter
    {
        private readonly Dictionary<GameState, IMenuHandler> handlers;
        private readonly IMenuInputValidator validator;

        /// <summary>
        /// Creates a new MenuInputRouter instance.
        /// </summary>
        /// <param name="validator">The input validator for validating inputs</param>
        public MenuInputRouter(IMenuInputValidator validator)
        {
            this.handlers = new Dictionary<GameState, IMenuHandler>();
            this.validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        /// <summary>
        /// Registers a menu handler for a specific game state.
        /// </summary>
        /// <param name="handler">The handler to register</param>
        public void RegisterHandler(IMenuHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            handlers[handler.TargetState] = handler;
        }

        /// <summary>
        /// Routes input to the appropriate handler based on current game state.
        /// </summary>
        /// <param name="input">The user input</param>
        /// <param name="currentState">The current game state</param>
        /// <returns>The result of input processing</returns>
        public async Task<MenuInputResult> RouteInput(string input, GameState currentState)
        {
            try
            {
                // 1. Validate input
                var validationResult = validator.Validate(input, currentState);
                if (!validationResult.IsValid)
                {
                    return MenuInputResult.Failure(validationResult.Error ?? "Validation failed");
                }
                // 2. Find appropriate handler
                if (!handlers.TryGetValue(currentState, out var handler))
                {
                    var error = $"No handler registered for state: {currentState}";
                    return MenuInputResult.Failure(error);
                }


                // 3. Route to handler
                var result = await handler.HandleInput(input);
                return result;
            }
            catch (Exception ex)
            {
                var error = $"Exception routing input: {ex.Message}";
                return MenuInputResult.Failure("Error processing input");
            }
        }

        /// <summary>
        /// Gets the number of registered handlers.
        /// </summary>
        public int HandlerCount => handlers.Count;

        /// <summary>
        /// Checks if a handler is registered for the given state.
        /// </summary>
        public bool HasHandler(GameState state)
        {
            return handlers.ContainsKey(state);
        }
    }
}

