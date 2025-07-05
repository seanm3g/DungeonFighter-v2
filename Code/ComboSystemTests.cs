using System;
using System.Collections.Generic;

namespace RPGGame
{
    public class ComboSystemTests
    {
        public static void RunAll()
        {
            TestComboActionProgression();
        }

        public static void TestComboActionProgression()
        {
            Console.WriteLine("Running Combo Action Progression Test...");
            var character = new Character("ComboTester");
            var enemy = new Character("Dummy");
            int comboBonus = 0;
            double damageAmplifier = 1.0;
            var comboActions = character.GetComboActions();
            Console.WriteLine($"Found {comboActions.Count} combo actions.");
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
    }
} 