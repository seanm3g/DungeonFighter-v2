using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// UI customization configuration
    /// </summary>
    public class UICustomizationConfig
    {
        public string MenuSeparator { get; set; } = "";
        public string SubMenuSeparator { get; set; } = "";
        public string InvalidChoiceMessage { get; set; } = "";
        public string PressAnyKeyMessage { get; set; } = "";
        public RarityPrefixesConfig RarityPrefixes { get; set; } = new();
        public ActionNamesConfig ActionNames { get; set; } = new();
        public ErrorMessagesConfig ErrorMessages { get; set; } = new();
        public DebugMessagesConfig DebugMessages { get; set; } = new();
    }

    /// <summary>
    /// Rarity prefixes configuration
    /// </summary>
    public class RarityPrefixesConfig
    {
        public string Common { get; set; } = "";
        public string Uncommon { get; set; } = "";
        public string Rare { get; set; } = "";
        public string Epic { get; set; } = "";
        public string Legendary { get; set; } = "";
    }

    /// <summary>
    /// Action names configuration
    /// </summary>
    public class ActionNamesConfig
    {
        public string BasicAttackName { get; set; } = "";
        public string DefaultActionDescription { get; set; } = "";
    }

    /// <summary>
    /// Error messages configuration
    /// </summary>
    public class ErrorMessagesConfig
    {
        public string FileNotFoundError { get; set; } = "";
        public string JsonDeserializationError { get; set; } = "";
        public string InvalidDataError { get; set; } = "";
        public string SaveError { get; set; } = "";
        public string LoadError { get; set; } = "";
    }

    /// <summary>
    /// Debug messages configuration
    /// </summary>
    public class DebugMessagesConfig
    {
        public string DebugPrefix { get; set; } = "";
        public string WarningPrefix { get; set; } = "";
        public string ErrorPrefix { get; set; } = "";
        public string InfoPrefix { get; set; } = "";
    }
}
