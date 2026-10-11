using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using RPGGame.Config;
using RPGGame.Tuning;

namespace RPGGame.UI.Avalonia.Settings.ViewModels
{
    /// <summary>View model for Combat Tuning: Starting / Scaling / Outfit (+ Simulation tool).</summary>
    public sealed class CombatTuningPanelViewModel
    {
        private readonly Dictionary<string, CombatTuningParameterViewModel> byId;

        public CombatTuningPanelViewModel(
            IReadOnlyDictionary<CombatTuningTab, ObservableCollection<CombatTuningSubGroupViewModel>> subGroupsByTab,
            Dictionary<string, CombatTuningParameterViewModel> byId,
            ArchetypeTuningViewModel archetypeTuning,
            StatusEffectTuningViewModel statusEffectTuning)
        {
            SubGroupsByTab = subGroupsByTab;
            this.byId = byId;
            ArchetypeTuning = archetypeTuning;
            StatusEffectTuning = statusEffectTuning;

            StartingSubGroups = GetSubGroups(CombatTuningTab.Starting) ?? new ObservableCollection<CombatTuningSubGroupViewModel>();
            ScalingSubGroups = GetSubGroups(CombatTuningTab.Scaling) ?? new ObservableCollection<CombatTuningSubGroupViewModel>();
            OutfitSubGroups = FilterOutfitStandardSubGroups(
                GetSubGroups(CombatTuningTab.Outfit) ?? new ObservableCollection<CombatTuningSubGroupViewModel>());
        }

        public ObservableCollection<CombatTuningSubGroupViewModel> StartingSubGroups { get; }
        public ObservableCollection<CombatTuningSubGroupViewModel> ScalingSubGroups { get; }
        public ObservableCollection<CombatTuningSubGroupViewModel> OutfitSubGroups { get; }
        public ProgressionCurvePreviewViewModel ProgressionPreview { get; } = new();

        /// <summary>Shown at top of Combat Tuning so saves target the active balance patch file.</summary>
        public string ActiveBalancePatchLabel { get; private set; } = BuildActiveBalancePatchLabel();

        public void RefreshActiveBalancePatchLabel()
        {
            ActiveBalancePatchLabel = BuildActiveBalancePatchLabel();
        }

        private static string BuildActiveBalancePatchLabel()
        {
            string patch = PatchProfileService.LoadProfile().GetActivePatchName(PatchCategory.Balance);
            return string.IsNullOrWhiteSpace(patch)
                ? "Active balance patch: (none)"
                : $"Active balance patch: {patch} — Save writes to GameData/Patches/Balance/{patch}.json";
        }

        public IReadOnlyDictionary<CombatTuningTab, ObservableCollection<CombatTuningSubGroupViewModel>> SubGroupsByTab { get; }
        public ArchetypeTuningViewModel ArchetypeTuning { get; }
        public StatusEffectTuningViewModel StatusEffectTuning { get; }

        public static CombatTuningPanelViewModel FromRegistry()
        {
            CombatTuningParameterRegistry.EnsureSanitizedDefaults();

            var byId = new Dictionary<string, CombatTuningParameterViewModel>(StringComparer.OrdinalIgnoreCase);
            var rowsByTab = new Dictionary<CombatTuningTab, List<CombatTuningParameterViewModel>>();

            foreach (CombatTuningTab tab in Enum.GetValues(typeof(CombatTuningTab)))
                rowsByTab[tab] = new List<CombatTuningParameterViewModel>();

            foreach (var param in CombatTuningParameterRegistry.All)
            {
                var row = new CombatTuningParameterViewModel(param);
                rowsByTab[param.Tab].Add(row);
                byId[param.Id] = row;
            }

            var subGroupsByTab = new Dictionary<CombatTuningTab, ObservableCollection<CombatTuningSubGroupViewModel>>();
            foreach (var (tab, rows) in rowsByTab)
            {
                var subGroupIndex = CombatTuningParameterRegistry.GetSubGroupsForTab(tab)
                    .Select((name, index) => (name, index))
                    .ToDictionary(x => x.name, x => x.index);
                var groups = rows
                    .GroupBy(r => CombatTuningParameterRegistry.GetById(r.Id)?.SubGroup ?? "")
                    .OrderBy(g => subGroupIndex.TryGetValue(g.Key, out var index) ? index : int.MaxValue)
                    .Select(g => new CombatTuningSubGroupViewModel(
                        g.Key,
                        new ObservableCollection<CombatTuningParameterViewModel>(g)))
                    .ToList();
                subGroupsByTab[tab] = new ObservableCollection<CombatTuningSubGroupViewModel>(groups);
            }

            var outfitRows = rowsByTab[CombatTuningTab.Outfit];
            var archetypeRows = outfitRows.Where(r => r.Id.StartsWith("arch_", StringComparison.OrdinalIgnoreCase)).ToList();
            var statusRows = outfitRows.Where(r =>
                r.Id.StartsWith("status", StringComparison.OrdinalIgnoreCase)
                || r.Id.StartsWith("fx_", StringComparison.OrdinalIgnoreCase)).ToList();
            var globalStatus = statusRows.Where(r => string.IsNullOrEmpty(r.FilterKey)).ToList();
            var perEffectStatus = statusRows.Where(r => !string.IsNullOrEmpty(r.FilterKey)).ToList();

            var vm = new CombatTuningPanelViewModel(
                subGroupsByTab,
                byId,
                new ArchetypeTuningViewModel(archetypeRows),
                new StatusEffectTuningViewModel(globalStatus, perEffectStatus));

            vm.ReloadFromConfig();
            vm.RefreshActiveBalancePatchLabel();
            vm.WireRollFeelVarianceCompressionMasterSlider();
            vm.WireProgressionCurveRefresh();
            vm.ProgressionPreview.Refresh();
            return vm;
        }

        /// <summary>Outfit list view excludes archetype/status rows (shown via selectors).</summary>
        private static ObservableCollection<CombatTuningSubGroupViewModel> FilterOutfitStandardSubGroups(
            ObservableCollection<CombatTuningSubGroupViewModel> groups)
        {
            var filtered = groups
                .Where(g =>
                    !string.Equals(g.Name, "Archetypes", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(g.Name, "Status — per effect", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(g.Name, "Status — global", StringComparison.OrdinalIgnoreCase))
                .ToList();
            return new ObservableCollection<CombatTuningSubGroupViewModel>(filtered);
        }

        internal void WireProgressionCurveRefresh()
        {
            foreach (var row in byId.Values)
            {
                var id = row.Id;
                if (!IsProgressionCurveKnob(id))
                    continue;

                row.SetValueCommittedHandler(_ => RefreshProgressionPreview());
            }
        }

        private static bool IsProgressionCurveKnob(string id)
        {
            return id is "combatTempoScale" or "progressionShape" or "playerEnemyParity" or "progressionPivotLevel"
                or "playerBaseHealth" or "playerHealthPerLevel" or "baseHealthScale" or "healthGrowthScale"
                or "attributeGrowthScale";
        }

        public void RefreshProgressionPreview()
        {
            ApplyProgressionCurveKnobsForPreview();
            ProgressionPreview.Refresh();
        }

        private void ApplyProgressionCurveKnobsForPreview()
        {
            foreach (var row in byId.Values)
            {
                if (IsProgressionCurveKnob(row.Id))
                    row.CommitToConfig();
            }
        }

        internal void WireRollFeelVarianceCompressionMasterSlider()
        {
            if (!byId.TryGetValue(RollFeelVarianceCompression.MasterParameterId, out var masterRow))
                return;

            masterRow.SetValueCommittedHandler(compression =>
            {
                RollFeelVarianceCompression.Apply(compression);
                masterRow.CommitToConfig();
                ReloadRollFeelLinkedParameters();
            });
        }

        public void ReloadRollFeelLinkedParameters()
        {
            foreach (string id in RollFeelVarianceCompression.DrivenParameterIds)
            {
                if (byId.TryGetValue(id, out var row))
                    row.ReloadFromConfig();
            }
        }

        public ObservableCollection<CombatTuningSubGroupViewModel>? GetSubGroups(CombatTuningTab tab) =>
            SubGroupsByTab.TryGetValue(tab, out var groups) ? groups : null;

        public void ReloadFromConfig()
        {
            CombatTuningParameterRegistry.EnsureSanitizedDefaults();
            foreach (var row in byId.Values)
                row.ReloadFromConfig();
            RefreshActiveBalancePatchLabel();
            RefreshProgressionPreview();
        }

        public void FlushPendingEdits()
        {
            foreach (var row in byId.Values)
                row.FlushPendingText();
        }

        public void CommitAllToConfig()
        {
            FlushPendingEdits();
            foreach (var row in byId.Values)
                row.CommitToConfig();
        }

        public CombatTuningParameterViewModel? GetById(string id) =>
            byId.TryGetValue(id, out var row) ? row : null;

        public int TotalParameterCount => byId.Count;

        public int CountForTab(CombatTuningTab tab) =>
            SubGroupsByTab.TryGetValue(tab, out var groups)
                ? groups.Sum(g => g.Parameters.Count)
                : 0;
    }
}
