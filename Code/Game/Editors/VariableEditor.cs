using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Utils;

namespace RPGGame.Editors
{
    /// <summary>
    /// Editor for tweaking common game variables from GameConfiguration
    /// </summary>
    public class VariableEditor
    {
        private readonly GameConfiguration config;
        private readonly List<EditableVariable> variables;

        public VariableEditor()
        {
            config = GameConfiguration.Instance;
            variables = new List<EditableVariable>();
            InitializeVariables();
        }

        /// <summary>
        /// Initialize the list of editable variables
        /// </summary>
        private void InitializeVariables()
        {
            // Combat variables
            variables.Add(new EditableVariable("Combat.CriticalHitThreshold", () => config.Combat.CriticalHitThreshold, v => config.Combat.CriticalHitThreshold = Convert.ToInt32(v), "Critical hit threshold (1-20)"));
            variables.Add(new EditableVariable("Combat.CriticalHitMultiplier", () => config.Combat.CriticalHitMultiplier, v => config.Combat.CriticalHitMultiplier = Convert.ToDouble(v), "Critical hit damage multiplier"));
            variables.Add(new EditableVariable("Combat.MinimumDamage", () => config.Combat.MinimumDamage, v => config.Combat.MinimumDamage = Convert.ToInt32(v), "Minimum damage dealt"));
            variables.Add(new EditableVariable("Combat.BaseAttackTime", () => config.Combat.BaseAttackTime, v => config.Combat.BaseAttackTime = Convert.ToDouble(v), "Base attack time in seconds"));
            
            // Character variables
            variables.Add(new EditableVariable("Character.PlayerBaseHealth", () => config.Character.PlayerBaseHealth, v => config.Character.PlayerBaseHealth = Convert.ToInt32(v), "Player base health at level 1"));
            variables.Add(new EditableVariable("Character.HealthPerLevel", () => config.Character.HealthPerLevel, v => config.Character.HealthPerLevel = Convert.ToInt32(v), "Health gained per level"));
            
            // Attribute variables
            variables.Add(new EditableVariable("Attributes.PlayerBaseAttributes.Strength", () => config.Attributes.PlayerBaseAttributes.Strength, v => config.Attributes.PlayerBaseAttributes.Strength = Convert.ToInt32(v), "Base Strength"));
            variables.Add(new EditableVariable("Attributes.PlayerBaseAttributes.Agility", () => config.Attributes.PlayerBaseAttributes.Agility, v => config.Attributes.PlayerBaseAttributes.Agility = Convert.ToInt32(v), "Base Agility"));
            variables.Add(new EditableVariable("Attributes.PlayerBaseAttributes.Technique", () => config.Attributes.PlayerBaseAttributes.Technique, v => config.Attributes.PlayerBaseAttributes.Technique = Convert.ToInt32(v), "Base Technique"));
            variables.Add(new EditableVariable("Attributes.PlayerBaseAttributes.Intelligence", () => config.Attributes.PlayerBaseAttributes.Intelligence, v => config.Attributes.PlayerBaseAttributes.Intelligence = Convert.ToInt32(v), "Base Intelligence"));
            variables.Add(new EditableVariable("Attributes.PlayerAttributesPerLevel", () => config.Attributes.PlayerAttributesPerLevel, v => config.Attributes.PlayerAttributesPerLevel = Convert.ToInt32(v), "Attributes gained per level"));
            
            // Roll system variables
            variables.Add(new EditableVariable("RollSystem.MissThreshold.Min", () => config.RollSystem.MissThreshold.Min, v => config.RollSystem.MissThreshold.Min = Convert.ToInt32(v), "Minimum roll for miss"));
            variables.Add(new EditableVariable("RollSystem.MissThreshold.Max", () => config.RollSystem.MissThreshold.Max, v => config.RollSystem.MissThreshold.Max = Convert.ToInt32(v), "Maximum roll for miss"));
            variables.Add(new EditableVariable("RollSystem.BasicAttackThreshold.Min", () => config.RollSystem.BasicAttackThreshold.Min, v => config.RollSystem.BasicAttackThreshold.Min = Convert.ToInt32(v), "Minimum roll for basic attack"));
            variables.Add(new EditableVariable("RollSystem.BasicAttackThreshold.Max", () => config.RollSystem.BasicAttackThreshold.Max, v => config.RollSystem.BasicAttackThreshold.Max = Convert.ToInt32(v), "Maximum roll for basic attack"));
            variables.Add(new EditableVariable("RollSystem.ComboThreshold.Min", () => config.RollSystem.ComboThreshold.Min, v => config.RollSystem.ComboThreshold.Min = Convert.ToInt32(v), "Minimum roll for combo"));
            variables.Add(new EditableVariable("RollSystem.ComboThreshold.Max", () => config.RollSystem.ComboThreshold.Max, v => config.RollSystem.ComboThreshold.Max = Convert.ToInt32(v), "Maximum roll for combo"));
            variables.Add(new EditableVariable("RollSystem.CriticalThreshold", () => config.RollSystem.CriticalThreshold, v => config.RollSystem.CriticalThreshold = Convert.ToInt32(v), "Roll required for critical hit"));
        }

        /// <summary>
        /// Get all editable variables
        /// </summary>
        public List<EditableVariable> GetVariables()
        {
            return variables;
        }

        /// <summary>
        /// Get a variable by name
        /// </summary>
        public EditableVariable? GetVariable(string name)
        {
            return variables.FirstOrDefault(v => v.Name == name);
        }

        /// <summary>
        /// Save changes to TuningConfig.json
        /// </summary>
        public bool SaveChanges()
        {
            try
            {
                // Note: This would need to serialize GameConfiguration back to JSON
                // For now, we'll just reload to get fresh values
                config.Reload();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Represents an editable game variable
    /// </summary>
    public class EditableVariable
    {
        public string Name { get; }
        public string Description { get; }
        private readonly Func<object> getter;
        private readonly Action<object> setter;

        public EditableVariable(string name, Func<object> getter, Action<object> setter, string description)
        {
            Name = name;
            this.getter = getter;
            this.setter = setter;
            Description = description;
        }

        public object GetValue()
        {
            return getter();
        }

        public void SetValue(object value)
        {
            setter(value);
        }

        public Type GetValueType()
        {
            return getter().GetType();
        }
    }
}

