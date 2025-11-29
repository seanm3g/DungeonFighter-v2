using System;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Manages actions for enemies
    /// </summary>
    public static class EnemyActionManager
    {
        /// <summary>
        /// Adds actions to an enemy from enemy data
        /// </summary>
        public static void AddActionsToEnemy(Enemy enemy, EnemyData data)
        {
            // Clear default actions and add actions from the data
            enemy.ActionPool.Clear();
            foreach (var actionName in data.Actions)
            {
                var action = ActionLoader.GetAction(actionName);
                if (action != null)
                {
                    enemy.AddAction(action, 1.0); // Default weight of 1.0
                }
                else
                {
                    // If JSON action fails to load, add a fallback basic attack
                    var fallbackAction = new Action(
                        actionName,
                        ActionType.Attack,
                        TargetType.SingleTarget,
                        baseValue: 8,
                        range: 1,
                        description: $"A {actionName.ToLower()}"
                    );
                    enemy.AddAction(fallbackAction, 1.0);
                }
            }

            // Ensure ALL enemies have a BASIC ATTACK action
            bool hasBasicAttack = enemy.ActionPool.Any(a => string.Equals(a.action.Name, "BASIC ATTACK", StringComparison.OrdinalIgnoreCase));
            if (!hasBasicAttack)
            {
                // Try to inject BASIC ATTACK from actions data
                var basic = ActionLoader.GetAction("BASIC ATTACK");
                if (basic != null)
                {
                    enemy.AddAction(basic, 1.0);
                }
                else
                {
                    // Final fallback: create a simple basic attack
                    var createdBasic = new Action(
                        name: "BASIC ATTACK",
                        type: ActionType.Attack,
                        targetType: TargetType.SingleTarget,
                        baseValue: 8,
                        range: 1,
                        description: "A standard physical attack"
                    );
                    enemy.AddAction(createdBasic, 1.0);
                }
            }

            // Additional safeguard: ensure enemy has at least one damaging action (Attack or Spell)
            bool hasDamagingAction = enemy.ActionPool.Any(a => a.action.Type == ActionType.Attack || a.action.Type == ActionType.Spell);
            if (!hasDamagingAction)
            {
                UIManager.WriteSystemLine($"Warning: Enemy '{data.Name}' still has no damaging actions after adding BASIC ATTACK.");
            }
        }
    }
}

