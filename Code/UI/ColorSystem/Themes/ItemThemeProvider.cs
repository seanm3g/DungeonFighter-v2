using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.ColorSystem.Themes
{
    /// <summary>
    /// Provides color themes for item properties (rarity, tier, modifications, item types)
    /// </summary>
    public static class ItemThemeProvider
    {
        /// <summary>Dimmed color for &quot;of &quot; / &quot;of the &quot; before suffix keywords (grammar, not fantasy prose).</summary>
        public static Color AffixConnectorColor => ColorPalette.DarkGray.GetColor();

        private static Color QualityPrefixCraftColor => ColorPalette.Bronze.GetColor();

        private static Color MaterialPrefixDefaultColor => ColorPalette.Silver.GetColor();

        /// <summary>
        /// Gets all color themes for an item
        /// </summary>
        public static ItemColorThemes GetItemThemes(Item item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            
            var themes = new ItemColorThemes
            {
                RarityTheme = GetRarityTheme(item.Rarity ?? "Common"),
                TierTheme = GetTierTheme(item.Tier),
                ItemTypeTheme = GetItemTypeTheme(item.Type),
                ModificationThemes = GetModificationThemes(item.Modifications ?? new List<Modification>())
            };
            
            // Add weapon type theme if applicable
            if (item is WeaponItem weapon)
            {
                themes.WeaponTypeTheme = GetWeaponTypeTheme(weapon.WeaponType);
            }
            
            return themes;
        }
        
        /// <summary>
        /// Gets color theme for rarity
        /// </summary>
        public static List<ColoredText> GetRarityTheme(string rarity)
        {
            if (string.IsNullOrEmpty(rarity))
            {
                return ColoredText.FromColor("Common", Colors.White);
            }
            
            var rarityLower = rarity.ToLower();
            
            // Use color templates from ColorTemplates.json where available
            return rarityLower switch
            {
                "common" => ColoredText.FromTemplate("common", rarity),
                "uncommon" => ColoredText.FromTemplate("uncommon", rarity),
                "rare" => ColoredText.FromTemplate("rare", rarity),
                "epic" => ColoredText.FromTemplate("epic", rarity),
                "legendary" => ColoredText.FromTemplate("legendary", rarity),
                "mythic" => ColoredText.FromTemplate("mythic", rarity),
                "transcendent" => ColoredText.FromTemplate("transcendent", rarity),
                _ => ColoredText.FromColor(rarity, Colors.White)
            };
        }
        
        /// <summary>
        /// Gets color theme for tier
        /// Higher tiers get more vibrant colors
        /// </summary>
        public static List<ColoredText> GetTierTheme(int tier)
        {
            return tier switch
            {
                1 => ColoredText.FromColor($"Tier {tier}", Colors.Gray),
                2 => ColoredText.FromColor($"Tier {tier}", ColorPalette.Green.GetColor()),
                3 => ColoredText.FromColor($"Tier {tier}", ColorPalette.Blue.GetColor()),
                4 => ColoredText.FromColor($"Tier {tier}", ColorPalette.Cyan.GetColor()),
                5 => ColoredText.FromColor($"Tier {tier}", ColorPalette.Purple.GetColor()),
                6 => ColoredText.FromColor($"Tier {tier}", ColorPalette.Orange.GetColor()),
                7 => ColoredText.FromTemplate("golden", $"Tier {tier}"),
                8 => ColoredText.FromTemplate("legendary", $"Tier {tier}"),
                9 => ColoredText.FromTemplate("mythic", $"Tier {tier}"),
                >= 10 => ColoredText.FromTemplate("transcendent", $"Tier {tier}"),
                _ => ColoredText.FromColor($"Tier {tier}", Colors.White)
            };
        }
        
        /// <summary>
        /// Gets color themes for modifications
        /// </summary>
        public static Dictionary<string, List<ColoredText>> GetModificationThemes(List<Modification> modifications)
        {
            var themes = new Dictionary<string, List<ColoredText>>();
            
            if (modifications == null)
            {
                return themes;
            }
            
            foreach (var mod in modifications)
            {
                if (mod == null || string.IsNullOrEmpty(mod.Name))
                {
                    continue;
                }
                
                var modNameLower = mod.Name.ToLower();
                var theme = GetModificationTheme(modNameLower, mod.ItemRank ?? "");
                themes[mod.Name] = theme;
            }
            
            return themes;
        }
        
        /// <summary>
        /// Gets color theme for a specific modification
        /// </summary>
        public static List<ColoredText> GetModificationTheme(string modificationName, string itemRank)
        {
            // Try to match by exact name first (from ColorTemplates.json)
            var modNameLower = modificationName.ToLower();
            
            // Check if there's a template for this modification
            if (ColorTemplateLibrary.HasTemplate(modNameLower))
            {
                return ColoredText.FromTemplate(modNameLower, modificationName);
            }
            
            // Fall back to rank-based coloring
            return itemRank.ToLower() switch
            {
                "common" => ColoredText.FromColor(modificationName, Colors.Gray),
                "uncommon" => ColoredText.FromColor(modificationName, ColorPalette.Green.GetColor()),
                "rare" => ColoredText.FromColor(modificationName, ColorPalette.Blue.GetColor()),
                "epic" => ColoredText.FromColor(modificationName, ColorPalette.Purple.GetColor()),
                "legendary" => ColoredText.FromTemplate("legendary", modificationName),
                "mythic" => ColoredText.FromTemplate("mythic", modificationName),
                "transcendent" => ColoredText.FromTemplate("transcendent", modificationName),
                _ => ColoredText.FromColor(modificationName, Colors.White)
            };
        }
        
        /// <summary>
        /// Gets color theme for item type
        /// </summary>
        public static List<ColoredText> GetItemTypeTheme(ItemType itemType)
        {
            return itemType switch
            {
                ItemType.Weapon => ColoredText.FromTemplate("weapon_class", itemType.ToString()),
                ItemType.Head => ColoredText.FromTemplate("head_armor", itemType.ToString()),
                ItemType.Chest => ColoredText.FromTemplate("chest_armor", itemType.ToString()),
                ItemType.Feet => ColoredText.FromTemplate("feet_armor", itemType.ToString()),
                _ => ColoredText.FromColor(itemType.ToString(), Colors.White)
            };
        }
        
        /// <summary>
        /// Gets color theme for weapon type
        /// </summary>
        public static List<ColoredText> GetWeaponTypeTheme(WeaponType weaponType)
        {
            return weaponType switch
            {
                WeaponType.Sword => ColoredText.FromTemplate("sword_weapon", weaponType.ToString()),
                WeaponType.Dagger => ColoredText.FromTemplate("dagger_weapon", weaponType.ToString()),
                WeaponType.Mace => ColoredText.FromTemplate("mace_weapon", weaponType.ToString()),
                WeaponType.Wand => ColoredText.FromTemplate("wand_weapon", weaponType.ToString()),
                _ => ColoredText.FromColor(weaponType.ToString(), Colors.White)
            };
        }
        
        /// <summary>
        /// Gets the color for an item name based on its tier
        /// </summary>
        public static Color GetTierColorForName(int tier)
        {
            return tier switch
            {
                1 => Colors.Gray,
                2 => ColorPalette.Green.GetColor(),
                3 => ColorPalette.Blue.GetColor(),
                4 => ColorPalette.Cyan.GetColor(),
                5 => ColorPalette.Purple.GetColor(),
                6 => ColorPalette.Orange.GetColor(),
                7 => ColorPalette.Gold.GetColor(),
                8 => ColorPalette.Orange.GetColor(), // Legendary color
                9 => ColorPalette.Purple.GetColor(), // Mythic color
                >= 10 => ColorPalette.Purple.GetColor(), // Mythic (highest) color
                _ => Colors.White
            };
        }
        
        /// <summary>
        /// Gets a single color for rarity (helper method)
        /// </summary>
        public static Color GetRarityColor(string rarity)
        {
            if (string.IsNullOrWhiteSpace(rarity))
                return ColorPalette.Common.GetColor();

            return rarity.Trim().ToLowerInvariant() switch
            {
                "common" => ColorPalette.Common.GetColor(),
                "uncommon" => ColorPalette.Green.GetColor(),
                "rare" => ColorPalette.Blue.GetColor(),
                "epic" => ColorPalette.Purple.GetColor(),
                "legendary" => ColorPalette.Orange.GetColor(),
                "mythic" => ColorPalette.Purple.GetColor(),
                // Backward-compat for old saves/data: treat transcendent as mythic (no longer a category)
                "transcendent" => ColorPalette.Purple.GetColor(),
                _ => Colors.White
            };
        }

        /// <summary>
        /// Narrative coloring for one prefix-slot word (Quality / Adjective / Material): templates and rank fallbacks live here.
        /// </summary>
        public static List<ColoredText> GetPrefixWordTheme(Modification? mod, string surfaceForm, string? fallbackAffixRank)
        {
            if (string.IsNullOrEmpty(surfaceForm))
                return new List<ColoredText>();

            string rank = string.IsNullOrWhiteSpace(fallbackAffixRank) ? "Common" : fallbackAffixRank.Trim();

            if (mod == null)
                return GetModificationTheme(surfaceForm.ToLowerInvariant(), rank);

            string lower = surfaceForm.ToLowerInvariant();

            switch (mod.GetPrefixCategory())
            {
                case ModificationPrefixCategory.Quality:
                    return ColoredText.FromColor(surfaceForm, QualityPrefixCraftColor);

                case ModificationPrefixCategory.Material:
                    if (TryGetMaterialPaletteColor(surfaceForm, out Color substance))
                        return ColoredText.FromColor(surfaceForm, substance);
                    if (ColorTemplateLibrary.HasTemplate(lower))
                        return ColoredText.FromTemplate(lower, surfaceForm);
                    return ColoredText.FromColor(surfaceForm, MaterialPrefixDefaultColor);

                case ModificationPrefixCategory.Adjective:
                default:
                    if (ColorTemplateLibrary.HasTemplate(lower))
                        return ColoredText.FromTemplate(lower, surfaceForm);
                    return GetModificationTheme(lower, mod.ItemRank ?? rank);
            }
        }

        /// <summary>
        /// Keyword-only theme for a rolled stat line; <paramref name="affixLineRarity"/> is <see cref="StatBonus.Rarity"/> (affix pool tier).
        /// Uses the rarity ramp so tier power reads clearly (named word templates do not override affix tier for stat lines).
        /// </summary>
        public static List<ColoredText> GetStatBonusKeywordTheme(string keywordSurface, string affixLineRarity)
        {
            string rank = string.IsNullOrWhiteSpace(affixLineRarity) ? "Common" : affixLineRarity.Trim();
            var c = GetRarityColor(rank);
            return ColoredText.FromColor(keywordSurface, c);
        }

        /// <summary>
        /// Leading word(s) of the catalog base name (before the final archetype word): material, template, or rank tint.
        /// </summary>
        public static List<ColoredText> GetBaseLeadingWordTheme(string word, string itemRarityForFallback)
        {
            if (string.IsNullOrEmpty(word))
                return new List<ColoredText>();

            string lower = word.ToLowerInvariant();
            string rank = string.IsNullOrWhiteSpace(itemRarityForFallback) ? "Common" : itemRarityForFallback.Trim();

            if (ColorTemplateLibrary.HasTemplate(lower))
                return ColoredText.FromTemplate(lower, word);

            if (TryGetMaterialPaletteColor(word, out Color mat))
                return ColoredText.FromColor(word, mat);

            var mt = GetModificationTheme(lower, rank);
            if (mt != null && mt.Count > 0)
                return mt;

            return ColoredText.FromColor(word, ColorPalette.DarkGray.GetColor());
        }

        /// <summary>
        /// Uppercase material tokens embedded in base names (glass, mithril, …).
        /// </summary>
        public static bool TryGetMaterialPaletteColor(string word, out Color color)
        {
            color = default;
            if (string.IsNullOrEmpty(word))
                return false;

            string u = word.ToUpperInvariant();
            Color? c = u switch
            {
                "GLASS" => ColorPalette.Cyan.GetColor(),
                "CRYSTAL" => ColorPalette.Cyan.GetColor(),
                "OBSIDIAN" => ColorPalette.DarkBlue.GetColor(),
                "BONE" => ColorPalette.LightGray.GetColor(),
                "BRONZE" => ColorPalette.Bronze.GetColor(),
                "WILLOW" => ColorPalette.DarkGreen.GetColor(),
                "STEEL" => ColorPalette.Silver.GetColor(),
                "GOLD" => ColorPalette.Gold.GetColor(),
                "SILVER" => ColorPalette.Silver.GetColor(),
                "MITHRIL" => ColorPalette.Cyan.GetColor(),
                "DAMASCUS" => ColorPalette.Orange.GetColor(),
                "SHADOW" => ColorPalette.Purple.GetColor(),
                "STONE" => ColorPalette.Gray.GetColor(),
                "UNKNOWN" => ColorPalette.Purple.GetColor(),
                "STRANGE" => ColorPalette.Magenta.GetColor(),
                "CELESTIAL" => ColorPalette.Gold.GetColor(),
                _ => null
            };
            if (!c.HasValue)
                return false;
            color = c.Value;
            return true;
        }

        /// <summary>
        /// Keyword for a modification suffix (&quot;of …&quot; mod row); uses <paramref name="affixPoolRank"/> (<see cref="Modification.ItemRank"/>).
        /// </summary>
        public static List<ColoredText> GetModificationSuffixKeywordTheme(string keywordSurface, string affixPoolRank)
        {
            string rank = string.IsNullOrWhiteSpace(affixPoolRank) ? "Common" : affixPoolRank.Trim();
            return GetModificationTheme(keywordSurface.ToLowerInvariant(), rank);
        }
    }
}

