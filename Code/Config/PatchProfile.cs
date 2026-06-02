using System.Text.Json.Serialization;

namespace RPGGame.Config
{
    /// <summary>
    /// Local-only selection of which patch file is active per category.
    /// Persisted to gitignored <c>GameData/PatchProfile.json</c>.
    /// </summary>
    public sealed class PatchProfile
    {
        public const string DefaultPatchName = "default";

        [JsonPropertyName("activeGameSettingsPatch")]
        public string ActiveGameSettingsPatch { get; set; } = DefaultPatchName;

        [JsonPropertyName("activeAudioPatch")]
        public string ActiveAudioPatch { get; set; } = DefaultPatchName;

        [JsonPropertyName("activeBalancePatch")]
        public string ActiveBalancePatch { get; set; } = DefaultPatchName;

        public string GetActivePatchName(PatchCategory category) => category switch
        {
            PatchCategory.GameSettings => ActiveGameSettingsPatch,
            PatchCategory.Audio => ActiveAudioPatch,
            PatchCategory.Balance => ActiveBalancePatch,
            _ => DefaultPatchName
        };

        public void SetActivePatchName(PatchCategory category, string patchName)
        {
            switch (category)
            {
                case PatchCategory.GameSettings:
                    ActiveGameSettingsPatch = patchName;
                    break;
                case PatchCategory.Audio:
                    ActiveAudioPatch = patchName;
                    break;
                case PatchCategory.Balance:
                    ActiveBalancePatch = patchName;
                    break;
            }
        }
    }
}
