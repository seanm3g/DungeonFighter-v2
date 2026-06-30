using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.BattleStatistics;
using RPGGame.Entity.Services;

namespace RPGGame.ActionInteractionLab
{
    /// <summary>
    /// Shared lab hero/enemy setup used by <see cref="ActionInteractionLabSession"/> and
    /// <see cref="ActionLabEncounterSimulator"/>.
    /// </summary>
    internal static class LabCombatEntityFactory
    {
        public static Character ClonePlayerFromJson(
            string initialPlayerJson,
            string invalidJsonMessage = "Lab: invalid initial player json.")
        {
            var serializer = new CharacterSerializer();
            var data = serializer.Deserialize(initialPlayerJson)
                ?? throw new InvalidOperationException(invalidJsonMessage);
            return serializer.CreateCharacterFromSaveData(data);
        }

        public static void ApplyPanelDeltas(Character labPlayer, LabCombatSnapshot snapshot)
        {
            ApplyPanelDeltas(
                labPlayer,
                snapshot.LabPanelLevelDelta,
                snapshot.LabPanelStrDelta,
                snapshot.LabPanelAgiDelta,
                snapshot.LabPanelTecDelta,
                snapshot.LabPanelIntDelta,
                snapshot.LabPanelArmorDelta,
                snapshot.LabPanelActionSlotDelta);
        }

        public static void ApplyPanelDeltas(
            Character labPlayer,
            int labPanelLevelDelta,
            int labPanelStrDelta,
            int labPanelAgiDelta,
            int labPanelTecDelta,
            int labPanelIntDelta,
            int labPanelArmorDelta,
            int labPanelActionSlotDelta)
        {
            if (labPanelLevelDelta != 0)
                labPlayer.ApplyActionLabLevelDelta(labPanelLevelDelta);
            if (labPanelStrDelta != 0)
                labPlayer.Stats.Strength = Math.Max(1, labPlayer.Stats.Strength + labPanelStrDelta);
            if (labPanelAgiDelta != 0)
                labPlayer.Stats.Agility = Math.Max(1, labPlayer.Stats.Agility + labPanelAgiDelta);
            if (labPanelTecDelta != 0)
                labPlayer.Stats.Technique = Math.Max(1, labPlayer.Stats.Technique + labPanelTecDelta);
            if (labPanelIntDelta != 0)
                labPlayer.Stats.Intelligence = Math.Max(1, labPlayer.Stats.Intelligence + labPanelIntDelta);
            if (labPanelArmorDelta != 0)
                labPlayer.ActionLabArmorBonus = Math.Max(0, labPlayer.ActionLabArmorBonus + labPanelArmorDelta);
            if (labPanelActionSlotDelta != 0)
            {
                labPlayer.ActionLabActionSlotBonus = Math.Max(0, labPlayer.ActionLabActionSlotBonus + labPanelActionSlotDelta);
                ComboSequenceMaxHelper.TrimComboSequenceToMax(
                    labPlayer,
                    ComboSequenceMaxHelper.GetEffectiveMax(labPlayer));
            }
        }

        public static Enemy BuildLabEnemy(
            string? sessionEnemyLoaderType,
            int effectiveLevel,
            BattleConfiguration defaultConfig)
        {
            effectiveLevel = Math.Clamp(effectiveLevel, 1, 99);
            if (!string.IsNullOrEmpty(sessionEnemyLoaderType))
            {
                EnemyLoader.LoadEnemies();
                return EnemyLoader.CreateEnemy(sessionEnemyLoaderType.Trim(), effectiveLevel)
                    ?? TestCharacterFactory.CreateTestEnemy(defaultConfig, 0, effectiveLevel);
            }

            return TestCharacterFactory.CreateTestEnemy(defaultConfig, 0, effectiveLevel);
        }

        /// <summary>
        /// Restores combo strip order on <paramref name="labPlayer"/> from action names.
        /// When <paramref name="preferActionPool"/> is true, resolves each name from the hero pool first
        /// then <see cref="ActionLoader"/>; when false, prefers <see cref="ActionLoader"/> first.
        /// </summary>
        public static void ReapplyComboStrip(
            Character labPlayer,
            IReadOnlyList<string> orderedActionNames,
            bool preferActionPool = true)
        {
            foreach (var a in labPlayer.GetComboActions().ToList())
                labPlayer.RemoveFromCombo(a, ignoreWeaponRequirement: true);

            int nextSlot = 1;
            foreach (var name in orderedActionNames)
            {
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                var act = ResolveComboAction(labPlayer, name, preferActionPool);
                if (act == null)
                    continue;
                if (!act.IsComboAction)
                    act.IsComboAction = true;
                // Match ComboSequenceManager.RestoreComboFromActionNames so opener/middle ordering matches snapshot order.
                act.ComboOrder = nextSlot++;
                labPlayer.AddToCombo(act);
            }
        }

        private static Action? ResolveComboAction(Character labPlayer, string name, bool preferActionPool)
        {
            if (preferActionPool)
            {
                var poolEntry = labPlayer.ActionPool.FirstOrDefault(item =>
                    item.action != null && string.Equals(item.action.Name, name, StringComparison.OrdinalIgnoreCase));
                if (poolEntry.action != null)
                    return poolEntry.action;
                return ActionLoader.GetAction(name);
            }

            var fromLoader = ActionLoader.GetAction(name);
            if (fromLoader != null)
                return fromLoader;
            var fallbackPoolEntry = labPlayer.ActionPool.FirstOrDefault(item =>
                item.action != null && string.Equals(item.action.Name, name, StringComparison.OrdinalIgnoreCase));
            return fallbackPoolEntry.action;
        }
    }
}
