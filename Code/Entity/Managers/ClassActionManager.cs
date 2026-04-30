using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Manages class-specific actions and abilities
    /// Handles adding/removing actions based on character progression and CLASS ACTIONS / ClassActions.json rules.
    /// </summary>
    public class ClassActionManager
    {
        /// <summary>Historical names used on gear; union with sheet rules so removal/re-apply stays correct.</summary>
        private static readonly string[] LegacyClassActionNames =
        {
            "TAUNT", "JAB", "STUN", "CRIT", "SHIELD BASH", "DEFENSIVE STANCE",
            "BERSERK", "BLOOD FRENZY", "PRECISION STRIKE", "QUICK REFLEXES",
            "FOCUS", "READ BOOK", "HEROIC STRIKE", "WHIRLWIND", "BERSERKER RAGE",
            "SHADOW STRIKE", "FIREBALL", "METEOR", "ICE STORM", "LIGHTNING BOLT",
            "FOLLOW THROUGH", "MISDIRECT", "CHANNEL"
        };

        private static HashSet<string> AllClassActionNamesForPoolLogic()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string n in LegacyClassActionNames)
                set.Add(n);
            try
            {
                foreach (string n in GameConfiguration.Instance.ClassActionsUnlock.AllRuleActionNames())
                    set.Add(n);
            }
            catch
            {
                // Instance or config unavailable (tests)
            }
            return set;
        }

        /// <summary>
        /// Adds class-specific actions based on character progression
        /// </summary>
        public void AddClassActions(Actor entity, CharacterProgression? progression, WeaponType? weaponType)
        {
            if (progression != null && HasAnyClassPoints(progression))
                RemoveClassActions(entity, progression, weaponType);

            var pres = GameConfiguration.Instance.ClassPresentation.EnsureNormalized();
            var rules = GameConfiguration.Instance.ClassActionsUnlock?.Rules;
            if (rules == null || rules.Count == 0)
                return;

            var addedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var rule in rules)
            {
                if (!ClassActionsUnlockConfig.TryResolveClassKeyToWeaponType(rule.ClassKey, pres, out WeaponType path))
                    continue;
                if (path == WeaponType.Wand && !IsWizardClass(progression!, weaponType))
                    continue;

                int pts = ClassActionsUnlockConfig.GetClassPointsForWeapon(progression!, path, pres);
                if (!ClassActionsUnlockConfig.IsRuleUnlocked(rule, pts, pres))
                    continue;

                if (!addedNames.Add(rule.ActionName))
                    continue;

                AddActionIfExists(entity, rule.ActionName);
            }
        }

        private bool HasAnyClassPoints(CharacterProgression progression)
        {
            return progression.BarbarianPoints > 0
                   || progression.WarriorPoints > 0
                   || progression.RoguePoints > 0
                   || progression.WizardPoints > 0;
        }

        private void AddActionIfExists(Actor entity, string actionName)
        {
            try
            {
                var action = ActionLoader.GetAction(actionName);
                if (action != null)
                {
                    action.IsComboAction = true;
                    DebugLogger.LogFormat("ClassActionManager",
                        "Marked class action '{0}' as combo action", actionName);

                    entity.AddAction(action, 1.0);
                    DebugLogger.LogFormat("ClassActionManager",
                        "Added class action: {0} (isComboAction: {1})", actionName, action.IsComboAction);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogFormat("ClassActionManager",
                    "Error adding action {0}: {1}", actionName, ex.Message);
            }
        }

        private void RemoveClassActions(Actor entity, CharacterProgression? progression, WeaponType? weaponType)
        {
            if (progression == null)
                return;

            var pres = GameConfiguration.Instance.ClassPresentation.EnsureNormalized();
            var expected = BuildExpectedClassActionNames(progression, weaponType, pres);
            var allNames = AllClassActionNamesForPoolLogic();
            var actionsToRemove = new List<(Action action, double probability)>();

            foreach (var actionEntry in entity.ActionPool)
            {
                if (allNames.Contains(actionEntry.action.Name)
                    && expected.Contains(actionEntry.action.Name))
                    actionsToRemove.Add(actionEntry);
            }

            foreach (var (action, _) in actionsToRemove)
            {
                try
                {
                    entity.RemoveAction(action);
                }
                catch (Exception ex)
                {
                    DebugLogger.LogFormat("ClassActionManager",
                        "Error removing action {0}: {1}", action.Name, ex.Message);
                }
            }

            if (actionsToRemove.Count > 0)
            {
                DebugLogger.LogFormat("ClassActionManager",
                    "Removed {0} class actions (preserved gear actions with same names)", actionsToRemove.Count);
            }
        }

        private static HashSet<string> BuildExpectedClassActionNames(
            CharacterProgression progression,
            WeaponType? weaponType,
            ClassPresentationConfig pres)
        {
            var expected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var rules = GameConfiguration.Instance.ClassActionsUnlock?.Rules;
            if (rules == null)
                return expected;

            foreach (var rule in rules)
            {
                if (!ClassActionsUnlockConfig.TryResolveClassKeyToWeaponType(rule.ClassKey, pres, out WeaponType path))
                    continue;
                if (path == WeaponType.Wand && !IsWizardClass(progression, weaponType))
                    continue;

                int pts = ClassActionsUnlockConfig.GetClassPointsForWeapon(progression, path, pres);
                if (ClassActionsUnlockConfig.IsRuleUnlocked(rule, pts, pres))
                    expected.Add(rule.ActionName);
            }

            return expected;
        }

        private static bool IsWizardClass(CharacterProgression progression, WeaponType? weaponType)
        {
            if (weaponType == WeaponType.Wand)
                return true;
            return progression.WizardPoints > 0;
        }
    }
}
