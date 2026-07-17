using System;
using RPGGame.Tests;
using RPGGame.Utils;
using Lab = RPGGame.Tests.Unit.ActionInteractionLab;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Tests for developer action lab helpers and <see cref="ActionSelector.SetStoredActionRoll"/>.
    /// </summary>
    public static class ActionInteractionLabTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;

            // AsyncLocal lab sim rolls and static test rolls can leak ordering-sensitive tests across earlier suites.
            Dice.ClearAsyncLabEncounterTestRoll();
            Dice.ClearTestRoll();

            Lab.ActionInteractionLabMiscTests.StoredActionRollMatchesGetActionRoll(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabMiscTests.LabStepRoundTrip(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabSessionTests.LabSessionBeginsWithDefaultD20Selection16(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabSessionTests.LabSessionBegin_UsesCurrentTuningBaseHealth(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabSessionTests.LabSessionBegin_PicksDefaultCatalogEnemyWhenAvailable(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabMiscTests.ResolveD20ForNextStepUsesSelectedWhenNotRandom(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabSessionTests.LabPlayerIsActiveForDisplayWhenInLabState(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabMiscTests.SetLabEnemyFromLoaderSwapsEnemy(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabSessionTests.CanvasContextRestoredAfterEndSession(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabSessionTests.LabBeginDoesNotResetGlobalGameTime(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabCatalogTests.LabCatalogSelectionAddsActionToCombo(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabCatalogTests.LabRemoveFromComboShrinksSequence(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabComboTests.EnemyComboSelectionUsesComboStepIndex(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabCatalogTests.LabEnemyUsesPlayerComboStripForAmplification(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabUndoResetTests.UndoReplayPreservesComboStripEdits(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabUndoResetTests.UndoReplayPreservesLabStatEdits(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabUndoResetTests.UndoLastStep_RemovesOneStepFromMultiStepHistory(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabUndoResetTests.UndoLastStep_BumpsInputEpochSoQueuedStepsCanBeDropped(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabUndoResetTests.LabCombatLogSnapshot_CloneFromDeepCopiesLines(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabCatalogTests.ActionLabCatalogSync_EnemyNextUsesPlayerStrip(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabCatalogTests.ActionLabCatalogSync_EnemyNextUsesPlayerStripWhenEnemyHasCombo(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabCatalogTests.LabSessionSyncCatalogMatchesComputeHelper(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabUndoResetTests.ResetLabEncounterZerosBothSteps(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabUndoResetTests.ResetLabEncounterAsync_ClearsHistoryHpEffectsKeepsStripEnemy(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabUndoResetTests.ResetLabEncounterAsync_RestoresCombatLogEnemyAlignment(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabSessionTests.RefreshGameDataAsync_ReloadsAndPreservesComboStrip(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabComboTests.LabEnemyTurnUsesEnemyPoolNotForcedCatalog(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabComboTests.LabTotalActionTicks_StepUndoSimAndFightReset(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabCatalogTests.LabCatalogSyncShowsSecondSlotWhenComboStepOne(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabCatalogTests.LabNudgeComboStepClampsStrip(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabCatalogTests.LabSequenceEdit_ResetsStripPositionToFirstSlot(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabCatalogTests.AddSelectedCatalogToStripHelperAddsAction(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabCatalogTests.AddSelectedCatalogToStrip_AppendsToEnd(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabLeftPanelTests.LeftPanelStatAdjustment_StrArmorAndFloors(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabLeftPanelTests.LeftPanelStatAdjustment_ActionSlots(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabLeftPanelTests.LeftPanelStatAdjustment_HeroHpDamageAndHeal(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabLeftPanelTests.LeftPanelStatAdjustment_HeroLevelClamp(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabLeftPanelTests.LeftPanelStatAdjustment_LevelUpMirrorsGameLevelUpForWeapon(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabLeftPanelTests.LeftPanelHeroLevelSyncsLabEnemy_EnemyRowIndependent(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabLeftPanelTests.GetTotalArmorIncludesLabBonus(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabGearTests.ActionLabWeaponFactory_BuildsWithPrefixSuffix(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabGearTests.ActionLabWeaponFactory_BuildsWithMultiplePrefixesAndSuffixes(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabGearTests.ActionLabWeaponFactory_FindIndexMatchesTypeAndTier(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabGearTests.ActionLabArmorFactory_BuildsWithPrefixSuffix(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabGearTests.ActionLabArmorFactory_BuildsWithMultiplePrefixesAndSuffixes(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabGearTests.ActionLabArmorFactory_FindIndexMatchesSlotAndTier(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabGearTests.ActionLabArmorFactory_FilterMapsBodyToChest(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabGearTests.ActionLabGearCatalogFilter_Basics(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabGearTests.ClearLabGear_UnequipsSlot(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabComboTests.WouldNaturalRollSelectComboAction_MatchesSelectActionBasedOnRoll(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabCatalogTests.ApplyCatalogScrollOffsetDelta_Clamps(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabCatalogTests.ApplyEnemyCatalogScrollOffsetDelta_Clamps(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabMiscTests.MapPageStepInput_MapsUndoAndStep(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabComboTests.StepBlockedAfterCombatantDeath(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabSimulationTests.EncounterSimulationBatchCount_ClampedTiers(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabSimulationTests.UseParallelEncounterSimulation_DefaultsTrueAndMutable(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabSimulationTests.IgnoreActionRequirements_ToggleBypassesGearAndWeaponBasic(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabEnemyPanelTests.RightPanelEnemyLabHover_IdFormat(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabEnemyPanelTests.EnemyLevelCaption_ShowsHeroDelta(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabEnemyPanelTests.RightPanelEnemyAdjustment_TryApplyWrongId(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabEnemyPanelTests.LabSession_ApplyLabEnemyLevelDelta_RebuildsDummy(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabEnemyPanelTests.CaptureSimulationSnapshot_IncludesEnemyLevel(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabEnemyPanelTests.TestCharacterFactory_DirectEnemy_ScalesByLevel(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabEnemyPanelTests.DirectStatEnemy_GetEffectiveStrengthUsesDamage(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabEnemyPanelTests.DirectStatEnemy_CombatLogSpeedMatchesPanelSeconds(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabMiscTests.CharacterLabSnapshot_RoundTripIncludesGearAndStrip(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabMiscTests.LoadCharacterSnapshot_ReplacesLabHeroBaseline(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabSimulationTests.SeededDungeonGenerate_IsDeterministic(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabSimulationTests.SetLabDungeonSeed_UpdatesSessionSeed(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabSimulationTests.SeededD20_RepeatsSequenceAfterReset(ref run, ref passed, ref failed);
            Lab.ActionInteractionLabSimulationTests.DungeonSim_SingleRunCompletes(ref run, ref passed, ref failed);

            TestBase.PrintSummary("ActionInteractionLabTests", run, passed, failed);
        }
    }
}
