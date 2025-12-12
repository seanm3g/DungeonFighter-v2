using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using RPGGame;
using RPGGame.Config;
using RPGGame.MCP.Tools;

namespace RPGGame.MCP.Tools
{
    /// <summary>
    /// Patch management tools
    /// </summary>
    public static class PatchManagementTools
    {
        [McpServerTool(Name = "save_patch", Title = "Save Patch")]
        [Description("Saves the current configuration as a balance patch that can be shared or loaded later.")]
        public static Task<string> SavePatch(
            [Description("Patch name")] string name,
            [Description("Author name")] string author,
            [Description("Patch description")] string description,
            [Description("Patch version (default: '1.0')")] string version = "1.0",
            [Description("Optional tags (comma-separated)")] string? tags = null)
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var tagList = tags?.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();
                var success = BalanceTuningConsole.SavePatch(name, author, description, version, tagList);

                return new
                {
                    success = success,
                    message = success ? $"Patch '{name}' saved successfully" : $"Failed to save patch '{name}'"
                };
            });
        }

        [McpServerTool(Name = "load_patch", Title = "Load Patch")]
        [Description("Loads and applies a balance patch by patch ID or name.")]
        public static Task<string> LoadPatch(
            [Description("Patch ID or name")] string patchId)
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var success = BalanceTuningConsole.LoadPatch(patchId);
                return new
                {
                    success = success,
                    message = success ? $"Patch '{patchId}' loaded successfully" : $"Failed to load patch '{patchId}'"
                };
            });
        }

        [McpServerTool(Name = "list_patches", Title = "List Patches")]
        [Description("Lists all available balance patches.")]
        public static Task<string> ListPatches()
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var patches = BalancePatchManager.ListPatches();
                var patchList = patches.Select(p => new
                {
                    patchId = p.PatchMetadata.PatchId,
                    name = p.PatchMetadata.Name,
                    author = p.PatchMetadata.Author,
                    description = p.PatchMetadata.Description,
                    version = p.PatchMetadata.Version,
                    createdDate = p.PatchMetadata.CreatedDate,
                    tags = p.PatchMetadata.Tags,
                    testResults = p.PatchMetadata.TestResults != null ? new
                    {
                        averageWinRate = p.PatchMetadata.TestResults.AverageWinRate,
                        battlesTested = p.PatchMetadata.TestResults.BattlesTested,
                        testDate = p.PatchMetadata.TestResults.TestDate
                    } : null
                }).ToList();

                return new
                {
                    count = patchList.Count,
                    patches = patchList
                };
            }, writeIndented: true);
        }

        [McpServerTool(Name = "get_patch_info", Title = "Get Patch Info")]
        [Description("Gets detailed information about a specific patch.")]
        public static Task<string> GetPatchInfo(
            [Description("Patch ID or name")] string patchId)
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var patch = BalancePatchManager.GetPatch(patchId);
                if (patch == null)
                {
                    // Try to find by name
                    var patches = BalancePatchManager.ListPatches();
                    patch = patches.FirstOrDefault(p => p.PatchMetadata.Name == patchId);
                }

                if (patch == null)
                {
                    throw new InvalidOperationException($"Patch '{patchId}' not found");
                }

                return new
                {
                    patchId = patch.PatchMetadata.PatchId,
                    name = patch.PatchMetadata.Name,
                    author = patch.PatchMetadata.Author,
                    description = patch.PatchMetadata.Description,
                    version = patch.PatchMetadata.Version,
                    createdDate = patch.PatchMetadata.CreatedDate,
                    compatibleGameVersion = patch.PatchMetadata.CompatibleGameVersion,
                    tags = patch.PatchMetadata.Tags,
                    testResults = patch.PatchMetadata.TestResults != null ? new
                    {
                        averageWinRate = patch.PatchMetadata.TestResults.AverageWinRate,
                        battlesTested = patch.PatchMetadata.TestResults.BattlesTested,
                        testDate = patch.PatchMetadata.TestResults.TestDate
                    } : null,
                    configuration = new
                    {
                        globalMultipliers = new
                        {
                            health = patch.TuningConfig.EnemySystem.GlobalMultipliers.HealthMultiplier,
                            damage = patch.TuningConfig.EnemySystem.GlobalMultipliers.DamageMultiplier,
                            armor = patch.TuningConfig.EnemySystem.GlobalMultipliers.ArmorMultiplier,
                            speed = patch.TuningConfig.EnemySystem.GlobalMultipliers.SpeedMultiplier
                        }
                    }
                };
            }, writeIndented: true);
        }
    }
}
