using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Analyzes and visualizes gear rarity drop rates and power scaling
    /// Helps with tuning rarity system balance
    /// </summary>
    public class RarityAnalyzer
    {
        private readonly List<RarityData> _rarityData;

        public RarityAnalyzer(List<RarityData> rarityData)
        {
            _rarityData = rarityData ?? new List<RarityData>();
        }

        /// <summary>
        /// Analyzes current rarity configuration and prints detailed report
        /// </summary>
        public void AnalyzeAndPrint()
        {
            if (_rarityData == null || _rarityData.Count == 0)
            {
                Console.WriteLine("No rarity data available for analysis.");
                return;
            }

            double totalWeight = _rarityData.Sum(r => r.Weight);
            
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine("GEAR RARITY SYSTEM ANALYSIS");
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine();

            // Drop Rate Analysis
            Console.WriteLine("DROP RATE ANALYSIS");
            Console.WriteLine("-".PadRight(80, '-'));
            Console.WriteLine($"{"Rarity",-15} {"Weight",-12} {"Drop Rate %",-15} {"Expected per 1000",-20}");
            Console.WriteLine("-".PadRight(80, '-'));

            foreach (var rarity in _rarityData.OrderByDescending(r => r.Weight))
            {
                double dropRate = (rarity.Weight / totalWeight) * 100.0;
                double expectedPer1000 = dropRate * 10.0;
                
                Console.WriteLine($"{rarity.Name,-15} {rarity.Weight,-12:F2} {dropRate,-15:F2} {expectedPer1000,-20:F1}");
            }

            Console.WriteLine($"{"TOTAL",-15} {totalWeight,-12:F2} {"100.00%",-15} {"1000.0",-20}");
            Console.WriteLine();

            // Power Scaling Analysis
            Console.WriteLine("POWER SCALING ANALYSIS");
            Console.WriteLine("-".PadRight(80, '-'));
            Console.WriteLine($"{"Rarity",-15} {"Stat",-8} {"Action",-8} {"Mod",-8} {"Total",-8} {"Power Est.",-15}");
            Console.WriteLine("-".PadRight(80, '-'));

            foreach (var rarity in _rarityData.OrderByDescending(r => r.Weight))
            {
                int totalBonuses = rarity.StatBonuses + rarity.ActionBonuses + rarity.Modifications;
                
                // Special case for Common
                if (rarity.Name.Equals("Common", StringComparison.OrdinalIgnoreCase))
                {
                    // Common: 25% chance for 2 bonuses, 75% for 0
                    string powerEst = "Baseline";
                    Console.WriteLine($"{rarity.Name,-15} {rarity.StatBonuses,-8} {rarity.ActionBonuses,-8} {rarity.Modifications,-8} {"0-2*",-8} {powerEst,-15}");
                }
                else
                {
                    string powerEst = GetPowerEstimate(totalBonuses);
                    Console.WriteLine($"{rarity.Name,-15} {rarity.StatBonuses,-8} {rarity.ActionBonuses,-8} {rarity.Modifications,-8} {totalBonuses,-8} {powerEst,-15}");
                }
            }

            Console.WriteLine();

            // Recommendations
            Console.WriteLine("TUNING RECOMMENDATIONS");
            Console.WriteLine("-".PadRight(80, '-'));
            PrintRecommendations();
            Console.WriteLine();

            // Simulation Results
            Console.WriteLine("SIMULATION (10,000 drops)");
            Console.WriteLine("-".PadRight(80, '-'));
            SimulateDrops(10000);
        }

        /// <summary>
        /// Simulates drops to verify actual drop rates
        /// </summary>
        private void SimulateDrops(int count)
        {
            var random = new Random();
            var results = new Dictionary<string, int>();
            double totalWeight = _rarityData.Sum(r => r.Weight);

            for (int i = 0; i < count; i++)
            {
                double roll = random.NextDouble() * totalWeight;
                double cumulative = 0;

                foreach (var rarity in _rarityData)
                {
                    cumulative += rarity.Weight;
                    if (roll < cumulative)
                    {
                        results[rarity.Name] = results.GetValueOrDefault(rarity.Name, 0) + 1;
                        break;
                    }
                }
            }

            Console.WriteLine($"{"Rarity",-15} {"Count",-10} {"Actual %",-12} {"Expected %",-12} {"Difference",-12}");
            Console.WriteLine("-".PadRight(80, '-'));

            foreach (var rarity in _rarityData.OrderByDescending(r => r.Weight))
            {
                int actualCount = results.GetValueOrDefault(rarity.Name, 0);
                double actualPercent = (actualCount / (double)count) * 100.0;
                double expectedPercent = (rarity.Weight / totalWeight) * 100.0;
                double difference = actualPercent - expectedPercent;

                Console.WriteLine($"{rarity.Name,-15} {actualCount,-10} {actualPercent,-12:F2} {expectedPercent,-12:F2} {difference,-12:+F2;-F2;0.00}");
            }
        }

        /// <summary>
        /// Gets power estimate based on total bonuses
        /// </summary>
        private string GetPowerEstimate(int totalBonuses)
        {
            return totalBonuses switch
            {
                0 => "Baseline",
                <= 2 => "Slight",
                <= 4 => "Moderate",
                <= 6 => "Strong",
                <= 8 => "Very Strong",
                <= 10 => "Exceptional",
                _ => "Game-Changing"
            };
        }

        /// <summary>
        /// Prints tuning recommendations based on current configuration
        /// </summary>
        private void PrintRecommendations()
        {
            var epic = _rarityData.FirstOrDefault(r => r.Name.Equals("Epic", StringComparison.OrdinalIgnoreCase));
            var rare = _rarityData.FirstOrDefault(r => r.Name.Equals("Rare", StringComparison.OrdinalIgnoreCase));
            var legendary = _rarityData.FirstOrDefault(r => r.Name.Equals("Legendary", StringComparison.OrdinalIgnoreCase));
            var mythic = _rarityData.FirstOrDefault(r => r.Name.Equals("Mythic", StringComparison.OrdinalIgnoreCase));
            var transcendent = _rarityData.FirstOrDefault(r => r.Name.Equals("Transcendent", StringComparison.OrdinalIgnoreCase));

            if (epic != null && rare != null)
            {
                int epicTotal = epic.StatBonuses + epic.ActionBonuses + epic.Modifications;
                int rareTotal = rare.StatBonuses + rare.ActionBonuses + rare.Modifications;
                
                if (epicTotal == rareTotal)
                {
                    Console.WriteLine("⚠️  Epic and Rare have same bonus counts. Consider giving Epic +1 modification.");
                }
            }

            if (transcendent != null)
            {
                double transcendentRate = (transcendent.Weight / _rarityData.Sum(r => r.Weight)) * 100.0;
                if (transcendentRate < 0.01)
                {
                    Console.WriteLine("⚠️  Transcendent is extremely rare (<0.01%). Consider if this is intentional.");
                }
            }

            // Check for gaps in drop rates
            var sortedByWeight = _rarityData.OrderByDescending(r => r.Weight).ToList();
            for (int i = 0; i < sortedByWeight.Count - 1; i++)
            {
                double currentRate = (sortedByWeight[i].Weight / _rarityData.Sum(r => r.Weight)) * 100.0;
                double nextRate = (sortedByWeight[i + 1].Weight / _rarityData.Sum(r => r.Weight)) * 100.0;
                double ratio = currentRate / nextRate;

                if (ratio > 100 && sortedByWeight[i].Name != "Common")
                {
                    Console.WriteLine($"ℹ️  Large gap between {sortedByWeight[i].Name} ({currentRate:F2}%) and {sortedByWeight[i + 1].Name} ({nextRate:F2}%)");
                }
            }

            Console.WriteLine();
            Console.WriteLine("💡 Tuning Tips:");
            Console.WriteLine("   • Adjust weights in RarityTable.json to change drop rates");
            Console.WriteLine("   • Modify bonus counts to change power levels");
            Console.WriteLine("   • Test with small changes (10-20%) first");
            Console.WriteLine("   • See Documentation/05-Systems/GEAR_RARITY_ANALYSIS.md for details");
        }

        /// <summary>
        /// Compares two rarity configurations
        /// </summary>
        public static void CompareConfigurations(List<RarityData> config1, List<RarityData> config2, string name1 = "Current", string name2 = "Proposed")
        {
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine("RARITY CONFIGURATION COMPARISON");
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine();

            double total1 = config1.Sum(r => r.Weight);
            double total2 = config2.Sum(r => r.Weight);

            Console.WriteLine($"{"Rarity",-15} {$"{name1} Rate %",-18} {$"{name2} Rate %",-18} {"Difference",-15}");
            Console.WriteLine("-".PadRight(80, '-'));

            var allRarities = config1.Select(r => r.Name)
                .Union(config2.Select(r => r.Name))
                .Distinct()
                .OrderBy(n => n);

            foreach (var rarityName in allRarities)
            {
                var r1 = config1.FirstOrDefault(r => r.Name.Equals(rarityName, StringComparison.OrdinalIgnoreCase));
                var r2 = config2.FirstOrDefault(r => r.Name.Equals(rarityName, StringComparison.OrdinalIgnoreCase));

                double rate1 = r1 != null ? (r1.Weight / total1) * 100.0 : 0;
                double rate2 = r2 != null ? (r2.Weight / total2) * 100.0 : 0;
                double diff = rate2 - rate1;

                Console.WriteLine($"{rarityName,-15} {rate1,-18:F2} {rate2,-18:F2} {diff,-15:+F2;-F2;0.00}");
            }
        }
    }
}

