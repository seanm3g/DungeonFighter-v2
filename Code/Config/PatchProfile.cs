using System.Text.Json.Serialization;

namespace RPGGame.Config
{
    /// <summary>
    /// Local-only active audio patch selection and general settings path.
    /// Gameplay/UI prefs and audio bus volume live in gitignored <c>GeneralSettings.json</c>.
    /// Balance always uses the repo <c>default</c> patch so it updates on git pull.
    /// </summary>
    public sealed class PatchProfile
    {
        public const string DefaultPatchName = "default";

        /// <summary>Legacy field; ignored at runtime — game settings always use <see cref="DefaultPatchName"/>.</summary>
        [JsonPropertyName("activeGameSettingsPatch")]
        public string ActiveGameSettingsPatch { get; set; } = DefaultPatchName;

        [JsonPropertyName("activeAudioPatch")]
        public string ActiveAudioPatch { get; set; } = DefaultPatchName;

        /// <summary>Legacy field; ignored at runtime — balance always uses <see cref="DefaultPatchName"/>.</summary>
        [JsonPropertyName("activeBalancePatch")]
        public string ActiveBalancePatch { get; set; } = DefaultPatchName;

        /// <summary>Only audio patch selection is player-local; other categories always use the repo default.</summary>
        public string GetActivePatchName(PatchCategory category) => category switch
        {
            PatchCategory.Audio => ActiveAudioPatch,
            _ => DefaultPatchName
        };

        public void SetActivePatchName(PatchCategory category, string patchName)
        {
            if (category != PatchCategory.Audio)
                return;
            ActiveAudioPatch = patchName;
        }

        /// <summary>True when the player may choose among multiple patch files for this category.</summary>
        public static bool IsPlayerLocalCategory(PatchCategory category) =>
            category == PatchCategory.Audio;
    }
}
