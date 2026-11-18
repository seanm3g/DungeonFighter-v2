using System;

namespace RPGGame
{
    /// <summary>
    /// The dev build flag (core design pass D1.5, D7.9). Developer screens (Testing, DeveloperMenu,
    /// BattleStatistics, VariableEditor, TuningParameters, the five action editor states, ActionInteractionLab),
    /// the developer / balance / testing settings panels and the death-screen clone exist only in a dev build.
    /// <para>Debug builds are dev builds and Release builds are player builds. The environment variable
    /// <c>DF_DEV_BUILD</c> overrides that either way (<c>1</c> / <c>true</c> = dev, <c>0</c> / <c>false</c> = player),
    /// so a Debug build can be checked as a player sees it.</para>
    /// </summary>
    public static class DevBuild
    {
        public const string EnvironmentVariable = "DF_DEV_BUILD";

        private static bool? _override;
        private static bool? _resolved;

        /// <summary>True when developer screens and tools are available.</summary>
        public static bool IsEnabled => _override ?? (_resolved ??= Resolve());

        /// <summary>Forces the flag (tests). Pass null to go back to the build / environment default.</summary>
        public static void SetOverride(bool? enabled) => _override = enabled;

        private static bool Resolve()
        {
            string? env = null;
            try { env = System.Environment.GetEnvironmentVariable(EnvironmentVariable); }
            catch { /* no environment access: fall through to the build default */ }

            if (!string.IsNullOrWhiteSpace(env))
            {
                string v = env.Trim();
                if (v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase) || v.Equals("yes", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (v == "0" || v.Equals("false", StringComparison.OrdinalIgnoreCase) || v.Equals("no", StringComparison.OrdinalIgnoreCase))
                    return false;
            }

#if DEBUG
            return true;
#else
            return false;
#endif
        }

        /// <summary>The 11 developer states that only a dev build can enter.</summary>
        public static bool IsDevOnlyState(GameState state) => state switch
        {
            GameState.Testing => true,
            GameState.DeveloperMenu => true,
            GameState.BattleStatistics => true,
            GameState.VariableEditor => true,
            GameState.TuningParameters => true,
            GameState.ActionEditor => true,
            GameState.CreateAction => true,
            GameState.EditAction => true,
            GameState.ViewAction => true,
            GameState.DeleteActionConfirmation => true,
            GameState.ActionInteractionLab => true,
            _ => false
        };

        /// <summary>True when <paramref name="state"/> may be entered in this build.</summary>
        public static bool AllowsState(GameState state) => IsEnabled || !IsDevOnlyState(state);

        /// <summary>The death-screen clone is a dev tool for now (D7.9).</summary>
        public static bool AllowsClone => IsEnabled;
    }
}
