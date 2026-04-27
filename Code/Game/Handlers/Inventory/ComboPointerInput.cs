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
            InvPoolEquip
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
