using System;

namespace RPGGame
{
    public class TestScaling
    {
        public static void TestNewScalingValues()
        {
            Console.WriteLine("=== TESTING NEW SCALING VALUES ===");
            Console.WriteLine();
            
            // Test weapon damage scaling with new values
            Console.WriteLine("Weapon Damage Scaling (Base Damage = 10):");
            Console.WriteLine("Tier\tLevel 1\tLevel 5\tLevel 10");
            
            for (int tier = 1; tier <= 5; tier++)
            {
                int baseDamage = 10;
                
                // New formula: BaseDamage * (1 + (Tier - 1) * 0.3 + Level * 0.05)
                double level1 = baseDamage * (1 + (tier - 1) * 0.3 + 1 * 0.05);
                double level5 = baseDamage * (1 + (tier - 1) * 0.3 + 5 * 0.05);
                double level10 = baseDamage * (1 + (tier - 1) * 0.3 + 10 * 0.05);
                
                Console.WriteLine($"{tier}\t{level1:F1}\t{level5:F1}\t{level10:F1}");
            }
            
            Console.WriteLine();
            Console.WriteLine("Rarity Multipliers:");
            Console.WriteLine("Common: 1.0x, Uncommon: 1.1x, Rare: 1.25x, Epic: 1.4x, Legendary: 1.6x");
            
            Console.WriteLine();
            Console.WriteLine("Example: Tier 3 Weapon at Level 5 with Different Rarities:");
            int baseDmg = 10;
            double tierScaled = baseDmg * (1 + (3 - 1) * 0.3 + 5 * 0.05); // = 10 * (1 + 0.6 + 0.25) = 10 * 1.85 = 18.5
            
            Console.WriteLine($"Base (after tier/level scaling): {tierScaled:F1}");
            Console.WriteLine($"Common: {tierScaled * 1.0:F1}");
            Console.WriteLine($"Uncommon: {tierScaled * 1.1:F1}");
            Console.WriteLine($"Rare: {tierScaled * 1.25:F1}");
            Console.WriteLine($"Epic: {tierScaled * 1.4:F1}");
            Console.WriteLine($"Legendary: {tierScaled * 1.6:F1}");
            
            Console.WriteLine();
            Console.WriteLine("These values should provide much more balanced scaling!");
        }
    }
}
