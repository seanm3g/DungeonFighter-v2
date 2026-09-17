using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RPGGame.UI.Avalonia.ArtLab;

namespace RPGGame.Tests.Unit;

/// <summary>Integration checks against real state, inventory, and room-choice handlers; no saves.</summary>
public static class ArtLabSessionTests
{
    public static async Task Run()
    {
        EquipmentSpriteCatalogTests.Run();
        int checks = 0;
        void Check(bool condition, string reason)
        {
            if (!condition) throw new InvalidOperationException(reason);
            checks++;
        }
        var game = new GameCoordinator();
        var player = TestDataBuilders.Character().WithName("Art Adapter Test").WithStats(3, 3, 3, 3).Build();
        game.StateManager.SetCurrentPlayer(player);
        game.StateManager.TransitionToState(GameState.GameLoop);
        var view = new ArtLabSession(game);
        Check(ReferenceEquals(view.Player, player), "Art view must use the actual active character, not a clone.");
        player.CurrentHealth = 12;
        Check(view.Health == 12, "Live health changes must be visible without copying state.");
        var first = TestDataBuilders.Weapon().WithName("Iron Axe").WithTier(1).Build();
        var second = TestDataBuilders.Weapon().WithName("Runic Blade").WithTier(1).Build();
        var blocked = TestDataBuilders.Weapon().WithName("Unwieldable Blade").WithTier(1).Build();
        blocked.AttributeRequirements = new AttributeRequirements(new Dictionary<string, int> { ["strength"] = 10000 });
        player.Inventory.AddRange([first, second, blocked]);
        Check(view.Equip(first) && ReferenceEquals(player.Weapon, first), "Equip must change the actual backend slot.");
        Check(!player.Inventory.Contains(first) && ArtLabSession.ResolvePortrait(player) == 1, "Equipped gear must leave the bag and update the art family.");
        Check(!view.Equip(blocked) && ReferenceEquals(player.Weapon, first) && player.Inventory.Contains(blocked), "Backend attribute requirements must block equip without losing items.");
        Check(view.Equip(second) && player.Inventory.Contains(first) && !player.Inventory.Contains(second), "Existing equip handler must return replaced equipment to the bag.");
        Check(ArtLabSession.ResolvePortrait(player) == 2, "Weapon changes must update portrait selection.");

        var dungeon = new Dungeon("Adapter test", 1, 1, "Generic");
        dungeon.Rooms.Add(new Environment("First", "Room one", false, "Generic"));
        dungeon.Rooms.Add(new Environment("Second", "Room two", false, "Generic"));
        game.StateManager.SetCurrentDungeon(dungeon);
        game.StateManager.SetCurrentRoom(dungeon.Rooms[0]);
        game.StateManager.TransitionToState(GameState.Dungeon);
        Check(ReferenceEquals(view.Dungeon, dungeon) && view.Rooms.Select(r => r.Name).SequenceEqual(dungeon.Rooms.Select(r => r.Name)), "Map must project real generated room order.");
        Check(!view.CanTravel(1), "Cannot advance until the engine reaches its room-choice gate.");
        Check(!view.Equip(first), "Dungeon gear changes must be blocked.");
        var wait = game.GetExitChoiceHandler()!.ShowExitChoiceMenu(1, 2);
        Check(view.CanTravel(1) && !view.CanTravel(0) && !view.CanTravel(2), "Only the actual next room is navigable.");
        await view.TravelAsync(1);
        Check(await wait.WaitAsync(TimeSpan.FromSeconds(2)) == false, "Map click must release the production continue-room gate.");
        var exit = game.GetExitChoiceHandler()!.ShowExitChoiceMenu(1, 2);
        await view.SendInputAsync("2");
        Check(await exit.WaitAsync(TimeSpan.FromSeconds(2)), "Leave button must use the production exit choice.");
        game.StateManager.SetCurrentRoom(dungeon.Rooms[1]);
        Check(view.CurrentRoom == 1 && view.IsVisited(0), "Map position must track backend room changes.");
        var other = TestDataBuilders.Character().WithName("Other hero").Build();
        string otherId = game.StateManager.AddCharacter(other);
        Check(game.StateManager.SwitchCharacter(otherId), "Existing character registry must switch the active hero.");
        Check(ReferenceEquals(view.Player, other), "Character switching must remain live.");
        Console.WriteLine($"Art view: {checks} backend integration checks passed.");
    }
}
