using System;
using System.Collections.Generic;

namespace RPGGame
{
    public enum TrainingGroundTutorialActor
    {
        Any,
        Hero,
        Dummy
    }

    public sealed class TrainingGroundTutorialEvent
    {
        public TrainingGroundTutorialEvent(TrainingGroundTutorialActor actor, int roll, string narrativeLine,
            int? luckRoll = null, IEnumerable<TutorialCoachNote>? notes = null)
        {
            if (roll < 1 || roll > 20)
                throw new ArgumentOutOfRangeException(nameof(roll), "Tutorial rolls must be between 1 and 20.");
            if (luckRoll is < 1 or > 20)
                throw new ArgumentOutOfRangeException(nameof(luckRoll), "Tutorial rolls must be between 1 and 20.");
            if (string.IsNullOrWhiteSpace(narrativeLine))
                throw new ArgumentException("Tutorial narrative cannot be empty.", nameof(narrativeLine));

            Actor = actor;
            Roll = roll;
            LuckRoll = luckRoll;
            NarrativeLine = narrativeLine.Trim();
            Notes = notes != null ? new List<TutorialCoachNote>(notes) : new List<TutorialCoachNote>();
        }

        public TrainingGroundTutorialActor Actor { get; }

        /// <summary>The first d20 face.</summary>
        public int Roll { get; }

        /// <summary>The second face a 2d20 luck roll (Leather <c>Lucky</c>) shows; null leaves it random.</summary>
        public int? LuckRoll { get; }

        public string NarrativeLine { get; }

        /// <summary>Coach popups for this swing, each waiting on its sequence column.</summary>
        public IReadOnlyList<TutorialCoachNote> Notes { get; }

        public bool Matches(Actor actor)
        {
            if (actor == null)
                return false;

            return Actor switch
            {
                TrainingGroundTutorialActor.Any => true,
                TrainingGroundTutorialActor.Hero => actor is Character and not Enemy,
                TrainingGroundTutorialActor.Dummy => PreWeaponTrainingFlow.IsTrainingDummy(actor),
                _ => false
            };
        }
    }

    /// <summary>
    /// The Training Ground's scripted beats. Each actor takes its own beats in order (the hero's and the dummy's are
    /// independent queues), so the speed order of their turns can't hand one side the other's roll.
    /// </summary>
    public sealed class TrainingGroundTutorialScript
    {
        private readonly List<TrainingGroundTutorialEvent> events;
        private readonly bool[] consumed;

        public TrainingGroundTutorialScript(IEnumerable<TrainingGroundTutorialEvent> events,
            IEnumerable<TutorialCoachNote>? introNotes = null,
            IEnumerable<TutorialCoachNote>? outroNotes = null)
        {
            if (events == null)
                throw new ArgumentNullException(nameof(events));

            this.events = new List<TrainingGroundTutorialEvent>(events);
            consumed = new bool[this.events.Count];
            IntroNotes = introNotes != null ? new List<TutorialCoachNote>(introNotes) : new List<TutorialCoachNote>();
            OutroNotes = outroNotes != null ? new List<TutorialCoachNote>(outroNotes) : new List<TutorialCoachNote>();
        }

        public IReadOnlyList<TrainingGroundTutorialEvent> Events => events;

        /// <summary>Beats taken so far.</summary>
        public int ConsumedCount
        {
            get
            {
                int n = 0;
                foreach (bool c in consumed)
                    if (c)
                        n++;
                return n;
            }
        }

        public bool IsComplete => ConsumedCount >= events.Count;

        /// <summary>Popups before the first turn: how the board reads.</summary>
        public IReadOnlyList<TutorialCoachNote> IntroNotes { get; }

        /// <summary>Popups after the dummy falls.</summary>
        public IReadOnlyList<TutorialCoachNote> OutroNotes { get; }

        public void Reset() => Array.Clear(consumed);

        /// <summary>Takes <paramref name="actor"/>'s next untaken beat; false when it has none left.</summary>
        public bool TryConsumeForActor(Actor actor, out TrainingGroundTutorialEvent? tutorialEvent)
        {
            tutorialEvent = null;
            for (int i = 0; i < events.Count; i++)
            {
                if (consumed[i] || !events[i].Matches(actor))
                    continue;
                consumed[i] = true;
                tutorialEvent = events[i];
                return true;
            }
            return false;
        }
    }
}
