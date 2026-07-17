using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Actions;
using RPGGame.Actions.Execution;
using RPGGame.Actions.RollModification;
using RPGGame.Data;
using RPGGame.Tests;
using RPGGame.UI;
using RPGGame.Utils;

namespace RPGGame.Tests.Unit.Data.ActionBonusMechanics
{
    public static class ActionBonusMechanicsTestHelpers
    {



        /// <summary>
        /// Creates a 2-action combo: Adrenal Surge (Ability cadence, SpeedMod 20) and Rage.
        /// </summary>
        internal static Character CreateComboWithAbilitySpeedModAction()
        {
            var character = new Character("TestHero", 1);
            var adrenalSurge = TestDataBuilders.CreateMockAction("AdrenalSurge", ActionType.Attack);
            adrenalSurge.IsComboAction = true;
            adrenalSurge.ComboOrder = 1;
            adrenalSurge.Cadence = "Action";
            adrenalSurge.SpeedMod = "20";
            var rage = TestDataBuilders.CreateMockAction("Rage", ActionType.Attack);
            rage.IsComboAction = true;
            rage.ComboOrder = 2;
            rage.Length = 0.5;
            character.AddAction(adrenalSurge, 1.0);
            character.AddAction(rage, 1.0);
            character.Actions.AddToCombo(adrenalSurge);
            character.Actions.AddToCombo(rage);
            character.ComboStep = 0;
            return character;
        }


        /// <summary>
        /// Creates a 2-action combo: SetupStrike (grants "For next ACTION: +3 COMBO") and Finisher.
        /// </summary>
        internal static Character CreateComboWithBuffingAction(int count = 1)
        {
            var character = new Character("TestHero", 1);
            var setup = TestDataBuilders.CreateMockAction("SetupStrike", ActionType.Attack);
            setup.IsComboAction = true;
            setup.ComboOrder = 1;
            setup.ActionAttackBonuses = new ActionAttackBonuses
            {
                BonusGroups = new List<ActionAttackBonusGroup>
                {
                    new ActionAttackBonusGroup
                    {
                        CadenceType = "ACTION",
                        Count = count,
                        Bonuses = new List<ActionAttackBonusItem> { new ActionAttackBonusItem { Type = "COMBO", Value = 3 } }
                    }
                }
            };
            var finisher = TestDataBuilders.CreateMockAction("Finisher", ActionType.Attack);
            finisher.IsComboAction = true;
            finisher.ComboOrder = 2;
            finisher.ActionAttackBonuses = new ActionAttackBonuses
            {
                BonusGroups = new List<ActionAttackBonusGroup>
                {
                    new ActionAttackBonusGroup
                    {
                        CadenceType = "ACTION",
                        Count = count,
                        Bonuses = new List<ActionAttackBonusItem> { new ActionAttackBonusItem { Type = "COMBO", Value = 3 } }
                    }
                }
            };
            character.AddAction(setup, 1.0);
            character.AddAction(finisher, 1.0);
            character.Actions.AddToCombo(setup);
            character.Actions.AddToCombo(finisher);
            character.ComboStep = 0;
            return character;
        }


        internal static Character CreateFourActionComboWithBuffingAtSlot(int slotIndex, int bonusCount)
        {
            var character = new Character("TestHero", 1);
            var names = new[] { "Opener", "MidA", "MidB", "Finisher" };
            for (int i = 0; i < names.Length; i++)
            {
                var action = TestDataBuilders.CreateMockAction(names[i], ActionType.Attack);
                action.IsComboAction = true;
                action.ComboOrder = i + 1;
                if (i == slotIndex)
                {
                    action.ActionAttackBonuses = new ActionAttackBonuses
                    {
                        BonusGroups = new List<ActionAttackBonusGroup>
                        {
                            new ActionAttackBonusGroup
                            {
                                CadenceType = "ACTION",
                                Count = bonusCount,
                                Bonuses = new List<ActionAttackBonusItem> { new ActionAttackBonusItem { Type = "COMBO", Value = 2 } }
                            }
                        }
                    };
                }
                character.AddAction(action, 1.0);
                character.Actions.AddToCombo(action);
            }
            character.ComboStep = 0;
            return character;
        }


        internal static Character CreateFourActionComboWithDamageModBuffAtSlot0(int staleGroupCount, int comboBonusDuration, double damageModPercent)
        {
            var character = new Character("TestHero", 1);
            var names = new[] { "ActionBonus", "Burn", "Bleed", "Dance" };
            for (int i = 0; i < names.Length; i++)
            {
                var action = TestDataBuilders.CreateMockAction(names[i], ActionType.Attack);
                action.IsComboAction = true;
                action.ComboOrder = i + 1;
                action.DamageMultiplier = 1.0;
                if (i == 0)
                {
                    action.Cadence = "Action";
                    action.ComboBonusDuration = comboBonusDuration;
                    action.DamageMod = damageModPercent.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    action.ActionAttackBonuses = new ActionAttackBonuses
                    {
                        BonusGroups = new List<ActionAttackBonusGroup>
                        {
                            new ActionAttackBonusGroup
                            {
                                CadenceType = "ACTION",
                                Count = staleGroupCount,
                                Bonuses = new List<ActionAttackBonusItem>
                                {
                                    new ActionAttackBonusItem { Type = "DAMAGE_MOD", Value = damageModPercent }
                                }
                            }
                        }
                    };
                }
                character.AddAction(action, 1.0);
                character.Actions.AddToCombo(action);
            }
            character.ComboStep = 0;
            return character;
        }


        internal static Character CreateTestCharacterWithCombo()
        {
            var character = new Character("TestHero", 1);
            var combo1 = TestDataBuilders.CreateMockAction("COMBO1", ActionType.Attack);
            combo1.IsComboAction = true;
            var combo2 = TestDataBuilders.CreateMockAction("COMBO2", ActionType.Attack);
            combo2.IsComboAction = true;
            character.AddAction(combo1, 1.0);
            character.AddAction(combo2, 1.0);
            character.Actions.AddToCombo(combo1);
            character.Actions.AddToCombo(combo2);
            character.ComboStep = 0;
            return character;
        }


        internal static Character CreateThreeActionComboWithDamageModBuff(int count, double damageModPercent)
        {
            var character = new Character("TestHero", 1);
            var names = new[] { "ActionBonus", "Slam", "Finisher" };
            for (int i = 0; i < names.Length; i++)
            {
                var action = TestDataBuilders.CreateMockAction(names[i], ActionType.Attack);
                action.IsComboAction = true;
                action.ComboOrder = i + 1;
                action.DamageMultiplier = 1.0;
                if (i == 0)
                {
                    action.Cadence = "Action";
                    action.DamageMod = damageModPercent.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    action.ActionAttackBonuses = new ActionAttackBonuses
                    {
                        BonusGroups = new List<ActionAttackBonusGroup>
                        {
                            new ActionAttackBonusGroup
                            {
                                CadenceType = "ACTION",
                                Count = count,
                                Bonuses = new List<ActionAttackBonusItem>
                                {
                                    new ActionAttackBonusItem { Type = "DAMAGE_MOD", Value = damageModPercent }
                                }
                            }
                        }
                    };
                }
                character.AddAction(action, 1.0);
                character.Actions.AddToCombo(action);
            }
            character.ComboStep = 0;
            return character;
        }

        internal static Character BuildFourActionLabComboWithStaleStripBonus(int staleGroupCount)
        {
            var character = new Character("LabHero", 1);
            string[] names = { "ACTION BONUS", "CRITICAL ATTACK", "FIGHT BONUS", "SLAM" };
            for (int i = 0; i < names.Length; i++)
            {
                var action = ActionLoader.GetAction(names[i]) ?? TestDataBuilders.CreateMockAction(names[i], ActionType.Attack);
                action.IsComboAction = true;
                action.ComboOrder = i + 1;
                if (i == 0 && action.ActionAttackBonuses?.BonusGroups?.Count > 0)
                {
                    action.ComboBonusDuration = 0;
                    action.ActionAttackBonuses.BonusGroups[0].Count = staleGroupCount;
                }
                character.AddAction(action, 1.0);
                character.Actions.AddToCombo(action);
            }
            character.ComboStep = 0;
            return character;
        }

        internal static string FormatBonusGroupShort(ActionAttackBonusGroup g)
        {
            if (g?.Bonuses == null || g.Bonuses.Count == 0) return "(no bonuses)";
            var parts = g.Bonuses.Select(b =>
            {
                string sign = b.Value >= 0 ? "+" : "";
                return $"{sign}{b.Value:0} {b.Type}";
            });
            string cadence = g.CadenceType ?? g.Keyword ?? "";
            string count = g.Count > 1 ? $"{g.Count} {cadence}S" : $"Next {cadence}";
            return $"For {count}: {string.Join(", ", parts)}";
        }
    }
}
