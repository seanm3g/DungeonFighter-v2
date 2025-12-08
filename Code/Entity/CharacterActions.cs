using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Coordinator for character action management
    /// Delegates to specialized managers for different action responsibilities
    /// Uses composition pattern with focused manager classes
    /// </summary>
    public class CharacterActions
    {
        // Specialized managers using composition pattern
        private readonly ComboSequenceManager _comboManager;
        private readonly GearActionManager _gearManager;
        private readonly ClassActionManager _classManager;
        private readonly EnvironmentActionManager _environmentManager;
        private readonly DefaultActionManager _defaultManager;

        // Public property for backwards compatibility
        public List<Action> ComboSequence => _comboManager.GetComboActions();

        public CharacterActions()
        {
            _comboManager = new ComboSequenceManager();
            _gearManager = new GearActionManager();
            _classManager = new ClassActionManager();
            _environmentManager = new EnvironmentActionManager();
            _defaultManager = new DefaultActionManager();
        }

        // ========== Combo System Delegation ==========
        
        public List<Action> GetComboActions()
        {
            return _comboManager.GetComboActions();
        }

        public void AddToCombo(Action action)
        {
            _comboManager.AddToCombo(action);
        }

        public void RemoveFromCombo(Action action)
        {
            _comboManager.RemoveFromCombo(action);
        }

        public void InitializeDefaultCombo(Actor actor, WeaponItem? weapon)
        {
            _comboManager.InitializeDefaultCombo(actor, weapon);
        }

        public void UpdateComboSequenceAfterGearChange(Actor actor)
        {
            _comboManager.UpdateComboSequenceAfterGearChange(actor);
        }

        // ========== Gear Actions Delegation ==========

        public void AddWeaponActions(Actor actor, WeaponItem weapon)
        {
            _gearManager.AddWeaponActions(actor, weapon);
        }

        public void AddArmorActions(Actor actor, Item armor)
        {
            _gearManager.AddArmorActions(actor, armor);
        }

        public void RemoveWeaponActions(Actor actor, WeaponItem? weapon = null)
        {
            _gearManager.RemoveWeaponActions(actor, weapon);
        }

        public void RemoveArmorActions(Actor actor, Item? armor)
        {
            _gearManager.RemoveArmorActions(actor, armor);
        }

        public void ApplyRollBonusesFromGear(Actor actor, Item gear)
        {
            _gearManager.ApplyRollBonusesFromGear(actor, gear);
        }

        public void RemoveRollBonusesFromGear(Actor actor, Item gear)
        {
            _gearManager.RemoveRollBonusesFromGear(actor, gear);
        }

        // ========== Class Actions Delegation ==========

        public void AddClassActions(Actor actor, CharacterProgression progression, WeaponType? weaponType)
        {
            _classManager.AddClassActions(actor, progression, weaponType);
        }

        // ========== Environment Actions Delegation ==========

        public void AddEnvironmentActions(Actor actor, Environment environment)
        {
            _environmentManager.AddEnvironmentActions(actor, environment);
        }

        public void ClearEnvironmentActions(Actor actor)
        {
            _environmentManager.ClearEnvironmentActions(actor);
        }

        // ========== Default Actions Delegation ==========

        public void AddDefaultActions(Actor actor)
        {
            _defaultManager.AddDefaultActions(actor);
        }

        public void EnsureBasicAttackAvailable(Actor actor)
        {
            _defaultManager.EnsureBasicAttackAvailable(actor);
        }

        public List<Action> GetAvailableUniqueActions(WeaponItem? weapon)
        {
            return _defaultManager.GetAvailableUniqueActions(weapon);
        }

        public void UpdateComboBonus(CharacterEquipment equipment)
        {
            _defaultManager.UpdateComboBonus(equipment);
        }

        // ========== Utility Methods ==========

        public double CalculateTurnsFromActionLength(double actionLength)
        {
            return actionLength / Character.DEFAULT_ACTION_LENGTH;
        }

        /// <summary>
        /// Gets action pool with combo actions from actor
        /// </summary>
        public List<Action> GetActionPool(Actor actor)
        {
            var allActions = new List<Action>();
            foreach (var (action, _) in actor.ActionPool)
            {
                if (action.IsComboAction)
                {
                    allActions.Add(action);
                }
            }
            return allActions;
        }

        /// <summary>
        /// Gets all actions available to the character (combo actions + action pool)
        /// </summary>
        public List<Action> GetAllActions(Actor actor)
        {
            var allActions = new HashSet<Action>();
            
            // Add combo actions
            var comboActions = GetComboActions();
            foreach (var action in comboActions)
            {
                allActions.Add(action);
            }
            
            // Add action pool actions
            var actionPool = GetActionPool(actor);
            foreach (var action in actionPool)
            {
                allActions.Add(action);
            }
            
            return allActions.ToList();
        }

        /// <summary>
        /// Removes all item actions and reinitializes defaults
        /// </summary>
        public void RemoveItemActions(Actor actor)
        {
            // Reset class actions - actor must be a Character to have Progression
            if (actor is Character character)
            {
                _classManager.AddClassActions(actor, character.Progression, null);
            }
        }
    }
}
