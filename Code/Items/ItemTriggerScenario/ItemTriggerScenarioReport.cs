using System;
using System.Collections.Generic;
using RPGGame;

namespace RPGGame.Items.ItemTriggerScenario
{
    /// <summary>Structured result of one item-trigger scenario (setup → forced WHEN → buff deltas).</summary>
    public sealed class ItemTriggerScenarioReport
    {
        public int IdentityIndex { get; set; }
        public string IdentityName { get; set; } = "";
        public string Description { get; set; } = "";
        public string When { get; set; } = "";
        public string Scope { get; set; } = "";
        public string Mechanics { get; set; } = "";
        public double? Value { get; set; }
        public string? ScaleFrom { get; set; }
        public IReadOnlyList<string> Filters { get; set; } = Array.Empty<string>();
        public string Channel { get; set; } = "combat";

        public string Path { get; set; } = "";
        public int ForcedD20 { get; set; }
        public IReadOnlyList<string> SetupNotes { get; set; } = Array.Empty<string>();

        public bool OutcomeMatched { get; set; } = true;
        public string? OutcomeDetail { get; set; }

        public ItemTriggerBuffSnapshot? Before { get; set; }
        public ItemTriggerBuffSnapshot? After { get; set; }
        public IReadOnlyList<string> BuffDeltaLines { get; set; } = Array.Empty<string>();

        public bool Passed { get; set; }
        public string Finding { get; set; } = "";
        public string? Error { get; set; }

        public IReadOnlyList<string> StatusMessages { get; set; } = Array.Empty<string>();
        public int DamageDealt { get; set; }
        public int NestedRetriggerCount { get; set; }
        public bool Hit { get; set; }
        public bool IsCritical { get; set; }
        public bool IsCombo { get; set; }
        public bool IsCriticalMiss { get; set; }
    }

    /// <summary>Observable combat banks / statuses captured before or after a scenario swing.</summary>
    public sealed class ItemTriggerBuffSnapshot
    {
        public int HeroHp { get; set; }
        public int HeroMaxHp { get; set; }
        public int ActionBankCount { get; set; }
        public int TurnBonusCount { get; set; }
        public int FightBonusCount { get; set; }
        public int DungeonBonusCount { get; set; }
        public int HardenTurns { get; set; }
        public int FocusTurns { get; set; }
        public int FortifyTurns { get; set; }
        public int MissSalvageCharges { get; set; }
        public bool HasCritFaceMin { get; set; }
        public int StripDisabledCount { get; set; }
        public bool StripHasPendingRouting { get; set; }
        public bool StripHasReplace { get; set; }
        public bool StripHasShuffle { get; set; }
        public double ConsumedDamageModPercent { get; set; }
        public double ConsumedSpeedModPercent { get; set; }
        public double ConsumedAmpModPercent { get; set; }
        public int EquipArmorBonus { get; set; }
        public int EquipPrimaryStatBonus { get; set; }
        public int GrantedActionCount { get; set; }
        public int GrantedTagCount { get; set; }
    }

    /// <summary>Batch summary for CLI / Lab Run All.</summary>
    public sealed class ItemTriggerScenarioBatchResult
    {
        public IReadOnlyList<ItemTriggerScenarioReport> Reports { get; set; } = Array.Empty<ItemTriggerScenarioReport>();
        public int Passed { get; set; }
        public int Failed { get; set; }
        public int Total => Passed + Failed;
    }
}
