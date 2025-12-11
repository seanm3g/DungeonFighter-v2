using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Combat.Events;
using RPGGame.Utils;

namespace RPGGame.Combat.Outcomes
{
    /// <summary>
    /// Registry for managing outcome handlers
    /// Uses Strategy pattern similar to EffectHandlerRegistry
    /// </summary>
    public class OutcomeHandlerRegistry
    {
        private static readonly OutcomeHandlerRegistry _instance = new OutcomeHandlerRegistry();
        private readonly Dictionary<string, IOutcomeHandler> _handlers;

        private OutcomeHandlerRegistry()
        {
            _handlers = new Dictionary<string, IOutcomeHandler>
            {
                { "conditional", new ConditionalOutcomeHandler() }
                // Can add more handler types here (e.g., "xpGain", "itemDrop", etc.)
            };
        }

        public static OutcomeHandlerRegistry Instance => _instance;

        /// <summary>
        /// Registers an outcome handler
        /// </summary>
        public void RegisterHandler(string handlerType, IOutcomeHandler handler)
        {
            _handlers[handlerType.ToLower()] = handler;
        }

        /// <summary>
        /// Gets an outcome handler by type
        /// </summary>
        public IOutcomeHandler? GetHandler(string handlerType)
        {
            return _handlers.TryGetValue(handlerType.ToLower(), out var handler) ? handler : null;
        }

        /// <summary>
        /// Processes outcome handlers for an action
        /// </summary>
        public void ProcessOutcomes(Action action, CombatEvent evt, Actor source, Actor? target)
        {
            if (action.OutcomeHandlers == null || action.OutcomeHandlers.Count == 0)
                return;

            foreach (var handlerType in action.OutcomeHandlers)
            {
                var handler = GetHandler(handlerType);
                if (handler != null)
                {
                    try
                    {
                        handler.HandleOutcome(evt, source, target, action);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }
    }
}

