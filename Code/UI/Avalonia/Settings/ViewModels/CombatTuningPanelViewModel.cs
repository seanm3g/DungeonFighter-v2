using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using RPGGame.Tuning;

namespace RPGGame.UI.Avalonia.Settings.ViewModels
{
    /// <summary>View model for the Combat Tuning parameters tab (tabbed registry sections).</summary>
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

            // Legacy layer collections for Core tab backward compatibility
            DurationParameters = CollectCoreLayer(CombatTuningLayer.Duration);
            WinRateParameters = CollectCoreLayer(CombatTuningLayer.WinRate);
            RollFeelParameters = CollectCoreLayer(CombatTuningLayer.RollFeel);
            ComboAffordanceParameters = CollectCoreLayer(CombatTuningLayer.ComboAffordance);
            GoalsParameters = CollectCoreLayer(CombatTuningLayer.Goals);

            CoreSubGroups = GetSubGroups(CombatTuningTab.Core) ?? new ObservableCollection<CombatTuningSubGroupViewModel>();
            HeroSubGroups = GetSubGroups(CombatTuningTab.HeroClasses) ?? new ObservableCollection<CombatTuningSubGroupViewModel>();
            SpeedSubGroups = GetSubGroups(CombatTuningTab.SpeedDefense) ?? new ObservableCollection<CombatTuningSubGroupViewModel>();
            EquipmentSubGroups = GetSubGroups(CombatTuningTab.Equipment) ?? new ObservableCollection<CombatTuningSubGroupViewModel>();
            EnemySubGroups = GetSubGroups(CombatTuningTab.EnemyStats) ?? new ObservableCollection<CombatTuningSubGroupViewModel>();
            RewardsSubGroups = GetSubGroups(CombatTuningTab.RewardsLoot) ?? new ObservableCollection<CombatTuningSubGroupViewModel>();
            GoalsAnalysisSubGroups = GetSubGroups(CombatTuningTab.GoalsAnalysis) ?? new ObservableCollection<CombatTuningSubGroupViewModel>();
        }

        public ObservableCollection<CombatTuningSubGroupViewModel> CoreSubGroups { get; }
        public ObservableCollection<CombatTuningSubGroupViewModel> HeroSubGroups { get; }
        public ObservableCollection<CombatTuningSubGroupViewModel> SpeedSubGroups { get; }
        public ObservableCollection<CombatTuningSubGroupViewModel> EquipmentSubGroups { get; }
        public ObservableCollection<CombatTuningSubGroupViewModel> EnemySubGroups { get; }
        public ObservableCollection<CombatTuningSubGroupViewModel> RewardsSubGroups { get; }
        public ObservableCollection<CombatTuningSubGroupViewModel> GoalsAnalysisSubGroups { get; }

        public IReadOnlyDictionary<CombatTuningTab, ObservableCollection<CombatTuningSubGroupViewModel>> SubGroupsByTab { get; }
        public ArchetypeTuningViewModel ArchetypeTuning { get; }
        public StatusEffectTuningViewModel StatusEffectTuning { get; }

        public ObservableCollection<CombatTuningParameterViewModel> DurationParameters { get; }
        public ObservableCollection<CombatTuningParameterViewModel> WinRateParameters { get; }
        public ObservableCollection<CombatTuningParameterViewModel> RollFeelParameters { get; }
        public ObservableCollection<CombatTuningParameterViewModel> ComboAffordanceParameters { get; }
        public ObservableCollection<CombatTuningParameterViewModel> GoalsParameters { get; }

        public static CombatTuningPanelViewModel FromRegistry()
        {
            CombatTuningParameterRegistry.EnsureSanitizedDefaults();

            var byId = new Dictionary<string, CombatTuningParameterViewModel>(StringComparer.OrdinalIgnoreCase);
            var rowsByTab = new Dictionary<CombatTuningTab, List<CombatTuningParameterViewModel>>();

            foreach (CombatTuningTab tab in Enum.GetValues(typeof(CombatTuningTab)))
                rowsByTab[tab] = new List<CombatTuningParameterViewModel>();

            foreach (var param in CombatTuningParameterRegistry.All)
            {
                var row = new CombatTuningParameterViewModel(param, v => param.SetValue(v));
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

            var archetypeRows = rowsByTab[CombatTuningTab.Archetypes];
            var statusRows = rowsByTab[CombatTuningTab.StatusEffects];
            var globalStatus = statusRows.Where(r => string.IsNullOrEmpty(r.FilterKey)).ToList();
            var perEffectStatus = statusRows.Where(r => !string.IsNullOrEmpty(r.FilterKey)).ToList();

            var vm = new CombatTuningPanelViewModel(
                subGroupsByTab,
                byId,
                new ArchetypeTuningViewModel(archetypeRows),
                new StatusEffectTuningViewModel(globalStatus, perEffectStatus));

            vm.ReloadFromConfig();
            vm.WireRollFeelVarianceCompressionMasterSlider();
            return vm;
        }

        /// <summary>
        /// When the variance compression master slider moves, refresh driven sub-parameter rows in the UI.
        /// </summary>
        internal void WireRollFeelVarianceCompressionMasterSlider()
        {
            if (!byId.TryGetValue(RollFeelVarianceCompression.MasterParameterId, out var masterRow))
                return;

            masterRow.SetValueCommittedHandler(compression =>
            {
                RollFeelVarianceCompression.Apply(compression);
                ReloadRollFeelLinkedParameters();
            });
        }

        /// <summary>Reload UI rows driven by the variance compression master slider.</summary>
        public void ReloadRollFeelLinkedParameters()
        {
            foreach (string id in RollFeelVarianceCompression.DrivenParameterIds)
            {
                if (byId.TryGetValue(id, out var row))
                    row.ReloadFromConfig();
            }
        }

        private ObservableCollection<CombatTuningParameterViewModel> CollectCoreLayer(CombatTuningLayer layer)
        {
            var result = new ObservableCollection<CombatTuningParameterViewModel>();
            if (!SubGroupsByTab.TryGetValue(CombatTuningTab.Core, out var groups))
                return result;

            foreach (var group in groups)
            {
                foreach (var row in group.Parameters)
                {
                    var param = CombatTuningParameterRegistry.GetById(row.Id);
                    if (param?.Layer == layer)
                        result.Add(row);
                }
            }
            return result;
        }

        public ObservableCollection<CombatTuningSubGroupViewModel>? GetSubGroups(CombatTuningTab tab) =>
            SubGroupsByTab.TryGetValue(tab, out var groups) ? groups : null;

        public void ReloadFromConfig()
        {
            CombatTuningParameterRegistry.EnsureSanitizedDefaults();
            foreach (var row in byId.Values)
                row.ReloadFromConfig();
        }

        public void CommitAllToConfig()
        {
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

        public int CountForLayer(CombatTuningLayer layer) =>
            layer switch
            {
                CombatTuningLayer.Duration => DurationParameters.Count,
                CombatTuningLayer.WinRate => WinRateParameters.Count,
                CombatTuningLayer.RollFeel => RollFeelParameters.Count,
                CombatTuningLayer.ComboAffordance => ComboAffordanceParameters.Count,
                CombatTuningLayer.Goals => GoalsParameters.Count,
                _ => 0
            };
    }
}
