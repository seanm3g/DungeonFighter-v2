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
                        cooldown: 0,
                        description: $"A {actionName.ToLower()}"
                    );
                    enemy.AddAction(fallbackAction, 1.0);
                }
            }

            // Additional safeguard: ensure enemy has at least one damaging action (Attack or Spell)
            bool hasDamagingAction = enemy.ActionPool.Any(a => a.action.Type == ActionType.Attack || a.action.Type == ActionType.Spell);
            if (!hasDamagingAction)
            {
                UIManager.WriteSystemLine($"Warning: Enemy '{data.Name}' has no damaging actions.");
            }
        }
    }
}

