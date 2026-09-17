using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RPGGame.Handlers.Inventory;
using RPGGame.MCP;
using RPGGame.MCP.Models;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.UI.Avalonia.ArtLab;

/// <summary>Presentation adapter over the SAME GameCoordinator as the classic window.
/// No simulated actors, fixtures, combat formulas, rewards, or separate save system.</summary>
public sealed class ArtLabSession
{
    public sealed record Room(int Id, string Name, string Kind, string Description, int X, int Y, int[] Neighbors);
    public GameCoordinator Game { get; }
    private readonly CanvasUICoordinator? canvas;
    private readonly InventoryItemActionHandler equipment;
    private bool dispatching;
    public string? LastError { get; private set; }
    public ArtLabSession(GameCoordinator game, CanvasUICoordinator? canvas = null)
    {
        Game = game;
        this.canvas = canvas;
        equipment = new InventoryItemActionHandler(game.StateManager, canvas, new InventoryStateManager());
    }
    public Character? Player => Game.CurrentPlayer;
    public Dungeon? Dungeon => Game.CurrentDungeon;
    public Environment? Location => Game.CurrentRoom;
    public Enemy? Enemy => canvas?.GetCurrentEnemy() ?? Location?.GetEnemies().FirstOrDefault(e => e.IsAlive);
    public int CurrentRoom => Dungeon != null && Location != null ? Dungeon.Rooms.IndexOf(Location) : -1;
    public int Health => Player?.CurrentHealth ?? 0;
    public int MaxHealth => Player?.GetEffectiveMaxHealth() ?? 1;
    public bool IsDead => Game.CurrentState == GameState.Death || Player is { IsAlive: false };
    public bool Won => Game.CurrentState == GameState.DungeonCompletion;
    public bool InCombat => Game.CurrentState == GameState.Combat && Enemy is { IsAlive: true };
    public bool WaitingForRoom => Game.GetExitChoiceHandler()?.IsWaitingForChoice == true;
    public bool CanEquip => Player is { IsAlive: true } && !Game.StateManager.IsComboStripEncounterLocked && Game.CurrentState is GameState.GameLoop or GameState.Inventory or GameState.DungeonCompletion;
    public IReadOnlyList<Action> Moves => Player?.GetComboActions() ?? [];
    public int ActiveMove => Moves.Count == 0 ? -1 : Math.Max(0, Player!.ComboStep) % Moves.Count;
    public IReadOnlyList<Item> Inventory => Game.CurrentInventory;
    public string Journal => canvas?.GetDisplayBufferText() ?? "";

    // Bend the real sequential route for display; never invent branches or replay rooms.
    public Room[] Rooms => (Dungeon?.Rooms ?? []).Select((room, id) =>
    {
        int withinPage = id % 18;
        int row = withinPage / 6;
        int col = row % 2 == 0 ? withinPage % 6 : 5 - withinPage % 6;
        return new Room(id, room.Name, room.IsHostile ? "ENCOUNTER" : "EXPLORATION", room.Description,
            col, row * 2, new[] { id - 1, id + 1 }.Where(n => n >= 0 && n < Dungeon!.Rooms.Count).ToArray());
    }).ToArray();
    public int MapPage => Math.Max(0, CurrentRoom) / 18;
    public bool IsOnMapPage(int id) => id / 18 == MapPage;
    public bool IsVisited(int id) => CurrentRoom >= id;
    public bool IsDiscovered(int id) => id <= CurrentRoom + 1;
    public bool CanTravel(int id) => WaitingForRoom && id == CurrentRoom + 1 && id < (Dungeon?.Rooms.Count ?? 0);
    public Task TravelAsync(int id) => CanTravel(id) ? SendInputAsync("1") : Task.CompletedTask;
    public IReadOnlyList<AgentChoice> Choices
    {
        get
        {
            if (Game.CurrentState is GameState.Combat or GameState.Dungeon && !WaitingForRoom) return [];
            var choices = AgentChoiceBuilder.BuildChoices(Game, AgentChoiceBuilder.ResolveInputContext(Game));
            if (Game.CurrentState == GameState.WeaponSelection)
                return GameInitializer.BuildStarterWeaponsForMenu().Select((weapon, i) => new AgentChoice { Input = (i + 1).ToString(), Label = weapon.name }).ToArray();
            return choices;
        }
    }
    public async Task SendInputAsync(string input)
    {
        // The existing dungeon task waits on its exit-choice channel. Keep that channel usable
        // while rejecting overlapping menu dispatches.
        if (WaitingForRoom) { Game.GetExitChoiceHandler()!.HandleMenuInput(input); return; }
        if (dispatching) return;
        dispatching = true;
        LastError = null;
        try { await Game.HandleInput(input); }
        catch (Exception ex) { LastError = ex.Message; }
        finally { dispatching = false; }
    }
    public bool Equip(Item item)
    {
        if (!CanEquip) return false;
        int index = Game.CurrentInventory.IndexOf(item);
        string? slot = InventoryActionPoolEntries.GetEquipSlotForItem(item);
        if (index < 0 || slot == null) return false;
        bool changed = equipment.ConfirmEquipItem(index, slot, true, refreshInventoryScreen: false, announce: false, out string? failure);
        LastError = changed ? null : failure;
        return changed;
    }
    // Approximate art families only. Exact names and statistics remain backend-driven.
    public static int ResolvePortrait(Character? player)
    {
        if (player?.Weapon == null) return 0;
        string name = player.Weapon.Name.ToLowerInvariant();
        if (name.Contains("axe") || name.Contains("mace") || name.Contains("hammer") || name.Contains("log")) return 1;
        if (name.Contains("wand") || name.Contains("staff") || name.Contains("rune") || name.Contains("runic")) return 2;
        return player.Head != null && player.Body != null ? 2 : 0;
    }
}
