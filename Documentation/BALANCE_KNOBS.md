# Balance knobs (fastbot-v18)

Every tunable value in `GameData/Patches/Balance/fastbot-v18.json`, generated 2026-11-05. Tables (per enemy, archetype, status effect, rarity, item type) are one row each. "Code refs" counts uses of the property name outside its declaration and `"name"` strings in `Code/` outside tests; **0 means nothing reads it** (dead or legacy). "Settings" = exposed in Settings → Combat Tuning (`CombatTuningParameterRegistry`).

432 knobs in 36 sections; 28 with no code reference.

## character (3)

| Knob | Value / shape | Code refs | Settings |
|---|---|---|---|
| `character.playerBaseHealth` | number: 150 | 11 | yes |
| `character.healthPerLevel` | number: 45 | 15 | yes |
| `character.enemyHealthPerLevel` | number: 0 | 5 |  |

## attributes (10)

| Knob | Value / shape | Code refs | Settings |
|---|---|---|---|
| `attributes.playerBaseAttributes.strength` | number: 10 | 190 | yes |
| `attributes.playerBaseAttributes.agility` | number: 10 | 168 | yes |
| `attributes.playerBaseAttributes.technique` | number: 10 | 154 | yes |
| `attributes.playerBaseAttributes.intelligence` | number: 10 | 156 | yes |
| `attributes.playerAttributesPerLevel` | number: 2 | 6 | yes |
| `attributes.playerPrimaryStatPerLevel` | number: 3 | 15 | yes |
| `attributes.playerSecondaryStatPerLevel` | number: 1 | 15 | yes |
| `attributes.enemyAttributesPerLevel` | number: 0 | 6 |  |
| `attributes.enemyPrimaryAttributeBonus` | number: 0 | 3 | yes |
| `attributes.intelligenceRollBonusPer` | number: 10 | 3 |  |

## progression (6, 1 unread)

| Knob | Value / shape | Code refs | Settings |
|---|---|---|---|
| `progression.baseXPToLevel2` | number: 0 | 8 |  |
| `progression.xpScalingFactor` | number: 0 | **0** |  |
| `progression.enemyXPBase` | number: 25 | 11 | yes |
| `progression.enemyXPPerLevel` | number: 5 | 11 | yes |
| `progression.enemyGoldBase` | number: 10 | 7 | yes |
| `progression.enemyGoldPerLevel` | number: 3 | 7 | yes |

## xpRewards (3, 3 unread)

| Knob | Value / shape | Code refs | Settings |
|---|---|---|---|
| `xpRewards.baseXPFormula` | text: "" | **0** |  |
| `xpRewards.minimumXP` | number: 0 | **0** |  |
| `xpRewards.maximumXPMultiplier` | number: 0 | **0** |  |

## experienceSystem (5, 1 unread)

| Knob | Value / shape | Code refs | Settings |
|---|---|---|---|
| `experienceSystem.baseXPFormula` | text: "" | **0** |  |
| `experienceSystem.levelCap` | number: 0 | 3 |  |
| `experienceSystem.statPointsPerLevel` | number: 0 | 3 |  |
| `experienceSystem.skillPointsPerLevel` | number: 0 | 3 |  |
| `experienceSystem.attributeCap` | number: 0 | 3 |  |

## classBalance (12)

| Knob | Value / shape | Code refs | Settings |
|---|---|---|---|
| `classBalance.barbarian.healthMultiplier` | number: 1.55 | 22 |  |
| `classBalance.barbarian.damageMultiplier` | number: 1.35 | 86 |  |
| `classBalance.barbarian.speedMultiplier` | number: 1 | 23 |  |
| `classBalance.warrior.healthMultiplier` | number: 1.65 | 22 |  |
| `classBalance.warrior.damageMultiplier` | number: 1.55 | 86 |  |
| `classBalance.warrior.speedMultiplier` | number: 1 | 23 |  |
| `classBalance.rogue.healthMultiplier` | number: 1.88 | 22 |  |
| `classBalance.rogue.damageMultiplier` | number: 1.5 | 86 |  |
| `classBalance.rogue.speedMultiplier` | number: 1 | 23 |  |
| `classBalance.wizard.healthMultiplier` | number: 1.8 | 22 |  |
| `classBalance.wizard.damageMultiplier` | number: 1 | 86 |  |
| `classBalance.wizard.speedMultiplier` | number: 1.0 | 23 |  |

## classPresentation (19)

| Knob | Value / shape | Code refs | Settings |
|---|---|---|---|
| `classPresentation.wandEquippedComboSlotBonus` | number: 0 | 4 | yes |
| `classPresentation.defaultNoPointsClassName` | text: "Fighter" | 26 |  |
| `classPresentation.maceClassDisplayName` | text: "Barbarian" | 13 |  |
| `classPresentation.swordClassDisplayName` | text: "Warrior" | 13 |  |
| `classPresentation.daggerClassDisplayName` | text: "Rogue" | 13 |  |
| `classPresentation.wandClassDisplayName` | text: "Wizard" | 13 |  |
| `classPresentation.tierThresholds` | list: [2, 20, 60, 120] | 24 |  |
| `classPresentation.attributeSoloTrioTierPrefixes` | list: ["Scarred", "Blooded", "Dread", "Abyssal"] | 9 |  |
| `classPresentation.attributeQuadTierNames` | list: ["Prophet", "Godkiller", "Worldbreaker", "Eternal"] | 9 |  |
| `classPresentation.attributeModifierMace` | text: "of Fury" | 10 |  |
| `classPresentation.attributeModifierSword` | text: "of the Vanguard" | 10 |  |
| `classPresentation.attributeModifierDagger` | text: "of the Veil" | 10 |  |
| `classPresentation.attributeModifierWand` | text: "of the Void" | 10 |  |
| `classPresentation.attributeDuoMaceSword` | text: "Warbrute" | 11 |  |
| `classPresentation.attributeDuoMaceDagger` | text: "Ravager" | 11 |  |
| `classPresentation.attributeDuoMaceWand` | text: "Ragecaller" | 11 |  |
| `classPresentation.attributeDuoSwordDagger` | text: "Duelist" | 11 |  |
| `classPresentation.attributeDuoSwordWand` | text: "Spellblade" | 11 |  |
| `classPresentation.attributeDuoDaggerWand` | text: "Hexblade" | 11 |  |

## earlyGame (9)

| Knob | Value / shape | Code refs | Settings |
|---|---|---|---|
| `earlyGame.startingActionBonusLevelCap` | number: 5 | 4 |  |
| `earlyGame.startingClassPointsBonus.mace` | number: 0 | 147 |  |
| `earlyGame.startingClassPointsBonus.sword` | number: 0 | 170 |  |
| `earlyGame.startingClassPointsBonus.dagger` | number: 0 | 136 |  |
| `earlyGame.startingClassPointsBonus.wand` | number: 0 | 155 |  |
| `earlyGame.startingActionDamageMultiplier.mace` | number: 1 | 147 |  |
| `earlyGame.startingActionDamageMultiplier.sword` | number: 1 | 170 |  |
| `earlyGame.startingActionDamageMultiplier.dagger` | number: 1 | 136 |  |
| `earlyGame.startingActionDamageMultiplier.wand` | number: 1 | 155 |  |

## combat (96)

| Knob | Value / shape | Code refs | Settings |
|---|---|---|---|
| `combat.criticalHitThreshold` | number: 20 | 15 | yes |
| `combat.criticalHitMultiplier` | number: 2 | 2 |  |
| `combat.minimumDamage` | number: 2 | 10 | yes |
| `combat.playerBaseArmor` | number: 6 | 6 | yes |
| `combat.maximumDamageCap` | number: 950 | 12 | yes |
| `combat.attributeDamageScale` | number: 0.1 | 13 | yes |
| `combat.strengthDamagePerPoint` | number: 0.025 | 7 |  |
| `combat.strengthDamageMaxStat` | number: 200 | 9 |  |
| `combat.materialConvertPercentPerUnit` | number: 1 | 9 |  |
| `combat.materialConvertPercentByKeyword.RAGE` | number: 5.42 | 15 |  |
| `combat.materialConvertPercentByKeyword.CRISIS` | number: 4.38 | 6 |  |
| `combat.materialConvertPercentByKeyword.TRIUMPH` | number: 15 | 4 |  |
| `combat.materialConvertPercentByKeyword.BEAT` | number: 0.99 | 3 |  |
| `combat.materialConvertPercentByKeyword.DRAG` | number: 1.99 | 6 |  |
| `combat.materialConvertPercentByKeyword.STREAK` | number: 0.85 | 3 |  |
| `combat.materialConvertPercentByKeyword.GRAZE` | number: 3.54 | 7 |  |
| `combat.materialConvertPercentByKeyword.TROPHY` | number: 10 | 6 |  |
| `combat.materialConvertPercentByKeyword.SWING` | number: 0.8 | 5 |  |
| `combat.materialConvertPercentByKeyword.CHAIN` | number: 8 | 32 |  |
| `combat.materialConvertPercentByKeyword.LENS` | number: 18 | 6 |  |
| `combat.materialConvertPercentByKeyword.CONTACT` | number: 1.04 | 5 |  |
| `combat.materialConvertMaxUnits` | number: 0 | 2 |  |
| `combat.materialFiveStackMintMultiplier` | number: 2 | 2 |  |
| `combat.speedOverflowDamageEfficiency` | number: 0.75 | 4 |  |
| `combat.retriggerDamageMultiplier` | number: 0.5 | 2 |  |
| `combat.agilityOverflowSpeedPerPoint` | number: 0.01 | 1 |  |
| `combat.strengthOverflowEfficiency` | number: 0.5 | 8 |  |
| `combat.intelligenceAmpPerPoint` | number: 0.005 | 4 |  |
| `combat.armorReductionFactor` | number: 275 | 10 | yes |
| `combat.weaponAttackTimeClampMin` | number: 0.5 | 8 | yes |
| `combat.weaponAttackTimeClampMax` | number: 1.5 | 7 | yes |
| `combat.tutorialCombatDelayMultiplier` | number: 2 | 16 |  |
| `combat.enemyEnrageThresholdPercent` | number: 25 | 2 |  |
| `combat.enemyEnrageDamageMult` | number: 1.25 | 2 |  |
| `combat.statusEffectBaseProcChance` | number: 1 | 2 |  |
| `combat.globalHealEffectiveness` | number: 1 | 2 |  |
| `combat.attributeSoftCap` | number: 50 | 2 |  |
| `combat.techniqueMilestones` | list: [] | 2 |  |
| `combat.baseAttackTime` | number: 8 | 12 | yes |
| `combat.minimumAttackTime` | number: 2 | 11 | yes |
| `combat.agilityMin` | number: 10 | 6 | yes |
| `combat.agilityMax` | number: 300 | 10 | yes |
| `combat.agilityMinSpeedMultiplier` | number: 1 | 5 | yes |
| `combat.agilityMaxSpeedMultiplier` | number: 0.3 | 4 | yes |
| `combat.agilitySpeedCurveExponent` | number: 0.8 | 3 |  |
| `combat.weaponSpeedSpread` | number: 0.7 | 1 |  |
| `combat.weaponSpeedPercentPerPoint` | number: 10 | 3 |  |
| `combat.roomStructureRollShiftCap` | number: 2 | 3 |  |
| `combat.stanceAggressiveBlock` | number: 0.5 | 2 |  |
| `combat.stanceDefensiveBlock` | number: 1.5 | 2 |  |
| `combat.statusWeakenDamageMultiplier` | number: 0.5 | 2 |  |
| `combat.statusFocusDamagePerStack` | number: 0.15 | 2 |  |
| `combat.statusVulnerabilityDamagePerStack` | number: 0.15 | 2 |  |
| `combat.statusHardenDefensePerStack` | number: 0.15 | 2 |  |
| `combat.statusExposeDefensePerStack` | number: 0.15 | 2 |  |
| `combat.statusFortifyDefensePerStack` | number: 0.15 | 3 |  |
| `combat.dotDamagePerLevel` | number: 0.1 | 3 |  |
| `combat.enemyDotFollowsDamageCurve` | bool: true | 1 |  |
| `combat.statusMaxStacks` | number: 3 | 2 |  |
| `combat.materialSetSpeedPercentPerPiece.Iron` | number: 18.0 | 48 |  |
| `combat.materialSetSpeedPercentPerPiece.Crystal` | number: 9.0 | 39 |  |
| `combat.materialFightMintSpeedPercentPerUnit.TRIUMPH` | number: 8.0 | 4 |  |
| `combat.materialFightMintSpeedPercentPerUnit.SWING` | number: 1.6 | 5 |  |
| `combat.materialFightMintSpeedPercentPerUnit.RAGE` | number: 1.5 | 15 |  |
| `combat.materialFightMintSpeedPercentPerUnit.CONTACT` | number: 1.6 | 5 |  |
| `combat.materialFightMintSpeedMaxPercent` | number: 100.0 | 2 |  |
| `combat.materialFightMintDamageReductionPercentPerUnit.RAGE` | number: 3.0 | 15 |  |
| `combat.materialFightMintDamageReductionPercentPerUnit.STREAK` | number: 2.0 | 3 |  |
| `combat.materialFightMintDamageReductionPercentPerUnit.CHAIN` | number: 2 | 32 |  |
| `combat.materialFightMintDamageReductionPercentPerUnit.TRIUMPH` | number: 4 | 4 |  |
| `combat.materialFightMintDamageReductionPercentPerUnit.CRISIS` | number: 4 | 6 |  |
| `combat.materialFightMintDamageReductionPercentPerUnit.LENS` | number: 3 | 6 |  |
| `combat.materialFightMintDamageReductionMaxPercent` | number: 60.0 | 3 |  |
| `combat.heroAgilitySpeedSoftCap` | number: 130 | 3 |  |
| `combat.heroAgilitySpeedOverCapEfficiency` | number: 0.1 | 3 |  |
| `combat.materialSetDamageReductionPercentPerPiece.Bronze` | number: 4.0 | 28 |  |
| `combat.materialSetDamageReductionPercentPerPiece.Obsidian` | number: 5.0 | 17 |  |
| `combat.materialSetAttributePerPiece.Gold` | number: 8.0 | 227 |  |
| `combat.materialSetAttributePerPiece.Glass` | number: 4.0 | 19 |  |
| `combat.materialSetAttributePerPiece.Silver` | number: 4.0 | 34 |  |
| `combat.materialConvertPrimaryStatPerUnit.RAGE` | number: 0.271 | 15 |  |
| `combat.materialConvertPrimaryStatPerUnit.CRISIS` | number: 0.219 | 6 |  |
| `combat.materialConvertPrimaryStatPerUnit.TRIUMPH` | number: 0.75 | 4 |  |
| `combat.materialConvertPrimaryStatPerUnit.BEAT` | number: 0.05 | 3 |  |
| `combat.materialConvertPrimaryStatPerUnit.DRAG` | number: 0.1 | 6 |  |
| `combat.materialConvertPrimaryStatPerUnit.STREAK` | number: 0.043 | 3 |  |
| `combat.materialConvertPrimaryStatPerUnit.GRAZE` | number: 0.177 | 7 |  |
| `combat.materialConvertPrimaryStatPerUnit.TROPHY` | number: 0.5 | 6 |  |
| `combat.materialConvertPrimaryStatPerUnit.SWING` | number: 0.04 | 5 |  |
| `combat.materialConvertPrimaryStatPerUnit.CHAIN` | number: 0.4 | 32 |  |
| `combat.materialConvertPrimaryStatPerUnit.LENS` | number: 0.9 | 6 |  |
| `combat.materialConvertPrimaryStatPerUnit.CONTACT` | number: 0.052 | 5 |  |
| `combat.materialConvertPrimaryStatMaxPerFight` | number: 20 | 3 |  |
| `combat.heroAgilitySoftCapExemptsSets` | bool: true | 1 |  |
| `combat.enemyMaximumDamageCap` | number: 999999 | 2 |  |
| `combat.enemyAttackTimeMultiplier` | number: 2.5 | 1 |  |

## combatBalance (19, 3 unread)

| Knob | Value / shape | Code refs | Settings |
|---|---|---|---|
| `combatBalance.criticalHitChance` | number: 0 | 3 | yes |
| `combatBalance.criticalHitDamageMultiplier` | number: 1.59 | 13 | yes |
| `combatBalance.rollDamageMultipliers.comboRollDamageMultiplier` | number: 1.5 | 10 | yes |
| `combatBalance.rollDamageMultipliers.basicRollDamageMultiplier` | number: 1.2143051663552442 | 10 | yes |
| `combatBalance.rollDamageMultipliers.comboAmplificationScalingMultiplier` | number: 1 | 4 | yes |
| `combatBalance.rollDamageMultipliers.tierScalingFallbackMultiplier` | number: 1 | 4 | yes |
| `combatBalance.statusEffectScaling.bleedDuration` | number: 0 | 2 |  |
| `combatBalance.statusEffectScaling.poisonDuration` | number: 0 | 2 |  |
| `combatBalance.statusEffectScaling.stunDuration` | number: 0 | 5 |  |
| `combatBalance.statusEffectScaling.burnDuration` | number: 0 | 2 |  |
| `combatBalance.statusEffectScaling.freezeDuration` | number: 0 | 2 |  |
| `combatBalance.statusEffectScaling.statusEffectDamageScaling` | number: 0 | 3 |  |
| `combatBalance.environmentalEffects.enableEnvironmentalEffects` | bool: false | **0** |  |
| `combatBalance.environmentalEffects.environmentalDamageMultiplier` | number: 0 | **0** |  |
| `combatBalance.environmentalEffects.environmentalDebuffChance` | number: 0 | **0** |  |
| `combatBalance.actionMechanics.swiftSpeedPercent` | number: 10 | 5 | yes |
| `combatBalance.actionMechanics.bludgeonDamagePercent` | number: 15 | 6 | yes |
| `combatBalance.critMissComfortShift` | number: -2 | 5 | yes |
| `combatBalance.rollFeelVarianceCompression` | number: 0.5 | 9 | yes |

## rollSystem (7)

| Knob | Value / shape | Code refs | Settings |
|---|---|---|---|
| `rollSystem.missThreshold.min` | number: 1 | 427 | yes |
| `rollSystem.missThreshold.max` | number: 5 | 1507 | yes |
| `rollSystem.basicAttackThreshold.min` | number: 6 | 427 | yes |
| `rollSystem.basicAttackThreshold.max` | number: 13 | 1507 | yes |
| `rollSystem.comboThreshold.min` | number: 14 | 427 | yes |
| `rollSystem.comboThreshold.max` | number: 19 | 1507 | yes |
| `rollSystem.criticalThreshold` | number: 20 | 5 | yes |

## comboSystem (5)

| Knob | Value / shape | Code refs | Settings |
|---|---|---|---|
| `comboSystem.resetChainOnNormalHit` | bool: false | 1 |  |
| `comboSystem.comboAmplifierMax` | number: 2.5 | 9 | yes |
| `comboSystem.comboAmplifierMaxTech` | number: 110 | 10 | yes |
| `comboSystem.comboAmplifierCurveExponent` | number: 1.1 | 8 | yes |
| `comboSystem.comboAmplifierOverflowPerPoint` | number: 0.004 | 1 |  |

## poison (3)

| Knob | Value / shape | Code refs | Settings |
|---|---|---|---|
| `poison.tickInterval` | number: 0 | 12 |  |
| `poison.damagePerTick` | number: 0 | 7 |  |
| `poison.stacksPerApplication` | number: 0 | 5 |  |

## statusEffects (1)

| Knob | Value / shape | Code refs | Settings |
|---|---|---|---|
| `statusEffects.effects` | table of 5: damagePerTick, duration, maxStacks, skipTurns, speedReduction, stacksPerApplication, tickInterval | 414 |  |

## enemyScaling (6, 1 unread)

| Knob | Value / shape | Code refs | Settings |
|---|---|---|---|
| `enemyScaling.enemyHealthMultiplier` | number: 0 | 19 |  |
| `enemyScaling.enemyDamageMultiplier` | number: 0 | 18 |  |
| `enemyScaling.enemyLevelVariance` | number: 0 | 1 |  |
| `enemyScaling.baseDPSAtLevel1` | number: 0 | 2 |  |
| `enemyScaling.dpsPerLevel` | number: 0 | **0** |  |
| `enemyScaling.enemyArmorPerLevel` | number: 0 | 3 |  |

## enemyBalance (11)

| Knob | Value / shape | Code refs | Settings |
|---|---|---|---|
| `enemyBalance.attributePool.basePointsAtLevel1` | number: 1 | 4 |  |
| `enemyBalance.attributePool.pointsPerLevel` | number: 0 | 4 | yes |
| `enemyBalance.sustainPool.basePointsAtLevel1` | number: 1 | 4 |  |
| `enemyBalance.sustainPool.pointsPerLevel` | number: 0 | 4 | yes |
| `enemyBalance.statConversionRates.strengthPerPoint` | number: 0.1 | 2 | yes |
| `enemyBalance.statConversionRates.agilityPerPoint` | number: 0.1 | 2 | yes |
| `enemyBalance.statConversionRates.techniquePerPoint` | number: 0.1 | 2 | yes |
| `enemyBalance.statConversionRates.intelligencePerPoint` | number: 0.1 | 2 | yes |
| `enemyBalance.statConversionRates.healthPerPoint` | number: 0.1 | 2 | yes |
| `enemyBalance.statConversionRates.armorPerPoint` | number: 0.1 | 2 | yes |
| `enemyBalance.archetypeConfigs` | table of 0:  | 10 |  |

## enemyBaseline (7)

| Knob | Value / shape | Code refs | Settings |
|---|---|---|---|
| `enemyBaseline.health` | number: 50 | 221 |  |
| `enemyBaseline.strength` | number: 3 | 190 |  |
| `enemyBaseline.agility` | number: 3 | 168 |  |
| `enemyBaseline.technique` | number: 3 | 154 |  |
| `enemyBaseline.intelligence` | number: 3 | 156 |  |
| `enemyBaseline.armor` | number: 2 | 299 |  |
| `enemyBaseline.weaponDamage` | number: 0 | 4 |  |

## enemyDPS (5, 1 unread)

| Knob | Value / shape | Code refs | Settings |
|---|---|---|---|
| `enemyDPS.baseDPSAtLevel1` | number: 0 | 2 |  |
| `enemyDPS.dpsPerLevel` | number: 0 | **0** |  |
| `enemyDPS.archetypes` | table of 0:  | 21 |  |
| `enemyDPS.sustainBalance.targetActionsToKill.formula` | text: "" | 2 |  |
| `enemyDPS.sustainBalance.sustainScaling.healthPerLevel` | number: 0 | 15 |  |

## enemySystem (52)

| Knob | Value / shape | Code refs | Settings |
|---|---|---|---|
| `enemySystem.globalMultipliers.healthMultiplier` | number: 1 | 22 | yes |
| `enemySystem.globalMultipliers.damageMultiplier` | number: 0.8 | 86 | yes |
| `enemySystem.globalMultipliers.armorMultiplier` | number: 1 | 10 | yes |
| `enemySystem.globalMultipliers.speedMultiplier` | number: 1 | 23 | yes |
| `enemySystem.progressionScales.baseHealthScale` | number: 1 | 10 | yes |
| `enemySystem.progressionScales.healthGrowthScale` | number: 2.8 | 7 | yes |
| `enemySystem.progressionScales.attributeGrowthScale` | number: 1.25 | 11 | yes |
| `enemySystem.progressionScales.combatTempoScale` | number: 1 | 9 | yes |
| `enemySystem.progressionScales.progressionShape` | number: 0.5 | 7 | yes |
| `enemySystem.progressionScales.playerEnemyParity` | number: 0 | 8 | yes |
| `enemySystem.progressionScales.progressionPivotLevel` | number: 10 | 9 | yes |
| `enemySystem.baselineStats.health` | number: 125 | 221 | yes |
| `enemySystem.baselineStats.strength` | number: 5 | 190 | yes |
| `enemySystem.baselineStats.agility` | number: 5 | 168 | yes |
| `enemySystem.baselineStats.technique` | number: 5 | 154 | yes |
| `enemySystem.baselineStats.intelligence` | number: 5 | 156 | yes |
| `enemySystem.baselineStats.armor` | number: 0 | 299 | yes |
| `enemySystem.baselineStats.weaponDamage` | number: 2 | 4 |  |
| `enemySystem.damageCurve.enabled` | bool: true | 92 |  |
| `enemySystem.damageCurve.baseMultiplier` | number: 1 | 4 |  |
| `enemySystem.damageCurve.growthPerLevel` | number: 0 | 19 |  |
| `enemySystem.damageCurve.exponent` | number: 1 | 4 |  |
| `enemySystem.damageCurve.anchors[]` | list of 41: level, multiplier | 7 |  |
| `enemySystem.healthCurve.enabled` | bool: true | 92 |  |
| `enemySystem.healthCurve.baseMultiplier` | number: 1 | 4 |  |
| `enemySystem.healthCurve.growthPerLevel` | number: 0 | 19 |  |
| `enemySystem.healthCurve.exponent` | number: 1 | 4 |  |
| `enemySystem.healthCurve.anchors[]` | list of 41: level, multiplier | 7 |  |
| `enemySystem.packSizeCurve.enabled` | bool: true | 92 |  |
| `enemySystem.packSizeCurve.baseMultiplier` | number: 1 | 4 |  |
| `enemySystem.packSizeCurve.growthPerLevel` | number: 0 | 19 |  |
| `enemySystem.packSizeCurve.exponent` | number: 1 | 4 |  |
| `enemySystem.packSizeCurve.anchors[]` | list of 3: level, multiplier | 7 |  |
| `enemySystem.scalingPerLevel.health` | number: 10 | 221 | yes |
| `enemySystem.scalingPerLevel.attributes` | number: 0 | 45 | yes |
| `enemySystem.scalingPerLevel.armor` | number: 0 | 299 | yes |
| `enemySystem.archetypes` | table of 6: agility, armor, description, health, intelligence, strength, technique | 21 |  |
| `enemySystem.archetypeCombat` | table of 9: attackTime, damage | 3 |  |
| `enemySystem.identity.enabled` | bool: true | 92 |  |
| `enemySystem.identity.intensityAtLevel1` | number: 0.5 | 3 |  |
| `enemySystem.identity.fullIntensityLevel` | number: 50 | 2 |  |
| `enemySystem.identity.speedExponent` | number: 0.3 | 1 |  |
| `enemySystem.identity.referenceAgilityStrengthRatio` | number: 2.15 | 2 |  |
| `enemySystem.identity.referenceHealthGrowthPercent` | number: 4.29 | 3 |  |
| `enemySystem.identity.referenceBaseAttribute` | number: 3 | 1 |  |
| `enemySystem.identity.calibration` | table of 128: Goblin, Spider, Wolf, Fire Elemental, Lava Golem, Bats | 8 |  |
| `enemySystem.actionKits` | bool: true | 2 |  |
| `enemySystem.attributeGrowthBudgetPerLevel` | number: 6 | 8 | yes |
| `enemySystem.levelVariance` | number: 1 | 7 | yes |
| `enemySystem.spawnTierWeights` | table of 7: commonPercent, uncommonBiomePercent, uncommonRegionPercent, uncommonLocationPercent, rareLocationPercent, anywherePercent | 3 |  |
| `enemySystem.spawnTierWeightsBySettlement` | table of 3: anywherePercent, commonPercent, rareLocationPercent, totalPercent, uncommonBiomePercent, uncommonLocationPercent, uncommonRegionPercent | 11 |  |
| `enemySystem.locationSettlementOverrides` | table of 0:  | 3 |  |

## itemScaling (6)

| Knob | Value / shape | Code refs | Settings |
|---|---|---|---|
| `itemScaling.globalDamageMultiplier` | number: 1 | 17 |  |
| `itemScaling.weaponDamagePerTier` | number: 3 | 11 |  |
| `itemScaling.armorValuePerTier` | number: 2 | 9 |  |
| `itemScaling.speedBonusPerTier` | number: 0.05 | 12 |  |
| `itemScaling.maxTier` | number: 10 | 16 |  |
| `itemScaling.enchantmentChance` | number: 0 | 4 |  |

## weaponScaling (15)

| Knob | Value / shape | Code refs | Settings |
|---|---|---|---|
| `weaponScaling.startingWeaponDamage.mace` | number: 6 | 147 |  |
| `weaponScaling.startingWeaponDamage.sword` | number: 7 | 170 |  |
| `weaponScaling.startingWeaponDamage.dagger` | number: 6 | 136 |  |
| `weaponScaling.startingWeaponDamage.wand` | number: 7 | 155 |  |
| `weaponScaling.tierDamageRanges.tier1.min` | number: 0 | 427 |  |
| `weaponScaling.tierDamageRanges.tier1.max` | number: 0 | 1507 |  |
| `weaponScaling.tierDamageRanges.tier2.min` | number: 0 | 427 |  |
| `weaponScaling.tierDamageRanges.tier2.max` | number: 0 | 1507 |  |
| `weaponScaling.tierDamageRanges.tier3.min` | number: 0 | 427 |  |
| `weaponScaling.tierDamageRanges.tier3.max` | number: 0 | 1507 |  |
| `weaponScaling.tierDamageRanges.tier4.min` | number: 0 | 427 |  |
| `weaponScaling.tierDamageRanges.tier4.max` | number: 0 | 1507 |  |
| `weaponScaling.tierDamageRanges.tier5.min` | number: 0 | 427 |  |
| `weaponScaling.tierDamageRanges.tier5.max` | number: 0 | 1507 |  |
| `weaponScaling.globalDamageMultiplier` | number: 1 | 17 |  |

## rarityScaling (11, 5 unread)

| Knob | Value / shape | Code refs | Settings |
|---|---|---|---|
| `rarityScaling.statBonusMultipliers.common` | number: 0 | 215 |  |
| `rarityScaling.statBonusMultipliers.uncommon` | number: 0 | 87 |  |
| `rarityScaling.statBonusMultipliers.rare` | number: 0 | 95 |  |
| `rarityScaling.statBonusMultipliers.epic` | number: 0 | 76 |  |
| `rarityScaling.statBonusMultipliers.legendary` | number: 0 | 79 |  |
| `rarityScaling.magicFindScaling.uncommon.perPointMultiplier` | number: 0 | **0** |  |
| `rarityScaling.magicFindScaling.rare.perPointMultiplier` | number: 0 | **0** |  |
| `rarityScaling.magicFindScaling.epic.perPointMultiplier` | number: 0 | **0** |  |
| `rarityScaling.magicFindScaling.legendary.perPointMultiplier` | number: 0 | **0** |  |
| `rarityScaling.magicFindScaling.mythic.perPointMultiplier` | number: 0 | **0** |  |
| `rarityScaling.levelBasedRarityScaling` | table of 5: baseMultiplier, description, earlyMultiplier, earlyThreshold, levelBonus, levelReduction, midMultiplier, minLevel | 2 |  |

## equipmentScaling (13)

| Knob | Value / shape | Code refs | Settings |
|---|---|---|---|
| `equipmentScaling.weaponDamagePerTier` | number: 3 | 11 | yes |
| `equipmentScaling.armorValuePerTier` | number: 5 | 9 | yes |
| `equipmentScaling.speedBonusPerTier` | number: 0.05 | 12 | yes |
| `equipmentScaling.maxTier` | number: 10 | 16 | yes |
| `equipmentScaling.enchantmentChance` | number: 0 | 4 |  |
| `equipmentScaling.armorMultiplier` | number: 1.5 | 10 |  |
| `equipmentScaling.armorFlatPerPiece` | number: 4.5 | 1 |  |
| `equipmentScaling.armorPerItemLevel` | number: 0.15 | 1 |  |
| `equipmentScaling.armorPerItemTier` | number: 6 | 1 |  |
| `equipmentScaling.armorSlotWeights.chest` | number: 1.3 | 63 |  |
| `equipmentScaling.armorSlotWeights.legs` | number: 1.1 | 194 |  |
| `equipmentScaling.armorSlotWeights.head` | number: 0.8 | 213 |  |
| `equipmentScaling.armorSlotWeights.feet` | number: 0.8 | 201 |  |

## lootSystem (24)

| Knob | Value / shape | Code refs | Settings |
|---|---|---|---|
| `lootSystem.baseDropChance` | number: 0.15 | 10 | yes |
| `lootSystem.dropChancePerLevel` | number: 0 | 9 |  |
| `lootSystem.maxDropChance` | number: 0.95 | 6 | yes |
| `lootSystem.guaranteedLootChance` | number: 1 | 6 |  |
| `lootSystem.magicFindEffectiveness` | number: 0 | 12 |  |
| `lootSystem.goldDropMultiplier` | number: 1 | 5 | yes |
| `lootSystem.itemValueMultiplier` | number: 1 | 2 |  |
| `lootSystem.affixMagicFindMaxWeightBoost` | number: 2 | 7 |  |
| `lootSystem.affixMagicFindMaxExtraChanceBoost` | number: 2 | 7 |  |
| `lootSystem.comboSequenceBaseMax` | number: 1 | 14 | yes |
| `lootSystem.comboSequenceAbsoluteMax` | number: 8 | 11 | yes |
| `lootSystem.comboSequenceIntelligenceThreshold` | number: 10 | 9 | yes |
| `lootSystem.rarityFromAffixes` | bool: false | 2 |  |
| `lootSystem.affixTierFalloff` | number: 3 | 3 |  |
| `lootSystem.equippedWeaponTypeBias` | number: 0.5 | 1 |  |
| `lootSystem.materialPieceTriggers` | bool: true | 3 |  |
| `lootSystem.comboSlotCapByLevel` | list: [[1, 1], [5, 2], [10, 3], [15, 4], [25, 5], [45, 6], [70, 7] | 3 |  |
| `lootSystem.weaponGearActionClassShare` | number: 0.7 | 2 |  |
| `lootSystem.armorGearActionClassShare` | number: 0 | 2 |  |
| `lootSystem.rarityUpgrade.enabled` | bool: false | 92 |  |
| `lootSystem.rarityUpgrade.baseUpgradeChance` | number: 0.05 | 10 |  |
| `lootSystem.rarityUpgrade.upgradeChanceDecayPerTier` | number: 0.5 | 9 |  |
| `lootSystem.rarityUpgrade.maxUpgradeTiers` | number: 6 | 1 |  |
| `lootSystem.rarityUpgrade.magicFindBonus` | number: 0.0001 | 4 |  |

## itemAffixByRarity (4)

| Knob | Value / shape | Code refs | Settings |
|---|---|---|---|
| `itemAffixByRarity.perRarity` | table of 6: actionBonuses, actionBonusesMax, actionExtraChance, affixRollChance, affixRolls, extraComboSlots, extraComboSlotsExtraChance, extraComboSlotsMax … | 7 |  |
| `itemAffixByRarity.affixPoolWeights` | table of 3: prefix, stat, action | 4 |  |
| `itemAffixByRarity.affixRollChance` | number: 0.5 | 10 |  |
| `itemAffixByRarity.perItemType` | table of 5: Common, Epic, Legendary, Mythic, Rare, Uncommon | 9 |  |

## startingGear (2)

| Knob | Value / shape | Code refs | Settings |
|---|---|---|---|
| `startingGear.weapons` | list: [] | 104 |  |
| `startingGear.armor` | list: [] | 299 |  |

## dungeonScaling (10)

| Knob | Value / shape | Code refs | Settings |
|---|---|---|---|
| `dungeonScaling.roomCountBase` | number: 2 | 7 |  |
| `dungeonScaling.roomCountPerLevel` | number: 0.5 | 7 |  |
| `dungeonScaling.maxRoomCount` | number: 4 | 4 |  |
| `dungeonScaling.postFightHealPercent` | number: 0 | 1 |  |
| `dungeonScaling.enemyCountPerRoom` | number: 2 | 8 |  |
| `dungeonScaling.bossRoomChance` | number: 0.1 | 5 |  |
| `dungeonScaling.bossMergesRoom` | bool: true | 1 |  |
| `dungeonScaling.bossDamageMultiplier` | number: 2.5 | 1 |  |
| `dungeonScaling.trapRoomChance` | number: 0.2 | 5 |  |
| `dungeonScaling.treasureRoomChance` | number: 0.15 | 5 |  |

## dungeonGeneration (11, 4 unread)

| Knob | Value / shape | Code refs | Settings |
|---|---|---|---|
| `dungeonGeneration.minRooms` | number: 2 | **0** |  |
| `dungeonGeneration.roomCountScaling` | number: 0.5 | **0** |  |
| `dungeonGeneration.hostileRoomChance` | number: 0.8 | **0** |  |
| `dungeonGeneration.bossRoomName` | text: "Boss" | **0** |  |
| `dungeonGeneration.defaultTheme` | text: "Forest" | 1 |  |
| `dungeonGeneration.defaultRoomType` | text: "Chamber" | 2 |  |
| `dungeonGeneration.defaultDungeonLevels` | number: 5 | 2 |  |
| `dungeonGeneration.defaultRoomCount` | number: 3 | 2 |  |
| `dungeonGeneration.equipmentSlots` | list: ["Head", "Chest", "Feet", "Weapon"] | 6 |  |
| `dungeonGeneration.statusEffectTypes` | list: ["bleed", "poison", "burn", "slow", "weaken", "stun"] | 6 |  |
| `dungeonGeneration.defaultCharacterName` | text: "Unknown" | 2 |  |

## gameSpeed (2)

| Knob | Value / shape | Code refs | Settings |
|---|---|---|---|
| `gameSpeed.gameTickerInterval` | number: 0 | 1 |  |
| `gameSpeed.gameSpeedMultiplier` | number: 0 | 2 |  |

## gameData (4, 3 unread)

| Knob | Value / shape | Code refs | Settings |
|---|---|---|---|
| `gameData.autoGenerateOnLaunch` | bool: false | **0** |  |
| `gameData.showGenerationMessages` | bool: false | 13 |  |
| `gameData.createBackupsOnAutoGenerate` | bool: true | **0** |  |
| `gameData.forceOverwriteOnAutoGenerate` | bool: false | **0** |  |

## debug (5, 2 unread)

| Knob | Value / shape | Code refs | Settings |
|---|---|---|---|
| `debug.enableDebugOutput` | bool: false | 1 |  |
| `debug.showCombatSimulationDebug` | bool: false | **0** |  |
| `debug.maxDetailedBattles` | number: 10 | **0** |  |
| `debug.logCombatActions` | bool: false | 4 |  |
| `debug.logOnlyOnErrors` | bool: true | 2 |  |

## balanceAnalysis (7, 3 unread)

| Knob | Value / shape | Code refs | Settings |
|---|---|---|---|
| `balanceAnalysis.simulationsPerMatchup` | number: 100 | 3 | yes |
| `balanceAnalysis.targetWinRateMin` | number: 85 | **0** |  |
| `balanceAnalysis.targetWinRateMax` | number: 98 | **0** |  |
| `balanceAnalysis.maxDetailedBattles` | number: 5 | **0** |  |
| `balanceAnalysis.damageCalculation.minimumDamage` | number: 1 | 10 |  |
| `balanceAnalysis.difficultyThresholds.moderate` | number: 0.6 | 4 |  |
| `balanceAnalysis.difficultyThresholds.hard` | number: 0.4 | 9 |  |

## balanceTuningGoals (29)

| Knob | Value / shape | Code refs | Settings |
|---|---|---|---|
| `balanceTuningGoals.winRate.minTarget` | number: 85 | 10 |  |
| `balanceTuningGoals.winRate.maxTarget` | number: 98 | 10 |  |
| `balanceTuningGoals.winRate.optimalMin` | number: 88 | 9 |  |
| `balanceTuningGoals.winRate.optimalMax` | number: 95 | 9 |  |
| `balanceTuningGoals.winRate.criticalLow` | number: 80 | 2 |  |
| `balanceTuningGoals.winRate.warningLow` | number: 85 | 1 |  |
| `balanceTuningGoals.winRate.warningHigh` | number: 98 | 1 |  |
| `balanceTuningGoals.winRate.criticalHigh` | number: 99 | 2 |  |
| `balanceTuningGoals.combatDuration.minTarget` | number: 8 | 10 |  |
| `balanceTuningGoals.combatDuration.maxTarget` | number: 15 | 10 |  |
| `balanceTuningGoals.combatDuration.optimalMin` | number: 9 | 9 |  |
| `balanceTuningGoals.combatDuration.optimalMax` | number: 13 | 9 |  |
| `balanceTuningGoals.combatDuration.criticalShort` | number: 6 | 2 |  |
| `balanceTuningGoals.combatDuration.warningShort` | number: 8 | 1 |  |
| `balanceTuningGoals.combatDuration.warningLong` | number: 15 | 1 |  |
| `balanceTuningGoals.combatDuration.criticalLong` | number: 18 | 2 |  |
| `balanceTuningGoals.weaponBalance.maxVariance` | number: 10 | 1 |  |
| `balanceTuningGoals.weaponBalance.optimalVariance` | number: 5 | 2 |  |
| `balanceTuningGoals.weaponBalance.criticalVariance` | number: 15 | 2 |  |
| `balanceTuningGoals.enemyDifferentiation.minVariance` | number: 3 | 1 |  |
| `balanceTuningGoals.enemyDifferentiation.optimalVariance` | number: 5 | 2 |  |
| `balanceTuningGoals.enemyDifferentiation.criticalVariance` | number: 1 | 2 |  |
| `balanceTuningGoals.qualityWeights.winRateWeight` | number: 0.4 | 1 |  |
| `balanceTuningGoals.qualityWeights.durationWeight` | number: 0.25 | 3 |  |
| `balanceTuningGoals.qualityWeights.weaponBalanceWeight` | number: 0.2 | 3 |  |
| `balanceTuningGoals.qualityWeights.enemyDiffWeight` | number: 0.15 | 3 |  |
| `balanceTuningGoals.levelWinRateCurve.enabled` | bool: true | 92 |  |
| `balanceTuningGoals.levelWinRateCurve.anchors[]` | list of 3: level, targetWinRate | 7 |  |
| `balanceTuningGoals.levelWinRateCurve.tolerancePercent` | number: 3 | 1 |  |

## difficultySettings (1)

| Knob | Value / shape | Code refs | Settings |
|---|---|---|---|
| `difficultySettings` | table of 4: enemyDamageMultiplier, enemyHealthMultiplier, lootMultiplier, xpMultiplier | 2 |  |

## uiCustomization (1, 1 unread)

| Knob | Value / shape | Code refs | Settings |
|---|---|---|---|
| `uiCustomization` | table of 8: basicAttackName, common, debugPrefix, defaultActionDescription, epic, errorPrefix, fileNotFoundError, infoPrefix … | **0** |  |

## modificationRarity (8)

| Knob | Value / shape | Code refs | Settings |
|---|---|---|---|
| `modificationRarity.common` | number: 35 | 215 |  |
| `modificationRarity.uncommon` | number: 25 | 87 |  |
| `modificationRarity.rare` | number: 20 | 95 |  |
| `modificationRarity.epic` | number: 12 | 76 |  |
| `modificationRarity.legendary` | number: 6 | 79 |  |
| `modificationRarity.mythic` | number: 1.8 | 81 |  |
| `modificationRarity.tierBonusPerLevel` | number: 1.5 | 1 |  |
| `modificationRarity.bonusPointEffectiveness` | number: 1 | 1 |  |

