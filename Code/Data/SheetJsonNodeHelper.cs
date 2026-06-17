using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RPGGame.Data
{
    /// <summary>Shared JSON node null/missing checks for sheet conversion.</summary>
    internal static class SheetJsonNodeHelper
    {
        internal static bool IsJsonNodeNullOrMissing(JsonNode n)
        {
            if (n is null)
                return true;
            return n.GetValueKind() == JsonValueKind.Null;
        }
    }
}
