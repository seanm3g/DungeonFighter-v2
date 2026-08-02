using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Manages class-specific actions and abilities.
    /// When <see cref="SkillTreesConfig"/> is loaded, unlocks come from learned skill-tree Action nodes.
    /// Otherwise falls back to CLASS ACTIONS / ClassActions.json rules.
    /// </summary>
    public class ClassActionManager
    {
        /// <summary>Historical names used on gear; union with sheet/tree rules so removal/re-apply stays correct.</summary>
        private static readonly string[] LegacyClassActionNames =
        {
            "TAUNT", "JAB", "STUN", "CRIT", "SHIELD BASH", "DEFENSIVE STANCE",
            "BERSERK", "BLOOD FRENZY", "PRECISION STRIKE", "QUICK REFLEXES",
            "FOCUS", "READ BOOK", "HEROIC STRIKE", "WHIRLWIND", "BERSERKER RAGE",
            "SHADOW STRIKE", "FIREBALL", "METEOR", "ICE STORM", "LIGHTNING BOLT",
            "FOLLOW THROUGH", "MISDIRECT", "CHANNEL",
            "MIGHTY SWING", "WARCRY", "BARBARIAN RAGE", "CHALLENGE", "MEASURED CUT",
            "LOADED DICE", "ECHO SPELL", "REWRITE FATE"
        };

        private static bool UseSkillTrees()
        {
            try
            {
                var trees = GameConfiguration.Instance.SkillTrees;
                return trees != null && trees.Trees.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        private static HashSet<string> AllClassActionNamesForPoolLogic()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string n in LegacyClassActionNames)
                set.Add(n);
            try
            {
                foreach (string n in GameConfiguration.Instance.ClassActionsUnlock.AllRuleActionNames())
                    set.Add(n);
                foreach (string n in SkillTreeService.Trees.AllUnlockActionNames())
                    set.Add(n!);
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
            if (progression == null)
                return;

            progression.EnsureSkillTreeRootsGranted();

            if (UseSkillTrees())
            {
                AddSkillTreeActions(entity, progression, weaponType);
                return;
            }

            AddLegacyClassActionRules(entity, progression, weaponType);
        }

        private void AddSkillTreeActions(Actor entity, CharacterProgression progression, WeaponType? weaponType)
        {
            var expected = new HashSet<string>(
                SkillTreeService.GetLearnedUnlockActionNames(progression),
                StringComparer.OrdinalIgnoreCase);
            var allNames = AllClassActionNamesForPoolLogic();

            if (HasAnyClassPoints(progression) || expected.Count > 0)
            {
                var presentExpectedInPool = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in entity.ActionPool)
                {
                    if (allNames.Contains(entry.action.Name) && expected.Contains(entry.action.Name))
                        presentExpectedInPool.Add(entry.action.Name);
                }

                if (presentExpectedInPool.SetEquals(expected))
                    return;

                RemoveExpectedClassActions(entity, expected, allNames);
            }

            SkillTreeService.ApplyLearnedSkillActions(entity, progression);
        }

        private void AddLegacyClassActionRules(Actor entity, CharacterProgression progression, WeaponType? weaponType)
        {
            var pres = GameConfiguration.Instance.ClassPresentation.EnsureNormalized();
            var rules = GameConfiguration.Instance.ClassActionsUnlock?.Rules;
            if (rules == null || rules.Count == 0)
                return;

            if (HasAnyClassPoints(progression))
            {
                var expected = BuildExpectedClassActionNames(progression, weaponType, pres);
                var allNames = AllClassActionNamesForPoolLogic();
                var presentExpectedInPool = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in entity.ActionPool)
                {
                    if (allNames.Contains(entry.action.Name) && expected.Contains(entry.action.Name))
                        presentExpectedInPool.Add(entry.action.Name);
                }

                if (presentExpectedInPool.SetEquals(expected))
                    return;

                RemoveExpectedClassActions(entity, expected, allNames);
            }

            var addedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var rule in rules)
            {
                if (!ClassActionsUnlockConfig.TryResolveClassKeyToWeaponType(rule.ClassKey, pres, out WeaponType path))
                    continue;
                if (path == WeaponType.Wand && !IsWizardClass(progression, weaponType))
                    continue;

                int pts = ClassActionsUnlockConfig.GetClassPointsForWeapon(progression, path, pres);
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
                    entity.AddAction(action, 1.0);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogFormat("ClassActionManager",
                    "Error adding action {0}: {1}", actionName, ex.Message);
            }
        }

        private void RemoveExpectedClassActions(
            Actor entity,
            HashSet<string> expected,
            HashSet<string> allNames)
        {
            var actionsToRemove = new List<(Action action, double probability)>();
            foreach (var actionEntry in entity.ActionPool)
            {
                if (allNames.Contains(actionEntry.action.Name)
                    && expected.Contains(actionEntry.action.Name))
                    actionsToRemove.Add(actionEntry);
            }

            // Also remove tree/class actions that are no longer expected
            foreach (var actionEntry in entity.ActionPool)
            {
                if (allNames.Contains(actionEntry.action.Name)
                    && !expected.Contains(actionEntry.action.Name)
                    && !actionsToRemove.Any(a => ReferenceEquals(a.action, actionEntry.action)))
                {
                    // Only strip managed class/tree names that are not currently expected
                    if (SkillTreeService.IsTreeActionName(actionEntry.action.Name)
                        || LegacyClassActionNames.Contains(actionEntry.action.Name, StringComparer.OrdinalIgnoreCase))
                        actionsToRemove.Add(actionEntry);
                }
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
