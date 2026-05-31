# Layered Text Animation System

**Last Updated:** May 2026  
**Purpose:** JSON-driven multi-layer compositing for animated canvas text

---

## Overview

Animated text is built as an **ordered layer stack** composited per character:

1. **Layer 0 — `baseColor` (required):** Static color data (solid code, template, or inherited segment colors).
2. **Layer 1+ — `colorOverlay` or `hsvAdjust`:** Applied using a per-character **alpha mask** (0–1).

Presets live in [`GameData/UIConfiguration.json`](../../GameData/UIConfiguration.json) under `textAnimationPresets`.

Implementation:

| File | Role |
|------|------|
| [`Code/UI/TextAnimation/TextAnimationCompositor.cs`](../../Code/UI/TextAnimation/TextAnimationCompositor.cs) | Composites layers → `List<ColoredText>` |
| [`Code/UI/TextAnimation/TextAnimationMaskEvaluator.cs`](../../Code/UI/TextAnimation/TextAnimationMaskEvaluator.cs) | Sine / undulation / brightness-mask alpha |
| [`Code/UI/TextAnimation/TextAnimationPresetLoader.cs`](../../Code/UI/TextAnimation/TextAnimationPresetLoader.cs) | Loads presets from UI config |
| [`Code/UI/TextAnimation/TextAnimationPresetConfig.cs`](../../Code/UI/TextAnimation/TextAnimationPresetConfig.cs) | JSON model types |

---

## Evaluation: What existed before

The game previously merged **one** animated brightness pass into a single RGB per character (`UndulatingTextHelper`, inline dungeon/crit loops, hardcoded path intro shimmer). There was no stackable overlay or independent hue/saturation animation.

Reusable building blocks that informed this system:

- HSV Value math — [`ColorValidator`](../../Code/UI/ColorSystem/Core/ColorValidator.cs)
- Undulation / brightness-mask timing — [`BaseAnimationState`](../../Code/UI/Avalonia/Managers/BaseAnimationState.cs)
- Static multi-color templates — [`ColorTemplateLibrary`](../../Code/UI/ColorSystem/Core/ColorTemplateLibrary.cs)
- Alpha glow behind text (not color layers) — [`TextGlowRenderer`](../../Code/UI/Avalonia/Effects/TextGlowRenderer.cs)

---

## Layer types

### `baseColor`

| Source field | Effect |
|--------------|--------|
| `"solid": "Y"` or `"#FFF4DC"` | Same color for every character |
| `"template": "ethereal"` | Per-character colors from template |
| `"inherit": true` | Use colors from incoming `ColoredText` segments |

### `colorOverlay`

Blends animated colors onto the accumulated result:

- **`gradient`:** Two or more hex / color-code endpoints.
- **`directGradient: true`:** Output `lerp(gradient[0], gradient[1], mask)` directly (path intro warm/cool shimmer).
- **Default blend:** `lerp(accumulated, overlayColor, maskAlpha)` — use when stacking on a template base.

### `hsvAdjust`

Modifies hue, saturation, and/or brightness of the accumulated color:

- **`brightnessScale`:** Undulation-style (`amplitude`) or brightness-mask-style (`fromMaskIntensity: true`).
- **`combineUndulationAndMask: true`:** Sums undulation + brightness mask into one factor (legacy dungeon/display shimmer).
- **`hueShift`:** Degrees, scaled by mask alpha.
- **`saturationScale`:** Multiplier, blended by mask alpha.

Optional **`skipWhenTemplateUndulateDisabled: true`** — skips the layer when the character's template has `"undulate": false`.

### Post-clamp

```json
"clampBrightness": { "min": 150, "max": 255 }
```

Maps to [`ColorValidator.ClampAnimatedTextBrightness`](../../Code/UI/ColorSystem/Core/ColorValidator.cs).

---

## Mask types

| `mask.type` | Behavior |
|-------------|----------|
| `sineWave` | Time + character phase; alpha = `(sin + 1) / 2` |
| `undulation` | Reads `BaseAnimationState.GetUndulationBrightnessAt` |
| `brightnessMask` | Reads `BaseAnimationState.GetBrightnessAt` (percent adjustment) |
| `constant` | Fixed alpha (`constantValue`, default 1.0) |

**`animationState`:** `"dungeonSelection"` (default) or `"crit"` — which singleton provides undulation/mask timing. Timing values still come from `dungeonSelectionAnimation` in UI config.

---

## Shipped presets

| Preset | Screen / usage |
|--------|----------------|
| `pathIntro` | Pre-weapon path quote shimmer |
| `dungeonSelection` | Dungeon name list templates |
| `critLine` | CRITICAL action segment in combat log |
| `displayLogShimmer` | Level-up banner, dungeon/room labels in display log |

---

## Authoring examples

### Path intro warm/cool shimmer

```json
"pathIntro": {
  "layers": [
    { "type": "baseColor", "source": { "solid": "#FFF4DC" } },
    {
      "type": "colorOverlay",
      "directGradient": true,
      "gradient": ["#FFF4DC", "#E2F1FF"],
      "mask": { "type": "sineWave", "phaseDivisorMs": 320, "characterPhaseOffset": 0.36 }
    }
  ]
}
```

### Template base + color sweep + hue/sat accent

```json
"richIntro": {
  "layers": [
    { "type": "baseColor", "source": { "template": "ethereal" } },
    {
      "type": "colorOverlay",
      "gradient": ["#FFF4DC", "#E2F1FF"],
      "mask": { "type": "sineWave", "phaseDivisorMs": 320, "characterPhaseOffset": 0.36 }
    },
    {
      "type": "hsvAdjust",
      "hueShift": 15,
      "saturationScale": 1.4,
      "mask": { "type": "sineWave", "phaseDivisorMs": 500, "characterPhaseOffset": 0.2 }
    }
  ]
}
```

### Dungeon-style brightness stack

```json
"dungeonSelection": {
  "inheritBaseFromSegments": true,
  "clampBrightness": { "min": 150, "max": 255 },
  "layers": [
    { "type": "baseColor", "source": { "inherit": true } },
    {
      "type": "hsvAdjust",
          "hsvAdjust": {
            "brightnessScale": {
              "min": 0.3,
              "max": 2.0,
              "amplitude": 3.0,
              "combineUndulationAndMask": true
            }
          },
          "mask": { "type": "undulation", "animationState": "dungeonSelection" }
        }
      ]
    }
```

---

## Workflow

1. Open **Settings → Text Animation** in the game (Avalonia settings overlay or pop-out window).
2. Pick a **preset** (`pathIntro`, `dungeonSelection`, `critLine`, `displayLogShimmer`).
3. Edit layers in the panel — live preview updates at ~20fps on the black preview bar.
4. For shimmer presets, choose a **preview template** (e.g. `fiery`) to see multi-color base layers.
5. Optionally add an **accent HSV layer** (hue shift + saturation) stacked on top.
6. Tune **global animation timing** (undulation / brightness mask clock).
7. Click the main **Save** button to write `UIConfiguration.json`.

You can also edit `GameData/UIConfiguration.json` directly → restart or reload config.

---

## Related documentation

- [COLOR_SYSTEM.md](COLOR_SYSTEM.md) — templates, color codes, static layering
- [TEXT_SYSTEM_TUNING_GUIDE.md](../02-Development/TEXT_SYSTEM_TUNING_GUIDE.md) — spacing and display tuning
