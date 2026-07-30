using System;

namespace RPGGame.Items.ItemTriggerScenario
{
    /// <summary>Selects which catalog identity (and optional filter) to exercise.</summary>
    public sealed class ItemTriggerScenarioRequest
    {
        /// <summary>Catalog index (Triggers.json <c>id</c> / stamp index). Ignored when <see cref="IdentityName"/> is set.</summary>
        public int? IdentityIndex { get; set; }

        /// <summary>Exact or substring match against identity name (case-insensitive).</summary>
        public string? IdentityName { get; set; }

        /// <summary>Optional substring filter for WHEN / mechanics / name when batching.</summary>
        public string? Filter { get; set; }

        /// <summary>When true, report includes verbose setup notes and buff lines.</summary>
        public bool Verbose { get; set; } = true;
    }
}
