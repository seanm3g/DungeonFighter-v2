using Avalonia.Controls;
using RPGGame;
using RPGGame.Data;
using RPGGame.UI.Avalonia.Settings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>Enemies settings tab: catalog with tag editing and JSON persistence.</summary>
    public class EnemiesTabManager
    {
        private readonly EnemiesDataService dataService;
        private readonly Action<string, bool>? showStatusMessage;
        private List<EnemyData>? loadedEnemies;
        private ObservableCollection<EnemySettingsRowViewModel>? rows;
        private EnemiesSettingsPanel? panel;

        public EnemiesTabManager(Action<string, bool>? showStatusMessage = null)
        {
            this.showStatusMessage = showStatusMessage;
            dataService = new EnemiesDataService(showStatusMessage);
        }

        public void Initialize(EnemiesSettingsPanel enemiesPanel)
        {
            panel = enemiesPanel;
            try
            {
                ReloadFromDisk();
                BindItemsSource();
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error loading enemies tab: {ex.Message}", false);
            }
        }

        /// <summary>After external file reload (e.g. spreadsheet sync), refresh this tab if it was already opened.</summary>
        public void RefreshFromFileIfLoaded()
        {
            if (panel == null)
                return;
            try
            {
                ReloadFromDisk();
                BindItemsSource();
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error refreshing enemies tab: {ex.Message}", false);
            }
        }

        private void BindItemsSource()
        {
            if (panel == null)
                return;
            var list = panel.FindControl<ItemsControl>("EnemiesItemsControl");
            if (list != null)
                list.ItemsSource = rows;
        }

        /// <summary>Reload rows from disk (file is source of truth for this tab).</summary>
        public void ReloadFromDisk()
        {
            loadedEnemies = dataService.LoadEnemies();
            foreach (var e in loadedEnemies)
                EnemyDataPostLoad.Apply(e);

            rows = new ObservableCollection<EnemySettingsRowViewModel>();
            for (var i = 0; i < loadedEnemies.Count; i++)
            {
                var e = loadedEnemies[i];
                var tagList = GameDataTagHelper.NormalizeDistinct(e.Tags);
                rows.Add(new EnemySettingsRowViewModel(
                    sourceIndex: i,
                    name: e.Name,
                    archetype: e.Archetype,
                    tagsCommaSeparated: tagList.Count == 0 ? "" : string.Join(", ", tagList)));
            }
        }

        /// <summary>Write tag edits to disk and refresh <see cref="EnemyLoader"/>.</summary>
        public void SaveEnemies()
        {
            if (loadedEnemies == null || rows == null)
            {
                showStatusMessage?.Invoke("No enemy data to save", false);
                return;
            }

            if (loadedEnemies.Count != rows.Count)
            {
                showStatusMessage?.Invoke("Enemy list out of sync; reopen the Enemies tab.", false);
                return;
            }

            try
            {
                foreach (var row in rows)
                {
                    if (row.SourceIndex < 0 || row.SourceIndex >= loadedEnemies.Count)
                        continue;
                    var parsed = GameDataTagHelper.ParseCommaSeparatedTags(row.Tags);
                    loadedEnemies[row.SourceIndex].Tags = parsed.Count == 0 ? null : parsed;
                }

                dataService.SaveEnemies(loadedEnemies);
                EnemyLoader.LoadEnemies();
                showStatusMessage?.Invoke("Enemies saved; runtime enemy data reloaded.", true);
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error saving enemies: {ex.Message}", false);
            }
        }
    }
}
