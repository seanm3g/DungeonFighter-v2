using System;
using System.Collections.Generic;
using System.Text.Json;
using Avalonia.Platform;

namespace RPGGame.UI.Avalonia.CombatVisuals;

/// <summary>Exact authored mappings, independent of enemy balance and combat logic.</summary>
public static class EnemyVisualCatalog
{
    private static readonly Lazy<Dictionary<string, string>> entries = new(() =>
    {
        using var stream = AssetLoader.Open(new Uri("avares://DF/Visuals/enemies.json"));
        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
            ?? throw new InvalidOperationException("Missing enemy visual catalog");
        return new Dictionary<string, string>(data, StringComparer.OrdinalIgnoreCase);
    });

    public static IReadOnlyDictionary<string, string> Entries => entries.Value;
    private static readonly Lazy<Dictionary<string, string>> arenas = new(() =>
    {
        using var stream = AssetLoader.Open(new Uri("avares://DF/Visuals/arenas.json"));
        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream) ?? new();
    });
    public static string Arena(string name) => arenas.Value.TryGetValue(name, out var arena) ? arena : BattlePresentation.ArenaFor(Resolve(name));
    public static string? Resolve(string? name) => name != null && entries.Value.TryGetValue(name.Trim(), out var family)
        ? family : null;
}
