using System;
using System.Collections.Generic;

namespace RPGGame.Combat.Sequence
{
    /// <summary>
    /// Maps action names to F7 narrative text effects. Seeded in code for v1
    /// (no Actions.json UI schema).
    /// </summary>
    public static class ActionTextEffectCatalog
    {
        private static readonly Dictionary<string, ActionTextEffectKind> ByActionName =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["SLAM"] = ActionTextEffectKind.DistortionPulse
            };

        public static ActionTextEffectKind Resolve(string? actionName)
        {
            if (string.IsNullOrWhiteSpace(actionName))
                return ActionTextEffectKind.None;

            return ByActionName.TryGetValue(actionName.Trim(), out var kind)
                ? kind
                : ActionTextEffectKind.None;
        }

        public static bool IsDistortionPulse(string? actionName) =>
            Resolve(actionName) == ActionTextEffectKind.DistortionPulse;
    }
}
