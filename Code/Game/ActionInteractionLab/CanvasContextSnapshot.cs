using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.ActionInteractionLab
{
    internal sealed class CanvasContextSnapshot
    {
        public Character? Character { get; init; }
        public Enemy? Enemy { get; init; }
        public string? DungeonName { get; init; }
        public string? RoomName { get; init; }

        public static CanvasContextSnapshot Capture(ICanvasContextManager ctx) => new()
        {
            Character = ctx.GetCurrentCharacter(),
            Enemy = ctx.GetCurrentEnemy(),
            DungeonName = ctx.GetDungeonName(),
            RoomName = ctx.GetRoomName(),
        };

        public void Restore(ICanvasContextManager ctx)
        {
            ctx.SetCurrentCharacter(Character);
            if (Enemy != null)
                ctx.SetCurrentEnemy(Enemy);
            else
                ctx.ClearCurrentEnemy();
            ctx.SetDungeonName(DungeonName);
            ctx.SetRoomName(RoomName);
        }
    }
}
