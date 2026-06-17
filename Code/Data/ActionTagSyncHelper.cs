using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.World.Tags;

namespace RPGGame.Data
{
    /// <summary>Syncs structured action fields and sheet columns to canonical registry tags.</summary>
    public static class ActionTagSyncHelper
    {
        public const string WeaponBasicTag = "weapon_basic";

        public static void SyncCanonicalTags(ActionData actionData)
        {
            if (actionData == null)
                return;

            var tags = actionData.Tags ?? new List<string>();

            void Ensure(string tag)
            {
                if (!tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
                    tags.Add(tag);
            }

            void Remove(string tag)
            {
                tags.RemoveAll(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));
            }

            if (actionData.IsOpener)
                Ensure("opener");
            else
                Remove("opener");

            if (actionData.IsFinisher)
                Ensure("finisher");
            else
                Remove("finisher");

            bool isRequired = tags.Any(t => string.Equals(t, WeaponBasicTag, StringComparison.OrdinalIgnoreCase));
            if (isRequired)
                Ensure("required");

            InferMechanicAndRollTags(actionData, Ensure, Remove);

            actionData.Tags = tags
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim().ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static void InferMechanicAndRollTags(ActionData actionData, Action<string> ensure, Action<string> remove)
        {
            if (!string.IsNullOrWhiteSpace(actionData.SpeedMod) && actionData.SpeedMod != "0")
                ensure("swift");
            if (!string.IsNullOrWhiteSpace(actionData.DamageMod) && actionData.DamageMod != "0")
                ensure("bludgeon");
            if (!string.IsNullOrWhiteSpace(actionData.AmpMod) && actionData.AmpMod != "0")
                ensure("focus");
            if (actionData.RollBonus != 0 || actionData.CausesFocus)
                ensure("insight");

            if (actionData.HitThresholdAdjustment != 0)
                ensure("footwork");
            if (actionData.ComboThresholdAdjustment != 0)
                ensure("target");
            if (actionData.CriticalHitThresholdAdjustment != 0)
                ensure("aim");
            if (actionData.CriticalMissThresholdAdjustment != 0)
                ensure("confidence");
        }

        public static bool HasTag(IEnumerable<string>? tags, string tag) =>
            tags != null && tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));

        public static bool IsRequiredBasic(IEnumerable<string>? tags) =>
            HasTag(tags, "required") || HasTag(tags, WeaponBasicTag);
    }
}

