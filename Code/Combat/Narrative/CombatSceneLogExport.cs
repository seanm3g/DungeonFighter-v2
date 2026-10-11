using System.Collections.Generic;

namespace RPGGame.Combat.Narrative
{
    /// <summary>Gathers live combat state into scene JSON for right-click export.</summary>
    public static class CombatSceneLogExport
    {
        public static bool TryBuild(
            Character? hero,
            Enemy? currentEnemy,
            Environment? room,
            Dungeon? dungeon,
            BattleNarrative? narrative,
            Action? lastPlayerAction,
            out IReadOnlyList<CombatScenePacket> scenes,
            out string json)
        {
            scenes = System.Array.Empty<CombatScenePacket>();
            json = "";

            scenes = CombatScenePacketBuilder.BuildLog(
                hero,
                currentEnemy,
                room,
                dungeon,
                narrative,
                lastPlayerAction);

            if (scenes.Count == 0)
                return false;

            json = CombatScenePacketBuilder.Serialize(scenes);
            return !string.IsNullOrWhiteSpace(json);
        }
    }
}
