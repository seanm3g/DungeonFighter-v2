using System;
using RPGGame;
using System.Collections.Generic;

namespace RPGGame.Tests
{
    public class ComboSystemTests
    {
        public static void RunAll()
        {
            TestComboActionProgression();
            TestTauntComboBonus();
        }

        public static void TestComboActionProgression()
        {
            var character = new Character("ComboTester");
            var enemy = new Character("Dummy");
            int comboBonus = 0;
            int comboStep = 0;
            double damageAmplifier = 1.0;
            var comboActions = new List<Action>();
            foreach (var actionEntry in character.GetType().GetField("ActionPool", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(character) as Dictionary<Action, double>)
            {
                if (actionEntry.Key.IsComboAction)
                    comboActions.Add(actionEntry.Key);
            }
            comboActions.Sort((a, b) => a.ComboOrder.CompareTo(b.ComboOrder));
            for (int i = 0; i < comboActions.Count; i++)
            {
                var action = comboActions[i];
                int roll = 15; // Simulate always succeeding
                if (roll + comboBonus >= 15)
                {
                    damageAmplifier *= (i == 0 ? 1 : 1.85);
                    Console.WriteLine($"Combo Step {i}: {action.Name} succeeded. Damage Multiplier: {damageAmplifier}");
                }
                else
                {
                    Console.WriteLine($"Combo Step {i}: {action.Name} failed. Combo resets.");
                    break;
                }
            }
        }

        public static void TestTauntComboBonus()
        {
            Console.WriteLine("Running Taunt Combo Bonus Test...");
            var character = new Character("ComboTester");
            var enemy = new Character("Dummy");
            var comboActions = character.GetComboActions();
            var taunt = comboActions.Find(a => a.Name == "Taunt");
            if (taunt == null)
            {
                Console.WriteLine("Taunt action not found!");
                return;
            }
            // Simulate using Taunt
            character.SetTempComboBonus(0, 0); // Reset any temp bonus
            if (taunt.ComboBonusAmount != 2 || taunt.ComboBonusDuration != 2)
            {
                Console.WriteLine($"FAIL: Taunt does not have correct combo bonus properties. Amount: {taunt.ComboBonusAmount}, Duration: {taunt.ComboBonusDuration}");
                return;
            }
            // Use Taunt and check bonus is applied
            character.SetTempComboBonus(taunt.ComboBonusAmount, taunt.ComboBonusDuration);
            int bonus1 = character.ConsumeTempComboBonus();
            int bonus2 = character.ConsumeTempComboBonus();
            int bonus3 = character.ConsumeTempComboBonus();
            if (bonus1 == 2 && bonus2 == 2 && bonus3 == 0)
            {
                Console.WriteLine("PASS: Taunt combo bonus and duration applied correctly.");
            }
            else
            {
                Console.WriteLine($"FAIL: Taunt bonus sequence incorrect. Got: {bonus1}, {bonus2}, {bonus3}");
            }
        }
    }
} 