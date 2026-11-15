using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using ModelContextProtocol.Server;

namespace RPGGame.MCP.Tools.LootTuning
{
    /// <summary>
    /// MCP tool: rarity table read-out (weights, unlock level, line counts per pool)
    /// Extracted from LootTuningTools to separate rarity tuning logic
    /// </summary>
    public static class RarityTuningTools
    {
        [McpServerTool(Name = "get_rarity_distribution", Title = "Get Rarity Distribution")]
        [Description("Gets current rarity weights and calculates probability percentages.")]
        public static Task<string> GetRarityDistribution()
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var dataCache = LootDataCache.Load();
                double totalWeight = dataCache.RarityData.Sum(r => r.Weight);

                var distributions = dataCache.RarityData.Select(r => new
                {
                    name = r.Name,
                    weight = r.Weight,
                    probability = totalWeight > 0 ? (r.Weight / totalWeight) * 100 : 0,
                    minLevel = r.MinLevel,
                    minLines = r.MinLines,
                    maxLines = r.MaxLines,
                    explode = r.Explode
                }).ToList();

                return new
                {
                    totalWeight = totalWeight,
                    rarities = distributions
                };
            }, writeIndented: true);
        }
    }
}

