namespace RPGGame
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using RPGGame.MCP;

    public enum StableFighterStatus
    {
        /// <summary>Between dungeons, waiting for the player to pick the next one.</summary>
        AtHub,
        /// <summary>Running a dungeon (rooms and fights resolve on their own).</summary>
        Exploring,
        /// <summary>Paused between rooms: continue or leave.</summary>
        AwaitingExit,
        /// <summary>Died; the save is already tombstoned.</summary>
        Dead,
        /// <summary>Checked out to the main game for direct play (inventory, skills, gear).</summary>
        InHand
    }

    /// <summary>
    /// One hero in the Stable: the character, its own headless <see cref="GameCoordinator"/>, and what the Stable screen shows.
    /// </summary>
    public sealed class StableFighter
    {
        public const int LogCapacity = 8;

        private readonly List<string> log = new();

        internal StableFighter(Character character, string characterId, OutputCapture output)
        {
            Character = character;
            CharacterId = characterId;
            Output = output;
        }

        public Character Character { get; }

        /// <summary>Registry / save ID: saves and the death tombstone write <c>character_{id}</c>'s own file.</summary>
        public string CharacterId { get; }

        public StableFighterStatus Status { get; internal set; } = StableFighterStatus.AtHub;

        /// <summary>
        /// When on, the fighter keeps going between rooms without asking while HP is at or above
        /// <see cref="StableManager.AutoContinueMinHpPercent"/>; below that it stops and asks.
        /// </summary>
        public bool AutoContinue { get; set; }

        /// <summary>After a dungeon ends (unless the player chose to leave), start the next one on its own.</summary>
        public bool KeepGoing { get; set; }

        /// <summary>Dungeon "Go" repeats when it is still offered.</summary>
        public string? LastDungeonName { get; internal set; }

        internal bool LeftByChoice { get; set; }

        /// <summary>Outcome of the last dungeon (or error), shown at the hub.</summary>
        public string? LastResult { get; internal set; }

        public string? DungeonName { get; internal set; }
        public int DungeonLevel { get; internal set; }
        public int RoomsCleared { get; internal set; }

        /// <summary>Rooms in the current (or last) dungeon; known once the run has generated it.</summary>
        public int TotalRooms
        {
            get => Game?.CurrentDungeon?.Rooms?.Count is > 0 and var live ? live : totalRooms;
            internal set => totalRooms = value;
        }

        private int totalRooms;

        /// <summary>Recent events, newest last.</summary>
        public IReadOnlyList<string> Log => log;

        public bool NeedsDecision => Status switch
        {
            // With keep going on, the hub is just a pause between runs unless the player chose to leave.
            StableFighterStatus.AtHub => !(KeepGoing && !LeftByChoice),
            StableFighterStatus.AwaitingExit or StableFighterStatus.Dead => true,
            _ => false
        };

        public bool IsBusy => Status is StableFighterStatus.Exploring or StableFighterStatus.AwaitingExit;

        /// <summary>Living enemy in the current room, if the fighter is in a fight.</summary>
        public Enemy? CurrentEnemy =>
            Status == StableFighterStatus.Exploring ? Game?.CurrentRoom?.GetEnemies()?.FirstOrDefault(e => e.IsAlive) : null;

        /// <summary>Room being fought through (1-based) while exploring.</summary>
        public int CurrentRoomNumber
        {
            get
            {
                var traversal = Game?.CurrentDungeon?.TraversalState;
                return traversal == null ? RoomsCleared : System.Math.Min(TotalRooms, traversal.NextContentIndex + 1);
            }
        }

        internal GameCoordinator? Game { get; set; }
        internal OutputCapture Output { get; }
        internal TaskCompletionSource<bool>? PendingExit { get; set; }

        /// <summary>Live play-by-play (actions, damage, rooms) for the fighter view.</summary>
        public StableCombatFeed Feed { get; } = new();

        internal void AddLog(string line)
        {
            Feed.Add(line, StableLineTone.Header);
            log.Add(line);
            if (log.Count > LogCapacity)
                log.RemoveAt(0);
        }
    }
}
