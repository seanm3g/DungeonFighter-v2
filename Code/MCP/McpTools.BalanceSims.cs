using System.ComponentModel;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using Tools = RPGGame.MCP.Tools;

namespace RPGGame.MCP
{
    /// <summary>
    /// MCP tools for the current balance pipeline (real-dungeon DUNGEONSIM sims, Bayesian anchor solve, quick scan,
    /// median-build check). Each start_* tool launches a background job; poll get_balance_job.
    /// </summary>
    public static partial class McpTools
    {
        [McpServerTool(Name = "get_balance_context", Title = "Get Balance Context")]
        [Description("START HERE for balance work. Active balance patch, clear-rate and fight-length targets, local settings that skew sims (GeneralSettings enemyHealthMultiplier), and the recommended workflow.")]
        public static Task<string> GetBalanceContext() => Tools.BalanceSimTools.GetBalanceContext();

        [McpServerTool(Name = "start_dungeon_sim", Title = "Start Dungeon Sim")]
        [Description("Background job: real dungeons (DUNGEONSIM via Scripts/dungeon-sim.py) at fixed hero levels, all classes, HP carried between fights, retreat rule, rules bot, seeded. Reports clear/retreat/death % per level and class. Use a new seed to confirm a change and run base and candidate patches on the same seed; >= 8 shards (16 heroes/class) to read class gaps.")]
        public static Task<string> StartDungeonSim(
            [Description("Comma list of hero levels, e.g. '8,13,18' (prefer region-band midpoints)")] string levels = "10",
            [Description("Balance patch name in GameData/Patches/Balance (default: active patch)")] string? patch = null,
            [Description("Processes per class (default 6; 8+ for class comparisons)")] int shards = 6,
            [Description("Heroes per shard (default 2)")] int runs = 2,
            [Description("Dungeons per hero (default 6)")] int dungeons = 6,
            [Description("Base seed; same seed replays the same heroes (default 1)")] int seed = 1,
            [Description("Optional comma list of classes")] string? classes = null,
            [Description("Bot: rules (default, fast), smart (~40x slower), basic")] string? bot = null,
            [Description("Spread runs across region themes")] bool regionMix = false,
            [Description("Start every run in this region theme: Forest, Water, Fire, Air")] string? regionTheme = null,
            [Description("Damage anchor overrides 'L=multiplier,...' for a quick what-if")] string? anchors = null,
            [Description("Folder with DF.dll (default: this server's build)")] string? toolsDir = null)
            => Tools.BalanceSimTools.StartDungeonSim(levels, patch, shards, runs, dungeons, seed, classes, bot, regionMix, regionTheme, anchors, toolsDir);

        [McpServerTool(Name = "start_bo_solve", Title = "Start Bayesian Damage Solve")]
        [Description("Background job: Scripts/bo-solve.py solves the enemy damage anchor (whole 5-level band by default) for a target dungeon clear rate. Run at a band midpoint (8, 13, 18, 33, 48...). Prints the band anchors to write into a NEW candidate patch; confirm with start_dungeon_sim on a fresh seed.")]
        public static Task<string> StartBoSolve(
            [Description("Hero level (band midpoint)")] int level,
            [Description("Target clear % (default: realClearAnchors at this level)")] double? target = null,
            [Description("Balance patch name (default: active patch)")] string? patch = null,
            [Description("band (default) or level")] string? scope = null,
            [Description("DGX Spark slots over ssh (0 = local only)")] int spark = 0,
            [Description("Seed base; each round uses fresh heroes (default 11)")] int seed = 11,
            [Description("Stop when the 90% interval is within +/- this fraction (default 0.10)")] double tolerance = 0.10,
            [Description("Max rounds (default 12)")] int maxRounds = 12,
            [Description("Other damage anchor overrides 'L=multiplier,...'")] string? anchors = null,
            [Description("Folder with DF.dll (default: this server's build)")] string? toolsDir = null)
            => Tools.BalanceSimTools.StartBoSolve(level, target, patch, scope, spark, seed, tolerance, maxRounds, anchors, toolsDir);

        [McpServerTool(Name = "start_balance_scan", Title = "Start Balance Scan")]
        [Description("Background job (~8 min): Scripts/balance-scan.py. Median catalog build grid (BUILDLAB) at every matrix level plus a small DUNGEONSIM sample; ranked problem areas with the focus command to run next. Dungeon rows are a few heroes: gross misses only.")]
        public static Task<string> StartBalanceScan(
            [Description("Balance patch name (default: active patch)")] string? patch = null,
            [Description("Comma list of median-build levels (default: all matrix levels)")] string? medianLevels = null,
            [Description("Comma list of DUNGEONSIM levels; band midpoints recommended (default '8,18,33,48'; empty string skips)")] string? dungeonLevels = "8,18,33,48",
            [Description("Heroes per class per level (default 2)")] int heroes = 2,
            [Description("Earlier scan JSON to compare against")] string? compare = null,
            [Description("Folder with DF.dll (default: this server's build)")] string? toolsDir = null)
            => Tools.BalanceSimTools.StartBalanceScan(patch, medianLevels, dungeonLevels, heroes, compare, toolsDir);

        [McpServerTool(Name = "start_median_check", Title = "Start Median Build Check")]
        [Description("Background job: Scripts/median-curve-solve.py --check. Median catalog build (BUILDLAB) hero actions per fight vs heroTurnsAnchors per level. Changes nothing.")]
        public static Task<string> StartMedianCheck(
            [Description("Balance patch name (default: active patch)")] string? patch = null,
            [Description("Comma list of levels (default: every anchor level)")] string? levels = null,
            [Description("Folder with DF.dll (default: this server's build)")] string? toolsDir = null)
            => Tools.BalanceSimTools.StartMedianCheck(patch, levels, toolsDir);

        [McpServerTool(Name = "get_balance_job", Title = "Get Balance Job")]
        [Description("Status, output tail and (when done) the result JSON of a balance job started with start_*.")]
        public static Task<string> GetBalanceJob(
            [Description("Job id from start_*")] string jobId,
            [Description("Output lines to return (default 40)")] int tailLines = 40)
            => Tools.BalanceSimTools.GetBalanceJob(jobId, tailLines);

        [McpServerTool(Name = "list_balance_jobs", Title = "List Balance Jobs")]
        [Description("Balance jobs started in this server session.")]
        public static Task<string> ListBalanceJobs() => Tools.BalanceSimTools.ListBalanceJobs();

        [McpServerTool(Name = "cancel_balance_job", Title = "Cancel Balance Job")]
        [Description("Kills a running balance job and its sim processes.")]
        public static Task<string> CancelBalanceJob([Description("Job id")] string jobId)
            => Tools.BalanceSimTools.CancelBalanceJob(jobId);
    }
}
