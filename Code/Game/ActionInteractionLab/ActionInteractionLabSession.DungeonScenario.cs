using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.ActionInteractionLab
{
    public sealed partial class ActionInteractionLabSession
    {
        private Dungeon? _labDungeon;
        private List<Environment> _labHostileRooms = new();
        private int _labDungeonRoomIndex;

        /// <summary>Catalog dungeon name or theme used for the last lab generate.</summary>
        public string LabDungeonCatalogKey { get; set; } = "";

        /// <summary>Dungeon level used for generation (min=max).</summary>
        public int LabDungeonLevel { get; set; } = 1;

        /// <summary>Level offset relative to lab hero level for Gen (applied when using hero-relative generate).</summary>
        public int LabDungeonLevelDelta { get; set; }

        /// <summary>Seed shown in the tools UI; updated after Gen.</summary>
        public int LabDungeonSeed { get; set; } = 12345;

        public bool HasLabDungeon => _labDungeon != null && _labHostileRooms.Count > 0;

        public int LabDungeonHostileRoomCount => _labHostileRooms.Count;

        public int LabDungeonRoomIndex => _labDungeonRoomIndex;

        public Environment? CurrentLabDungeonRoom =>
            HasLabDungeon && _labDungeonRoomIndex >= 0 && _labDungeonRoomIndex < _labHostileRooms.Count
                ? _labHostileRooms[_labDungeonRoomIndex]
                : null;

        public string FormatLabDungeonRoomCaption()
        {
            if (!HasLabDungeon)
                return "Dungeon: (none)";
            var room = CurrentLabDungeonRoom;
            string roomName = room?.Name ?? "?";
            if (roomName.Length > 18)
                roomName = roomName.Substring(0, 15) + "...";
            return $"Room {_labDungeonRoomIndex + 1}/{_labHostileRooms.Count}: {roomName}";
        }

        /// <summary>Generates a seeded dungeon and loads the first hostile room's primary enemy.</summary>
        public void GenerateLabDungeon(string? catalogKey = null, int? seed = null, int? dungeonLevel = null)
        {
            if (!string.IsNullOrWhiteSpace(catalogKey))
                LabDungeonCatalogKey = catalogKey.Trim();
            if (string.IsNullOrWhiteSpace(LabDungeonCatalogKey))
            {
                var names = ActionLabDungeonFactory.ListCatalogDungeonNames();
                LabDungeonCatalogKey = names.Count > 0 ? names[0] : "Forest";
            }

            if (seed.HasValue)
                LabDungeonSeed = seed.Value;
            int level = dungeonLevel ?? Math.Clamp(_labPlayer.Level + LabDungeonLevelDelta, 1, 99);
            LabDungeonLevel = level;

            var result = ActionLabDungeonFactory.Generate(LabDungeonCatalogKey, level, LabDungeonSeed, _labPlayer.CurrentRegionId);
            LabDungeonSeed = result.SeedUsed;
            LabDungeonCatalogKey = result.CatalogName;
            _labDungeon = result.Dungeon;
            _labHostileRooms = ActionLabDungeonFactory.GetHostileRooms(result.Dungeon).ToList();
            if (_labHostileRooms.Count == 0)
                _labHostileRooms = result.Dungeon.Rooms.ToList();
            _labDungeonRoomIndex = 0;
            EnterLabDungeonRoom(_labDungeonRoomIndex, resetEncounter: true);
        }

        public void CycleLabDungeonCatalog(int direction)
        {
            var names = ActionLabDungeonFactory.ListCatalogDungeonNames();
            if (names.Count == 0)
                return;
            int idx = -1;
            for (int i = 0; i < names.Count; i++)
            {
                if (string.Equals(names[i], LabDungeonCatalogKey, StringComparison.OrdinalIgnoreCase))
                {
                    idx = i;
                    break;
                }
            }
            if (idx < 0)
                idx = 0;
            int sign = direction >= 0 ? 1 : -1;
            idx = (idx + sign + names.Count * 8) % names.Count;
            LabDungeonCatalogKey = names[idx];
        }

        public void NudgeLabDungeonLevelDelta(int delta)
        {
            LabDungeonLevelDelta = Math.Clamp(LabDungeonLevelDelta + delta, -20, 20);
        }

        public void NudgeLabDungeonSeed(int delta)
        {
            unchecked
            {
                LabDungeonSeed += delta == 0 ? 1 : delta;
            }
        }

        /// <summary>Sets the dungeon generation seed shown in the tools UI (used by Gen / dungeon sim).</summary>
        public void SetLabDungeonSeed(int seed) => LabDungeonSeed = seed;

        public void RandomizeLabDungeonSeed() => LabDungeonSeed = Random.Shared.Next();

        /// <summary>Moves to an adjacent hostile room and loads its primary foe.</summary>
        public bool TryMoveLabDungeonRoom(int delta)
        {
            if (!HasLabDungeon)
                return false;
            int next = _labDungeonRoomIndex + delta;
            if (next < 0 || next >= _labHostileRooms.Count)
                return false;
            EnterLabDungeonRoom(next, resetEncounter: true);
            return true;
        }

        private void EnterLabDungeonRoom(int roomIndex, bool resetEncounter)
        {
            if (_labHostileRooms.Count == 0)
                return;
            _labDungeonRoomIndex = Math.Clamp(roomIndex, 0, _labHostileRooms.Count - 1);
            var room = _labHostileRooms[_labDungeonRoomIndex];
            _labRoom = room;
            var src = room.GetEnemies().FirstOrDefault(e => e.IsAlive) ?? room.GetEnemies().FirstOrDefault();
            if (src != null)
                ApplyRoomEnemyToLab(src);

            if (resetEncounter)
            {
                ClearStepHistoryAndSnapshots();
                ResetSimulatedCombatTurnAccumulator();
                BootstrapCombatState();
                if (UseSeededD20)
                    RewindSeededD20Stream();
            }

            SyncCatalogSelectionToUpcomingActor();
            SyncLabEnemyToCanvasContext();
            if (_restoreTarget != null)
                ApplyLabToCanvasContext(_restoreTarget);
            _refreshCombatUi();
        }

        private void ApplyRoomEnemyToLab(Enemy src)
        {
            EnemyLoader.LoadEnemies();
            var created = EnemyLoader.CreateEnemy(src.Name, Math.Clamp(src.Level, 1, 99));
            if (created != null)
            {
                _sessionEnemyLoaderType = src.Name;
                _labEnemyBaseLevel = Math.Clamp(src.Level, 1, 99);
                _labPanelEnemyLevelDelta = 0;
                _labEnemy = created;
                return;
            }

            // Fallback: use the generated instance (fresh full HP).
            src.CurrentHealth = src.GetEffectiveMaxHealth();
            _sessionEnemyLoaderType = null;
            _labEnemyBaseLevel = Math.Clamp(src.Level, 1, 99);
            _labPanelEnemyLevelDelta = 0;
            _labEnemy = src;
        }
    }
}
