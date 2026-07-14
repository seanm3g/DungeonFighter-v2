using System;
using System.Collections.Generic;

namespace RPGGame.ActionInteractionLab
{
    /// <summary>
    /// On-disk Action Lab character preset: campaign save JSON plus combo strip names
    /// (strip is not stored in <see cref="Entity.Save.CharacterSaveData"/>).
    /// </summary>
    public sealed class CharacterLabSnapshotData
    {
        public string DisplayName { get; set; } = "";

        public DateTime CreatedUtc { get; set; }

        /// <summary>JSON from <see cref="Entity.Services.CharacterSerializer.Serialize"/>.</summary>
        public string CharacterJson { get; set; } = "";

        public List<string> ComboStripActionNames { get; set; } = new();

        public string? Notes { get; set; }
    }
}
