using System.Text.Json.Serialization;

namespace RPGGame.Config
{
    /// <summary>
    /// Local-only active audio and balance patch selection.
    /// Gameplay/UI prefs and audio bus volume live in gitignored <c>GeneralSettings.json</c>.
    /// Repo <c>default</c> patches update on git pull; named patches stay local.
    /// </summary>
    public sealed class PatchProfile
    {
        public const string DefaultPatchName = "default";

        /// <summary>Legacy field; ignored at runtime — game settings always use <see cref="DefaultPatchName"/>.</summary>
        [JsonPropertyName("activeGameSettingsPatch")]
        public string ActiveGameSettingsPatch { get; set; } = DefaultPatchName;

        [JsonPropertyName("activeAudioPatch")]
        public string ActiveAudioPatch { get; set; } = DefaultPatchName;

        [JsonPropertyName("activeBalancePatch")]
        public string ActiveBalancePatch { get; set; } = DefaultPatchName;

        /// <summary>Audio and balance patch selection is player-local; game settings always use the repo default.</summary>
        public string GetActivePatchName(PatchCategory category) => category switch
        {
            PatchCategory.Audio => ActiveAudioPatch,
            PatchCategory.Balance => ActiveBalancePatch,
            _ => DefaultPatchName
        };

        public void SetActivePatchName(PatchCategory category, string patchName)
        {
            switch (category)
            {
                case PatchCategory.Audio:
                    ActiveAudioPatch = patchName;
                    break;
                case PatchCategory.Balance:
                    ActiveBalancePatch = patchName;
                    break;
            }
        }

        /// <summary>True when the player may choose among multiple patch files for this category.</summary>
        public static bool IsPlayerLocalCategory(PatchCategory category) =>
            category is PatchCategory.Audio or PatchCategory.Balance;
    }
}
