using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RPGGame.Data
{
    /// <summary>
    /// Enemy mechanic kits (<c>GameData/EnemyActions.json</c>): a small set of template actions (Venom, Scorch, Crush,
    /// Shell, Disrupt …) and an alias table from the flavor action names in <c>Enemies.json</c> to a template. An enemy
    /// keeps its flavor name ("Toxic Dart") and gets the template's mechanics. Kept out of <c>Actions.json</c> (sheet-owned,
    /// hero loot) so enemy kits never become hero gear actions. Used when <see cref="EnemySystemConfig.ActionKits"/> is on.
    /// </summary>
    public static class EnemyActionCatalog
    {
        public sealed class CatalogFile
        {
            [JsonPropertyName("description")] public string Description { get; set; } = "";
            [JsonPropertyName("templates")] public List<SpreadsheetActionJson> Templates { get; set; } = new();
            /// <summary>Flavor action name → template name.</summary>
            [JsonPropertyName("aliases")] public Dictionary<string, string> Aliases { get; set; } = new();
        }

        private static readonly object Lock = new();
        private static Dictionary<string, ActionData>? _templates;
        private static Dictionary<string, string>? _aliases;

        public static string FilePath => GameConstants.GetGameDataFilePath("EnemyActions.json");

        public static void Reload()
        {
            lock (Lock)
            {
                _templates = null;
                _aliases = null;
            }
        }

        private static void EnsureLoaded()
        {
            if (_templates != null)
                return;
            var templates = new Dictionary<string, ActionData>(StringComparer.OrdinalIgnoreCase);
            var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                string path = FilePath;
                if (File.Exists(path))
                {
                    var file = JsonSerializer.Deserialize<CatalogFile>(File.ReadAllText(path),
                        new JsonSerializerOptions { ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
                    if (file != null)
                    {
                        foreach (var data in SpreadsheetToActionDataConverter.ConvertList(file.Templates))
                        {
                            data.NormalizeStatBonuses();
                            data.NormalizeChainPositionBonuses();
                            data.NormalizeThresholds();
                            data.NormalizeAccumulations();
                            data.NormalizeTags();
                            if (!string.IsNullOrWhiteSpace(data.Name))
                                templates[data.Name] = data;
                        }
                        foreach (var kv in file.Aliases)
                            if (!string.IsNullOrWhiteSpace(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value))
                                aliases[kv.Key.Trim()] = kv.Value.Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.LogWarning($"EnemyActions.json failed to load: {ex.Message}", "EnemyActionCatalog");
            }
            _aliases = aliases;
            _templates = templates;
        }

        /// <summary>Template name for a flavor action (or the name itself when it is a template); null when unknown.</summary>
        public static string? ResolveTemplateName(string actionName)
        {
            lock (Lock)
            {
                EnsureLoaded();
                if (_templates!.ContainsKey(actionName))
                    return actionName;
                return _aliases!.TryGetValue(actionName, out var t) && _templates.ContainsKey(t) ? t : null;
            }
        }

        public static IReadOnlyCollection<string> TemplateNames
        {
            get { lock (Lock) { EnsureLoaded(); return _templates!.Keys.ToList(); } }
        }

        public static IReadOnlyDictionary<string, string> Aliases
        {
            get { lock (Lock) { EnsureLoaded(); return new Dictionary<string, string>(_aliases!, StringComparer.OrdinalIgnoreCase); } }
        }

        /// <summary>Runtime action for an enemy: the template's mechanics under the flavor name. Null when not in the catalog.</summary>
        public static Action? TryCreate(string actionName)
        {
            ActionData? template;
            lock (Lock)
            {
                EnsureLoaded();
                string? t = ResolveTemplateName(actionName);
                if (t == null || !_templates!.TryGetValue(t, out template))
                    return null;
            }
            var action = ActionDataToActionMapper.CreateAction(template);
            action.Name = actionName;
            return action;
        }
    }
}
