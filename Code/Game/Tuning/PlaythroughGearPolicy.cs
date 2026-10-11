using System;
using System.Linq;
using RPGGame.Tuning.Profiles;

namespace RPGGame.Tuning
{
    /// <summary>
    /// Headless playthrough gear behavior: at the hub, equip any bag item that beats the equipped one in its slot
    /// (weapons only within the run's weapon type so the class path is kept). Scores come from <see cref="ItemValueModel"/>.
    /// </summary>
    public static class PlaythroughGearPolicy
    {
        /// <summary>Returns how many items were equipped.</summary>
        public static int EquipUpgrades(Character? hero, WeaponType weaponType)
        {
            if (hero == null || hero.Inventory == null || hero.Inventory.Count == 0)
                return 0;

            int equipped = 0;
            foreach (var (slot, current) in new (string, Item?)[]
                     {
                         ("weapon", hero.Weapon), ("head", hero.Head), ("body", hero.Body), ("legs", hero.Legs), ("feet", hero.Feet)
                     })
            {
                double currentScore = current == null ? double.MinValue : ItemValueModel.Score(current);
                var best = hero.Inventory
                    .Where(i => SlotOf(i) == slot)
                    .Where(i => i is not WeaponItem w || w.WeaponType == weaponType)
                    .Select(i => (item: i, score: ItemValueModel.Score(i)))
                    .Where(x => x.score > currentScore * 1.02 + 0.01)
                    .OrderByDescending(x => x.score)
                    .ToList();

                foreach (var (item, _) in best)
                {
                    try
                    {
                        var before = SlotItem(hero, slot);
                        hero.EquipItem(item, slot);
                        if (!ReferenceEquals(SlotItem(hero, slot), before))
                        {
                            equipped++;
                            break;
                        }
                    }
                    catch
                    {
                        // requirements not met or slot rules rejected it: try the next candidate
                    }
                }
            }

            return equipped;
        }

        public static string SlotOf(Item item) => item switch
        {
            WeaponItem => "weapon",
            HeadItem => "head",
            ChestItem => "body",
            LegsItem => "legs",
            FeetItem => "feet",
            _ => ""
        };

        private static Item? SlotItem(Character hero, string slot) => slot switch
        {
            "weapon" => hero.Weapon,
            "head" => hero.Head,
            "body" => hero.Body,
            "legs" => hero.Legs,
            "feet" => hero.Feet,
            _ => null
        };
    }
}
