using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Combat.Events
{
    /// <summary>
    /// Event bus for combat events - enables event-driven conditional triggers
    /// Uses Observer pattern for decoupled event handling
    /// </summary>
    public class CombatEventBus
    {
        private static CombatEventBus? _instance;
        private readonly Dictionary<CombatEventType, List<Action<CombatEvent>>> _subscribers;

        private CombatEventBus()
        {
            _subscribers = new Dictionary<CombatEventType, List<Action<CombatEvent>>>();
        }

        /// <summary>
        /// Gets the singleton instance of the event bus
        /// </summary>
        public static CombatEventBus Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new CombatEventBus();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Subscribes to a specific event type
        /// </summary>
        public void Subscribe(CombatEventType eventType, Action<CombatEvent> handler)
        {
            if (!_subscribers.ContainsKey(eventType))
            {
                _subscribers[eventType] = new List<Action<CombatEvent>>();
            }

            _subscribers[eventType].Add(handler);
        }

        /// <summary>
        /// Unsubscribes from an event type
        /// </summary>
        public void Unsubscribe(CombatEventType eventType, Action<CombatEvent> handler)
        {
            if (_subscribers.TryGetValue(eventType, out var handlers))
            {
                handlers.Remove(handler);
            }
        }

        /// <summary>
        /// Publishes an event to all subscribers
        /// </summary>
        public void Publish(CombatEvent evt)
        {
            if (_subscribers.TryGetValue(evt.Type, out var handlers))
            {
                // Create a copy of the list to avoid modification during iteration
                var handlersCopy = handlers.ToList();
                foreach (var handler in handlersCopy)
                {
                    try
                    {
                        handler(evt);
                    }
                    catch (Exception)
                    {
                        // Log error but don't stop other handlers
                    }
                }
            }
        }

        /// <summary>
        /// Clears all subscribers (useful for testing or resetting state)
        /// </summary>
        public void Clear()
        {
            _subscribers.Clear();
        }

        /// <summary>
        /// Resets the singleton instance (useful for testing)
        /// </summary>
        public static void Reset()
        {
            _instance = null;
        }
    }
}

