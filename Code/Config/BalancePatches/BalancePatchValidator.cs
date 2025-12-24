using System.Collections.Generic;
using RPGGame.Config.BalancePatches;

namespace RPGGame.Config.BalancePatches
{
    /// <summary>
    /// Handles validation of balance patches
    /// Extracted from BalancePatchManager to separate validation logic
    /// </summary>
    public static class BalancePatchValidator
    {
        /// <summary>
        /// Validate patch structure and compatibility
        /// </summary>
        public static BalancePatchMetadata.ValidationResult ValidatePatch(BalancePatchMetadata.BalancePatch patch)
        {
            var result = new BalancePatchMetadata.ValidationResult { IsValid = true };
            string gameVersion = BalancePatchMetadata.GetGameVersion();

            // Check metadata
            if (string.IsNullOrWhiteSpace(patch.PatchMetadata.Name))
            {
                result.Errors.Add("Patch name is required");
                result.IsValid = false;
            }

            if (string.IsNullOrWhiteSpace(patch.PatchMetadata.Author))
            {
                result.Warnings.Add("Patch author is not specified");
            }

            if (string.IsNullOrWhiteSpace(patch.PatchMetadata.Description))
            {
                result.Warnings.Add("Patch description is not specified");
            }

            // Check game version compatibility
            if (!string.IsNullOrWhiteSpace(patch.PatchMetadata.CompatibleGameVersion))
            {
                if (patch.PatchMetadata.CompatibleGameVersion != gameVersion)
                {
                    result.Warnings.Add($"Patch was created for game version {patch.PatchMetadata.CompatibleGameVersion}, current version is {gameVersion}");
                }
            }

            // Check tuning config exists
            if (patch.TuningConfig == null)
            {
                result.Errors.Add("Tuning configuration is missing");
                result.IsValid = false;
            }

            return result;
        }
    }
}

