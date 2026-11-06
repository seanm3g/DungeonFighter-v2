using System;

namespace RPGGame.Handlers.Inventory
{
    /// <summary>
    /// Parses mouse-sent combo management tokens (see <see cref="InventoryComboManager.HandleComboPointerInput"/>).
    /// </summary>
    public static class ComboPointerInput
    {
        public const string Prefix = "cpi:";

        public enum Kind
        {
            Unknown,
            Back,
            AddAll,
            Reorder,
            PoolAdd,
            SequenceRemove,
            /// <summary>Equip the inventory item that grants this action, then add that action to the combo sequence.</summary>
            InvPoolEquip,
            /// <summary>Drag and drop: add pool action <c>index</c> to the sequence at slot <c>index2</c> (<c>cpi:insert:P:S</c>).</summary>
            PoolInsert,
            /// <summary>Drag and drop: move sequence slot <c>index</c> to slot <c>index2</c> (<c>cpi:move:F:T</c>).</summary>
            SequenceMove
        }

        public static string Insert(int poolIndex, int slot) => $"{Prefix}insert:{poolIndex}:{slot}";
        public static string Move(int fromSlot, int toSlot) => $"{Prefix}move:{fromSlot}:{toSlot}";

        /// <summary>Like <see cref="TryParse(string, out Kind, out int)"/>, plus the second number of insert / move tokens.</summary>
        public static bool TryParse(string input, out Kind kind, out int index, out int index2)
        {
            index2 = -1;
            if (TryParse(input, out kind, out index))
                return true;
            var parts = (input ?? "").Split(':');
            if (parts.Length != 4 || parts[0] + ":" != Prefix)
                return false;
            if (!int.TryParse(parts[2], out int a) || !int.TryParse(parts[3], out int b) || a < 0 || b < 0)
                return false;
            switch (parts[1])
            {
                case "insert":
                    kind = Kind.PoolInsert;
                    break;
                case "move":
                    kind = Kind.SequenceMove;
                    break;
                default:
                    return false;
            }
            index = a;
            index2 = b;
            return true;
        }

        public static bool TryParse(string input, out Kind kind, out int index)
        {
            kind = Kind.Unknown;
            index = -1;
            if (string.IsNullOrEmpty(input) || !input.StartsWith(Prefix, StringComparison.Ordinal))
                return false;

            var parts = input.Split(':');
            if (parts.Length < 2)
                return false;

            switch (parts[1])
            {
                case "back":
                    kind = Kind.Back;
                    return true;
                case "addall":
                    kind = Kind.AddAll;
                    return true;
                case "reorder":
                    kind = Kind.Reorder;
                    return true;
                case "pool":
                    if (parts.Length >= 3 && int.TryParse(parts[2], out int pi) && pi >= 0)
                    {
                        kind = Kind.PoolAdd;
                        index = pi;
                        return true;
                    }
                    return false;
                case "invpool":
                    if (parts.Length >= 3 && int.TryParse(parts[2], out int ii) && ii >= 0)
                    {
                        kind = Kind.InvPoolEquip;
                        index = ii;
                        return true;
                    }
                    return false;
                case "rm":
                    if (parts.Length >= 3 && int.TryParse(parts[2], out int ri) && ri >= 0)
                    {
                        kind = Kind.SequenceRemove;
                        index = ri;
                        return true;
                    }
                    return false;
                default:
                    return false;
            }
        }
    }
}
