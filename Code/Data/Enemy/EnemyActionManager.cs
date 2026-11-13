using System;
using System.Linq;
using RPGGame.Data;

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
            for (int slot = 0; slot < data.Actions.Count; slot++)
            {
                string actionName = data.Actions[slot];
                // Signature kit slot: the flavor name runs its dungeon's template (EnemyAffixes.json signature).
                string? kitTemplate = data.Kit != null && slot < data.Kit.Count && AffixFeatures.Kits ? data.Kit[slot] : null;
                if (!string.IsNullOrWhiteSpace(kitTemplate)
                    && GameConfiguration.Instance.EnemySystem?.ActionKits == true
                    && EnemyActionCatalog.TryCreate(actionName, kitTemplate) is { } signatureAction)
                {
                    enemy.AddAction(signatureAction, 1.0);
                    continue;
                }

                var action = ActionLoader.GetAction(actionName);
                if (action != null)
                {
                    if (GameDataTagHelper.HasEnvironmentTag(action.Tags))
                        continue;
                    enemy.AddAction(action, 1.0); // Default weight of 1.0
                }
                else
                {
                    var actionData = ActionLoader.GetActionData(actionName);
                    if (actionData != null && GameDataTagHelper.HasEnvironmentTag(actionData.Tags))
                        continue;

                    // Mechanic kit: the flavor name keeps its template's mechanics (EnemyActions.json).
                    if (GameConfiguration.Instance.EnemySystem?.ActionKits == true
                        && EnemyActionCatalog.TryCreate(actionName) is { } kitAction)
                    {
                        enemy.AddAction(kitAction, 1.0);
                        continue;
                    }

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

            enemy.InitializeActionSequence();

            // Additional safeguard: ensure enemy has at least one damaging action (Attack or Spell)
            bool hasDamagingAction = enemy.ActionPool.Any(a => a.action.Type == ActionType.Attack || a.action.Type == ActionType.Spell);
            if (!hasDamagingAction)
            {
                UIManager.WriteSystemLine($"Warning: Enemy '{data.Name}' has no damaging actions.");
            }
        }
    }
}

