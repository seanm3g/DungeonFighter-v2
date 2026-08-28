using System;
using System.Collections.Generic;

namespace RPGGame.Data
{
    /// <summary>Obsolete: animal suffix catalog replaced by <see cref="RPGGame.MaterialTriggerCatalog"/>.</summary>
    [Obsolete("Use MaterialTriggerCatalog.")]
    public readonly struct AnimalSuffixDef
    {
        public AnimalSuffixDef(string suffixName, string triggerName, string taxon, string description,
            string when, string mechanics, double value, string channel = "combat", string scope = "TURN",
            string? filters = null, string effectTarget = "hero")
        {
            SuffixName = suffixName;
            TriggerName = triggerName;
            Taxon = taxon;
            Description = description;
            When = when;
            Mechanics = mechanics;
            Value = value;
            Channel = channel;
            Scope = scope;
            Filters = filters;
            EffectTarget = effectTarget;
        }

        public string SuffixName { get; }
        public string TriggerName { get; }
        public string Taxon { get; }
        public string Description { get; }
        public string When { get; }
        public string Mechanics { get; }
        public double Value { get; }
        public string Channel { get; }
        public string Scope { get; }
        public string? Filters { get; }
        public string EffectTarget { get; }
    }

    /// <summary>Obsolete empty catalog — material pools own triggers.</summary>
    [Obsolete("Use MaterialTriggerCatalog.")]
    public static class AnimalSuffixCatalog
    {
        public static IReadOnlyList<AnimalSuffixDef> All { get; } = Array.Empty<AnimalSuffixDef>();

        public static IReadOnlyList<TriggerIdentityData> BuildTaxonSynergies(int startingId) =>
            Array.Empty<TriggerIdentityData>();
    }
}
