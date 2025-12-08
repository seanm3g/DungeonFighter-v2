using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Actions.RollModification
{
    /// <summary>
    /// Registry for managing roll modifiers using Strategy pattern
    /// Similar to EffectHandlerRegistry for status effects
    /// </summary>
    public class RollModifierRegistry
    {
        private readonly List<IRollModifier> _modifiers;
        private readonly Dictionary<string, IRollModifier> _modifiersByName;

        public RollModifierRegistry()
        {
            _modifiers = new List<IRollModifier>();
            _modifiersByName = new Dictionary<string, IRollModifier>();
        }

        /// <summary>
        /// Registers a roll modifier
        /// </summary>
        public void Register(IRollModifier modifier)
        {
            if (modifier == null)
                throw new ArgumentNullException(nameof(modifier));

            if (_modifiersByName.ContainsKey(modifier.Name))
            {
                // Replace existing modifier with same name
                var existing = _modifiersByName[modifier.Name];
                _modifiers.Remove(existing);
            }

            _modifiers.Add(modifier);
            _modifiersByName[modifier.Name] = modifier;
            
            // Sort by priority (higher priority first)
            _modifiers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }

        /// <summary>
        /// Unregisters a roll modifier
        /// </summary>
        public void Unregister(string name)
        {
            if (_modifiersByName.TryGetValue(name, out var modifier))
            {
                _modifiers.Remove(modifier);
                _modifiersByName.Remove(name);
            }
        }

        /// <summary>
        /// Applies all registered modifiers to a roll
        /// </summary>
        public int ApplyModifiers(int baseRoll, RollModificationContext context)
        {
            int modifiedRoll = baseRoll;
            
            foreach (var modifier in _modifiers)
            {
                modifiedRoll = modifier.ModifyRoll(modifiedRoll, context);
            }
            
            return modifiedRoll;
        }

        /// <summary>
        /// Gets a modifier by name
        /// </summary>
        public IRollModifier? GetModifier(string name)
        {
            _modifiersByName.TryGetValue(name, out var modifier);
            return modifier;
        }

        /// <summary>
        /// Gets all registered modifiers
        /// </summary>
        public IEnumerable<IRollModifier> GetAllModifiers()
        {
            return _modifiers.ToList();
        }

        /// <summary>
        /// Clears all registered modifiers
        /// </summary>
        public void Clear()
        {
            _modifiers.Clear();
            _modifiersByName.Clear();
        }
    }
}

