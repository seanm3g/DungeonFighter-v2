namespace RPGGame
{
    using System.Collections.Generic;

    /// <summary>How a Stable screen line or card border is colored.</summary>
    public enum StableLineTone
    {
        Normal,
        Dim,
        Header,
        /// <summary>A fighter waiting on the player.</summary>
        Alert,
        Good,
        Bad,
        Option,
        Selected
    }

    /// <summary>One line of the Stable screen. Lines with a <see cref="Key"/> are clickable and send that key.</summary>
    public sealed class StableLine
    {
        public StableLine(string text, StableLineTone tone = StableLineTone.Normal, string? key = null)
        {
            Text = text;
            Tone = tone;
            Key = key;
        }

        public string Text { get; }
        public StableLineTone Tone { get; }
        public string? Key { get; }

        public static StableLine Blank => new("");
    }

    /// <summary>One fighter's card on the Stable grid.</summary>
    public sealed class StableCardView
    {
        public int Number { get; init; }
        public bool Selected { get; init; }
        public bool NeedsDecision { get; init; }
        public StableLineTone BorderTone { get; init; }
        public string Title { get; init; } = "";
        /// <summary>Clicking the card body sends this (selects the card).</summary>
        public string SelectKey { get; init; } = "";
        /// <summary>Status block under the title (HP, where, enemy).</summary>
        public List<StableLine> Lines { get; init; } = new();
        /// <summary>Recent events, newest first; shown if there is room.</summary>
        public List<StableLine> Log { get; init; } = new();
        /// <summary>Actions pinned to the bottom of the card; keys route to this card.</summary>
        public List<StableLine> Actions { get; init; } = new();
    }

    /// <summary>Full-screen view of one fighter: hero, enemy and dungeon panels, the live feed, and its actions.</summary>
    public sealed class StableWatchView
    {
        public string Name { get; init; } = "";
        public StableLineTone NameTone { get; init; }
        public List<StableLine> Hero { get; init; } = new();
        public List<StableLine> Enemy { get; init; } = new();
        public List<StableLine> Dungeon { get; init; } = new();
        /// <summary>Play-by-play, oldest first.</summary>
        public List<StableLine> Feed { get; init; } = new();
        public List<StableLine> Actions { get; init; } = new();
    }

    /// <summary>The full-screen Stable as handed to the renderer (also read by tests).</summary>
    public sealed class StableMenuView
    {
        public string Title { get; init; } = "THE STABLE";
        public StableLine Summary { get; init; } = StableLine.Blank;
        public List<StableCardView> Cards { get; init; } = new();
        /// <summary>Last grid slot invites adding a hero when there is room.</summary>
        public bool ShowAddSlot { get; init; }
        /// <summary>When set, a centered panel (add-hero list) replaces the grid.</summary>
        public string? PanelTitle { get; init; }
        public List<StableLine> Panel { get; init; } = new();
        /// <summary>Key hints along the bottom, clickable.</summary>
        public List<StableLine> Footer { get; init; } = new();
        public StableLine? Notice { get; init; }
        /// <summary>When set, one fighter's full-screen view replaces the grid.</summary>
        public StableWatchView? Watch { get; init; }

        /// <summary>Every line on screen, for tests and console output.</summary>
        public IEnumerable<StableLine> AllLines()
        {
            yield return Summary;
            foreach (var card in Cards)
            {
                yield return new StableLine(card.Title, card.BorderTone, card.SelectKey);
                foreach (var l in card.Lines) yield return l;
                foreach (var l in card.Actions) yield return l;
                foreach (var l in card.Log) yield return l;
            }
            if (Watch != null)
            {
                yield return new StableLine(Watch.Name, Watch.NameTone);
                foreach (var l in Watch.Hero) yield return l;
                foreach (var l in Watch.Enemy) yield return l;
                foreach (var l in Watch.Dungeon) yield return l;
                foreach (var l in Watch.Actions) yield return l;
                foreach (var l in Watch.Feed) yield return l;
            }
            foreach (var l in Panel) yield return l;
            foreach (var l in Footer) yield return l;
            if (Notice != null) yield return Notice;
        }
    }
}
